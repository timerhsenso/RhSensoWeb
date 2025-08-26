using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Reflection;

using RhSensoWeb.Common;
using RhSensoWeb.Models;
using RhSensoWeb.Filters;              // RequirePermission
using RhSensoWeb.Services.Security;    // IRowTokenService
using RhSensoWeb.Areas.SEG.Services;   // ITsistemaService

namespace RhSensoWeb.Areas.SEG.Controllers
{
    [Area("SEG")]
    [Authorize]
    public class TsistemaController : Controller
    {
        private readonly ITsistemaService _service;
        private readonly ILogger<TsistemaController> _logger;
        private readonly IRowTokenService _rowToken;
        private readonly IMemoryCache _cache;

        public TsistemaController(
            ITsistemaService service,
            ILogger<TsistemaController> logger,
            IRowTokenService rowToken,
            IMemoryCache cache)
        {
            _service = service;
            _logger = logger;
            _rowToken = rowToken;
            _cache = cache;
        }

        // ===== Payload do token p/ ações de linha =====
        public sealed record RowKeys(string Cdsistema);

        // GET: /SEG/Tsistema
        // Abrir tela => Consultar (C)
        [HttpGet]
        [RequirePermission("SEG", "SEG_FM_TSISTEMA", "C")]
        public IActionResult Index()
        {
            // (Opcional) Se quiser sinalizar permissões para a View:
            // var acts = PermissionAccessHelper.GetActions(HttpContext, "SEG", "SEG_FM_TSISTEMA");
            // ViewBag.CanCreate = acts.Contains("I");
            // ViewBag.CanEdit   = acts.Contains("A");
            // ViewBag.CanDelete = acts.Contains("E");
            return View();
        }

        // GET: /SEG/Tsistema/GetData
        // Retorno para DataTables: { data: [...] }
        // Listar => Consultar (C)
        [HttpGet]
        [RequirePermission("SEG", "SEG_FM_TSISTEMA", "C")]
        public async Task<IActionResult> GetData()
        {
            var userId = User?.Identity?.Name ?? "anon";
            var result = await _service.GetDataAsync(userId);
            if (!result.Success)
                return Json(new { data = new List<object>(), error = result.Message });

            // Padroniza o shape p/ o front e inclui tokens por linha
            try
            {
                // Tenta converter para IEnumerable<Tsistema>
                var list = result.Data as IEnumerable<Tsistema>;
                if (list is not null)
                {
                    var dataTyped = list.Select(r =>
                    {
                        var id = (r.Cdsistema ?? string.Empty).Trim();
                        return new
                        {
                            // nomes seguem camelCase se a app estiver com policy padrão
                            r.Cdsistema,
                            r.Dcsistema,
                            r.Ativo,
                            // 👉 Tokens específicos por operação (compatível com tokenField = 'deleteToken')
                            deleteToken = _rowToken.Protect(
                                payload: new RowKeys(id),
                                purpose: "Delete",
                                userId: userId,
                                ttl: TimeSpan.FromMinutes(10)),
                            editToken = _rowToken.Protect(
                                payload: new RowKeys(id),
                                purpose: "Edit",
                                userId: userId,
                                ttl: TimeSpan.FromMinutes(10))
                        };
                    });
                    return Json(new { data = dataTyped });
                }

                // Fallback genérico com reflexão (caso o service retorne uma projeção)
                var raw = (result.Data as IEnumerable<object>) ?? Enumerable.Empty<object>();
                var data = raw.Select(o =>
                {
                    string id = GetString(o, "Cdsistema")?.Trim() ?? string.Empty;
                    return new
                    {
                        Cdsistema = id,
                        Dcsistema = GetString(o, "Dcsistema"),
                        Ativo = GetBool(o, "Ativo"),
                        deleteToken = _rowToken.Protect(
                            payload: new RowKeys(id),
                            purpose: "Delete",
                            userId: userId,
                            ttl: TimeSpan.FromMinutes(10)),
                        editToken = _rowToken.Protect(
                            payload: new RowKeys(id),
                            purpose: "Edit",
                            userId: userId,
                            ttl: TimeSpan.FromMinutes(10))
                    };
                });
                return Json(new { data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao montar retorno do GetData de Tsistema.");
                // Em caso de falha, devolve o data bruto para não quebrar o grid
                return Json(new { data = result.Data });
            }
        }

        // POST: /SEG/Tsistema/UpdateAtivo
        // Alterar => (A)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("UpdateAtivoPolicy")]
        [RequirePermission("SEG", "SEG_FM_TSISTEMA", "A")]
        public async Task<IActionResult> UpdateAtivo([FromForm] string id, [FromForm] bool ativo)
        {
            var userId = User?.Identity?.Name ?? "anon";

            // Anti-double click / cooldown curto
            var cooldownKey = $"SEG:Tsistema:UpdateAtivo:{userId}:{id}";
            if (_cache.TryGetValue(cooldownKey, out _))
                return Json(new { success = false, message = "Aguarde um instante antes de alterar novamente." });

            _cache.Set(cooldownKey, 1, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2)
            });

            var resp = await _service.UpdateAtivoAsync(id, ativo, userId);
            return Json(resp);
        }

        // GET: /SEG/Tsistema/SafeEdit?token=...
        // Editar => (A)
        [HttpGet]
        [RequirePermission("SEG", "SEG_FM_TSISTEMA", "A")]
        public async Task<IActionResult> SafeEdit([FromQuery] string token)
        {
            var userId = User?.Identity?.Name ?? "anon";
            var (resp, entidade) = await _service.GetForSafeEditAsync(token, userId);
            if (!resp.Success) return Forbid();
            return View("Edit", entidade!);
        }

        // DTOs padronizados
        public sealed class DeleteByTokenDto { public string Token { get; set; } = string.Empty; }
        public sealed class DeleteBatchDto { public List<string> Tokens { get; set; } = new(); }

        // POST: /SEG/Tsistema/DeleteByToken
        // Excluir => (E)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("SEG", "SEG_FM_TSISTEMA", "E")]
        public async Task<IActionResult> DeleteByToken([FromBody] DeleteByTokenDto dto)
        {
            var userId = User?.Identity?.Name ?? "anon";
            try
            {
                var resp = await _service.DeleteByTokenAsync(dto.Token, userId);
                if (resp.Success)
                    return Ok(ApiResponse.Ok("Excluído com sucesso."));

                // mapeia erro conhecido do serviço para mensagem amigável
                var msg = string.IsNullOrWhiteSpace(resp.Message)
                    ? "Não foi possível excluir o registro."
                    : resp.Message;

                return BadRequest(ApiResponse.Fail(msg));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro em DeleteByToken (Tsistema). token={Token} user={User}", dto.Token, userId);
                return StatusCode(500, ApiResponse.Fail("Erro interno ao excluir."));
            }
        }

        // POST: /SEG/Tsistema/DeleteBatch
        // Excluir em lote => (E)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("SEG", "SEG_FM_TSISTEMA", "E")]
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

            return Ok(new
            {
                success = allOk,
                ok,
                fail,
                message
            });
        }

        // ===== Ações padrão (Create/Edit/Delete tradicionais) =====

        // GET: /SEG/Tsistema/Create
        // Incluir => (I)
        [HttpGet]
        [RequirePermission("SEG", "SEG_FM_TSISTEMA", "I")]
        public IActionResult Create() => View();

        // POST: /SEG/Tsistema/Create
        // Incluir => (I)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("SEG", "SEG_FM_TSISTEMA", "I")]
        public async Task<IActionResult> Create([Bind("Cdsistema,Dcsistema,Ativo")] Tsistema sistema)
        {
            var resp = await _service.CreateAsync(sistema, ModelState);
            return Json(resp);
        }

        // POST: /SEG/Tsistema/Edit/{id}
        // Alterar => (A)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("SEG", "SEG_FM_TSISTEMA", "A")]
        public async Task<IActionResult> Edit(string id, [Bind("Cdsistema,Dcsistema,Ativo")] Tsistema sistema)
        {
            var resp = await _service.EditAsync(id, sistema, ModelState);
            return Json(resp);
        }

        // GET: /SEG/Tsistema/Delete/{id} (confirmação)
        // Excluir => (E)
        [HttpGet]
        [RequirePermission("SEG", "SEG_FM_TSISTEMA", "E")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var db = HttpContext.RequestServices.GetRequiredService<RhSensoWeb.Data.ApplicationDbContext>();
            var sistema = await db.Tsistema.AsNoTracking().FirstOrDefaultAsync(m => m.Cdsistema == id);
            if (sistema is null) return NotFound();
            return View(sistema);
        }

        // POST: /SEG/Tsistema/Delete/{id} (confirmação)
        // Excluir => (E)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RequirePermission("SEG", "SEG_FM_TSISTEMA", "E")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var db = HttpContext.RequestServices.GetRequiredService<RhSensoWeb.Data.ApplicationDbContext>();

            try
            {
                var sistema = await db.Tsistema.FindAsync(id);
                if (sistema != null)
                {
                    db.Tsistema.Remove(sistema);
                    await db.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Sistema excluído com sucesso!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Sistema não encontrado.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir sistema {Id}", id);
                TempData["ErrorMessage"] = "Erro ao excluir o sistema. Verifique dependências.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /SEG/Tsistema/HealthCheck
        // Consultar => (C)
        [HttpGet]
        [RequirePermission("SEG", "SEG_FM_TSISTEMA", "C")]
        public async Task<IActionResult> HealthCheck()
        {
            var resp = await _service.HealthCheckAsync();
            return Json(resp.Success
                ? new { success = true, message = resp.Message, totalSistemas = resp.Data }
                : new { success = false, message = resp.Message });
        }

        // ===== Helpers =====
        private static string? GetString(object obj, string prop)
        {
            var p = obj.GetType().GetProperty(prop, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            return p?.GetValue(obj)?.ToString();
        }

        private static bool GetBool(object obj, string prop)
        {
            var p = obj.GetType().GetProperty(prop, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            var v = p?.GetValue(obj);
            if (v is null) return false;
            if (v is bool b) return b;
            if (v is int i) return i != 0;
            if (bool.TryParse(v.ToString(), out var parsed)) return parsed;
            return false;
        }


        // GET: /SEG/Tsistema/Edit/{id}
        // Carrega o formulário de edição no modal (DataTables → AppModal.form)
        [HttpGet]
        [RequirePermission("SEG", "SEG_FM_TSISTEMA", "A")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Id inválido.");

            var db = HttpContext.RequestServices.GetRequiredService<RhSensoWeb.Data.ApplicationDbContext>();
            var entity = await db.Tsistema.AsNoTracking()
                                          .FirstOrDefaultAsync(x => x.Cdsistema == id);

            if (entity is null)
                return NotFound();

            return View(entity); // retorna a View "Edit"
        }

    }
}
