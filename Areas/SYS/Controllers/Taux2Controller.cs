using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Reflection;

using RhSensoWeb.Common;
using RhSensoWeb.Models;                   // Taux2
using RhSensoWeb.Filters;                  // RequirePermission
using RhSensoWeb.Services.Security;        // IRowTokenService
using RhSensoWeb.Areas.SYS.Services;       // ITaux2Service
using RhSensoWeb.Data;                     // ApplicationDbContext

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
        [HttpGet]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "C")]
        public IActionResult Index() => View();

        // ====== LISTAGEM PARA GRID ======
        [HttpGet]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "C")]
        public async Task<IActionResult> GetData()
        {
            var userId = User?.Identity?.Name ?? "anon";
            var result = await _service.GetDataAsync(userId);
            if (!result.Success)
                return Json(new { data = new List<object>(), error = result.Message });

            try
            {
                if (result.Data is IEnumerable<Taux2> list)
                {
                    var dataTyped = list.Select(r =>
                    {
                        var k1 = (r.Cdtptabela ?? string.Empty).Trim();
                        var k2 = (r.Cdsituacao ?? string.Empty).Trim();
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

                // Fallback genérico (projeção anônima)
                var raw = (result.Data as IEnumerable<object>) ?? Enumerable.Empty<object>();
                var data = raw.Select(o =>
                {
                    string k1 = GetString(o, "Cdtptabela")?.Trim() ?? string.Empty;
                    string k2 = GetString(o, "Cdsituacao")?.Trim() ?? string.Empty;
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

                var msg = string.IsNullOrWhiteSpace(resp.Message)
                    ? "Não foi possível excluir o registro."
                    : resp.Message;

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
                    if (resp.Success) ok++; else fail++;
                }
                catch
                {
                    fail++;
                }
            }

            var allOk = fail == 0;
            var message = allOk
                ? $"Exclusão concluída. OK {ok}."
                : $"Exclusão concluída com falhas. OK {ok} | Falhas {fail}.";

            return Ok(new { success = allOk, ok, fail, message });
        }

        // ====== CREATE (GET/POST) ======
        [HttpGet]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "I")]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "I")]
        public async Task<IActionResult> Create([Bind("Cdtptabela,Cdsituacao,Dcsituacao,Noordem,Flativoaux,Ativo")] Taux2 entity)
            => Json(await _service.CreateAsync(entity, ModelState));

        // ====== EDIT (GET/POST) ======
        [HttpGet]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "A")]
        public async Task<IActionResult> Edit(string cdtptabela, string cdsituacao)
        {
            if (string.IsNullOrWhiteSpace(cdtptabela) || string.IsNullOrWhiteSpace(cdsituacao))
                return BadRequest("Id inválido.");

            var db = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var entity = await db.Taux2.AsNoTracking()
                                       .FirstOrDefaultAsync(x => x.Cdtptabela == cdtptabela && x.Cdsituacao == cdsituacao);

            if (entity is null)
                return NotFound();

            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "A")]
        public async Task<IActionResult> Edit(string cdtptabela, string cdsituacao, [Bind("Cdtptabela,Cdsituacao,Dcsituacao,Noordem,Flativoaux,Ativo")] Taux2 entity)
            => Json(await _service.EditAsync((cdtptabela, cdsituacao), entity, ModelState));

        // ====== DELETE tradicional (confirmação) ======
        [HttpGet]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "E")]
        public async Task<IActionResult> Delete(string cdtptabela, string cdsituacao)
        {
            if (string.IsNullOrWhiteSpace(cdtptabela) || string.IsNullOrWhiteSpace(cdsituacao)) return NotFound();
            var entity = await _service.GetByIdAsync((cdtptabela, cdsituacao));
            if (entity is null) return NotFound();
            return View(entity);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "E")]
        public async Task<IActionResult> DeleteConfirmed(string cdtptabela, string cdsituacao)
        {
            var resp = await _service.DeleteByIdAsync((cdtptabela, cdsituacao));
            if (resp.Success) TempData["SuccessMessage"] = "Registro excluído com sucesso!";
            else TempData["ErrorMessage"] = resp.Message;
            return RedirectToAction(nameof(Index));
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