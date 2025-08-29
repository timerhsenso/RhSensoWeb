using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RhSensoWeb.Areas.SYS.Services;       // ITaux2Service
using RhSensoWeb.Common;
using RhSensoWeb.Data;                     // ApplicationDbContext
using RhSensoWeb.Filters;                  // RequirePermission
using RhSensoWeb.Models;                   // Taux2
using RhSensoWeb.Services.Security;        // IRowTokenService
using System.Reflection;
using Microsoft.AspNetCore.RateLimiting;


namespace RhSensoWeb.Areas.SYS.Controllers
{
    [Area("SYS")]
    [Authorize]
    public class Taux2Controller : Controller
    {
        private readonly ITaux2Service _service;
        private readonly ILogger<Taux2Controller> _logger;
        private readonly IRowTokenService _rowToken;
        private readonly IMemoryCache _cache;

        public Taux2Controller(
            ITaux2Service service,
            ILogger<Taux2Controller> logger,
            IRowTokenService rowToken,
            IMemoryCache cache)
        {
            _service = service;
            _logger = logger;
            _rowToken = rowToken;
            _cache = cache;
        }

        // Payload do token (PK composta)
        private sealed record RowKeys(string Cdtptabela, string Cdsituacao);

        // ====== VIEW PRINCIPAL ======
        // ===== VIEW PRINCIPAL =====
        [HttpGet]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "C")]
        public IActionResult Index(string cdtptabela = null, string dctabela = null)
        {
            // 1) se não vier pela querystring, tenta pegar do TempData (POST do pai)
            cdtptabela ??= TempData["Taux2.Filter.cdtptabela"] as string;
            dctabela ??= TempData["Taux2.Filter.dctabela"] as string;

            // 2) passa para a View
            ViewBag.CdtptabelaFiltro = cdtptabela?.Trim();
            ViewBag.SubTitle = string.IsNullOrWhiteSpace(dctabela) ? "" : dctabela.Trim();
            ViewBag.SubTitle = (!string.IsNullOrWhiteSpace(cdtptabela) && !string.IsNullOrWhiteSpace(dctabela))
                   ? $"{cdtptabela} - {dctabela}"
                   : (dctabela ?? "");
            return View();
        }


        // Recebe POST do Taux1 e salva filtro para o próximo request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult OpenFromParent([FromForm] string cdtptabela, [FromForm] string dctabela)
        {
            TempData["Taux2.Filter.cdtptabela"] = cdtptabela?.Trim();
            TempData["Taux2.Filter.dctabela"] = dctabela?.Trim();
            return RedirectToAction(nameof(Index));
        }


        // ====== LISTAGEM PARA GRID ======
        [HttpGet]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "C")]
        public async Task<IActionResult> GetData(string? cdtptabela = null)
        {
            var userId = User?.Identity?.Name ?? "anon";

            // pega tudo do service (sem quebrar o padrão/assinatura)
            var result = await _service.GetDataAsync(userId);
            if (!result.Success)
                return Json(new { data = new List<object>(), error = result.Message });

            var filtro = cdtptabela?.Trim();

            try
            {
                if (result.Data is IEnumerable<Taux2> list)
                {
                    // aplica o filtro se veio
                    if (!string.IsNullOrEmpty(filtro))
                        list = list.Where(x => (x.Cdtptabela ?? "").Trim() == filtro);

                    var dataTyped = list.Select(r =>
                    {
                        var k1 = (r.Cdtptabela ?? "").Trim();
                        var k2 = (r.Cdsituacao ?? "").Trim();
                        return new
                        {
                            r.Cdtptabela,
                            r.Cdsituacao,
                            r.Dcsituacao,
                            r.Noordem,
                            r.Ativo,
                            deleteToken = _rowToken.Protect(new RowKeys(k1, k2), "Delete", userId, TimeSpan.FromMinutes(10)),
                            editToken = _rowToken.Protect(new RowKeys(k1, k2), "Edit", userId, TimeSpan.FromMinutes(10))
                        };
                    });

                    return Json(new { data = dataTyped });
                }

                // fallback genérico caso o service retorne shape anônimo
                var raw = (result.Data as IEnumerable<object>) ?? Enumerable.Empty<object>();
                var data = raw.Where(o =>
                       string.IsNullOrEmpty(filtro)
                    || (GetString(o, "Cdtptabela") ?? "").Trim() == filtro)
                    .Select(o =>
                    {
                        var k1 = (GetString(o, "Cdtptabela") ?? "").Trim();
                        var k2 = (GetString(o, "Cdsituacao") ?? "").Trim();
                        return new
                        {
                            Cdtptabela = k1,
                            Cdsituacao = k2,
                            Dcsituacao = GetString(o, "Dcsituacao"),
                            Noordem = GetString(o, "Noordem"),
                            Ativo = GetString(o, "Ativo"),
                            deleteToken = _rowToken.Protect(new RowKeys(k1, k2), "Delete", userId, TimeSpan.FromMinutes(10)),
                            editToken = _rowToken.Protect(new RowKeys(k1, k2), "Edit", userId, TimeSpan.FromMinutes(10))
                        };
                    });

                return Json(new { data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao montar retorno do GetData de Taux2.");
                return Json(new { data = result.Data });
            }
        }


        // ====== SAFE EDIT via token ======
        [HttpGet]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "A")]
        public async Task<IActionResult> SafeEdit([FromQuery] string token)
        {
            var userId = User?.Identity?.Name ?? "anon";
            var (resp, entidade) = await _service.GetForSafeEditAsync(token, userId);
            if (!resp.Success) return Forbid();
            return View("Edit", entidade!);
        }

        // ====== DTOs de exclusão por token ======
        public sealed class DeleteByTokenDto { public string Token { get; set; } = string.Empty; }
        public sealed class DeleteBatchDto { public List<string> Tokens { get; set; } = new(); }

        // ====== DELETE por TOKEN ======
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "E")]
        public async Task<IActionResult> DeleteByToken([FromBody] DeleteByTokenDto dto)
        {
            var userId = User?.Identity?.Name ?? "anon";
            try
            {
                var resp = await _service.DeleteByTokenAsync(dto.Token, userId);
                if (resp.Success)
                    return Ok(ApiResponse.Ok("Excluído com sucesso."));
                var msg = string.IsNullOrWhiteSpace(resp.Message) ? "Não foi possível excluir o registro." : resp.Message;
                return BadRequest(ApiResponse.Fail(msg));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro em DeleteByToken (Taux2). token={Token} user={User}", dto.Token, userId);
                return StatusCode(500, ApiResponse.Fail("Erro interno ao excluir."));
            }
        }

        // ====== DELETE em lote por tokens ======
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "E")]
        public async Task<IActionResult> DeleteBatch([FromBody] DeleteBatchDto dto)
        {
            if (dto?.Tokens == null || dto.Tokens.Count == 0)
                return BadRequest(ApiResponse.Fail("Nenhum token informado."));
            var userId = User?.Identity?.Name ?? "anon";
            int ok = 0, fail = 0;
            foreach (var token in dto.Tokens)
            {
                try
                {
                    var resp = await _service.DeleteByTokenAsync(token, userId);
                    if (resp.Success) ok++;
                    else fail++;
                }
                catch
                {
                    fail++;
                }
            }
            var allOk = fail == 0;
            var message = allOk ? $"Exclusão concluída. OK {ok}." : $"Exclusão concluída com falhas. OK {ok} | Falhas {fail}.";
            return Ok(new { success = allOk, ok, fail, message });
        }

        // ====== CADASTRO (CRIAR NOVO) ======
        [HttpGet]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "I")]
        public async Task<IActionResult> Create()
        {
            ViewBag.TiposTabela = await _service.GetTiposTabelaAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "I")]
        public async Task<IActionResult> Create([Bind("Cdtptabela,Cdsituacao,Dcsituacao")] Taux2 entity)
        {
            var resp = await _service.CreateAsync(entity, ModelState);
            if (resp.Success)
            {
                TempData["SuccessMessage"] = resp.Message;
                return RedirectToAction(nameof(Index));
            }

            ViewBag.TiposTabela = await _service.GetTiposTabelaAsync();
            TempData["ErrorMessage"] = resp.Message;
            return View(entity);
        }

        // ====== EDICAO ======
        [HttpGet]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "A")]
        public async Task<IActionResult> Edit(string token)
        {
            var (resp, entidade) = await _service.GetForSafeEditAsync(token, User.Identity?.Name!);
            if (!resp.Success || entidade is null)
            {
                TempData["ErrorMessage"] = resp.Message;
                return RedirectToAction(nameof(Index));
            }
            return View(entidade);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "A")]
        public async Task<IActionResult> Edit(string token, [Bind("Cdtptabela,Cdsituacao,Dcsituacao")] Taux2 entity)
        {
            var (resp, entidade) = await _service.GetForSafeEditAsync(token, User.Identity?.Name!);
            if (!resp.Success || entidade is null)
            {
                TempData["ErrorMessage"] = resp.Message;
                return RedirectToAction(nameof(Index));
            }
            var respUpdate = await _service.EditAsync((entidade.Cdtptabela!, entidade.Cdsituacao!), entity, ModelState);
            if (respUpdate.Success)
            {
                TempData["SuccessMessage"] = respUpdate.Message;
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = respUpdate.Message;
            return View(entity);
        }

        // ====== EXCLUSAO ======
        [HttpGet]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "E")]
        public async Task<IActionResult> Delete(string token)
        {
            var (resp, entidade) = await _service.GetForSafeEditAsync(token, User.Identity?.Name!);
            if (!resp.Success || entidade is null)
            {
                TempData["ErrorMessage"] = resp.Message;
                return RedirectToAction(nameof(Index));
            }
            return View(entidade);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "E")]
        public async Task<IActionResult> DeleteConfirmed(string token)
        {
            var resp = await _service.DeleteByTokenAsync(token, User.Identity?.Name!);
            if (resp.Success) TempData["SuccessMessage"] = resp.Message;
            else TempData["ErrorMessage"] = resp.Message;
            return RedirectToAction(nameof(Index));
        }

        // ====== VIEW DETALHES ======
        [HttpGet]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "C")]
        public async Task<IActionResult> Details(string cdtptabela, string cdsituacao)
        {
            if (string.IsNullOrEmpty(cdtptabela) || string.IsNullOrEmpty(cdsituacao)) return NotFound();
            var entity = await _service.GetByIdAsync((cdtptabela, cdsituacao));
            if (entity is null) return NotFound();
            return View(entity);
        }

        // ====== TOGGLE ATIVO (AJAX) ======
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("UpdateAtivoPolicy")]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "A")]
        public async Task<IActionResult> UpdateAtivo([FromForm] string cdtptabela, [FromForm] string cdsituacao, [FromForm] bool ativo)
        {
            var userId = User?.Identity?.Name ?? "anon";

            // (Opcional) pequeno cooldown local — pode remover se quiser deixar só no Service
            var keyLocal = $"SYS:Taux2:UpdateAtivo:{userId}:{cdtptabela}|{cdsituacao}";
            if (_cache.TryGetValue(keyLocal, out _))
                return Json(new { success = false, message = "Aguarde um instante antes de alterar novamente." });
            _cache.Set(keyLocal, 1, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2) });

            var resp = await _service.UpdateAtivoAsync((cdtptabela, cdsituacao), ativo, userId);

            // NO-OP se a mensagem for a padronizada do service
            var noop = string.Equals(resp?.Message, "Status já estava atualizado.", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(resp?.Message, "Sem alteração (já estava nesse estado).", StringComparison.OrdinalIgnoreCase);

            return Json(new
            {
                success = resp?.Success ?? false,
                message = resp?.Message ?? "Erro ao processar a solicitação.",
                noop
            });
        }


        // ====== HEALTH CHECK ======
        [HttpGet]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "C")]
        public async Task<IActionResult> HealthCheck()
        {
            var resp = await _service.HealthCheckAsync();
            return Json(resp.Success
                ? new { success = true, message = resp.Message, total = resp.Data }
                : new { success = false, message = resp.Message });
        }

        // Helper de reflexão (fallback GetData)
        private static string? GetString(object obj, string prop)
        {
            var p = obj.GetType().GetProperty(
                prop,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            return p?.GetValue(obj)?.ToString();
        }
    }
}