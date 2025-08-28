using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using System.Reflection;
using RhSensoWeb.Common;
using RhSensoWeb.Models;                   // Taux1 (modelo)
using RhSensoWeb.Filters;                  // RequirePermission
using RhSensoWeb.Services.Security;        // IRowTokenService
using RhSensoWeb.Areas.SYS.Services;       // ITaux1Service

namespace RhSensoWeb.Areas.SYS.Controllers
{
    [Area("SYS")]
    [Authorize]
    public class Taux1Controller : Controller
    {
        private readonly ITaux1Service _service;
        private readonly ILogger<Taux1Controller> _logger;
        private readonly IRowTokenService _rowToken;
        private readonly IMemoryCache _cache;

        public Taux1Controller(
            ITaux1Service service,
            ILogger<Taux1Controller> logger,
            IRowTokenService rowToken,
            IMemoryCache cache)
        {
            _service = service;
            _logger = logger;
            _rowToken = rowToken;
            _cache = cache;
        }

        // ===== Payload do token p/ ações de linha =====
        public sealed record RowKeys(string Cdtptabela);

        // GET: /SYS/Taux1
        // Abrir tela => Consultar (C)
        [HttpGet]
        [RequirePermission("SYS", "SYS_TAUX1", "C")]
        public IActionResult Index() => View();

        // GET: /SYS/Taux1/GetData
        // Retorno para DataTables: { data: [...] }
        // Listar => Consultar (C)
        [HttpGet]
        [RequirePermission("SYS", "SYS_TAUX1", "C")]
        public async Task<IActionResult> GetData()
        {
            var userId = User?.Identity?.Name ?? "anon";
            var result = await _service.GetDataAsync(userId);
            if (!result.Success)
                return Json(new { data = new List<object>(), error = result.Message });

            try
            {
                // Tenta converter para IEnumerable<Taux1>
                if (result.Data is IEnumerable<Taux1> list)
                {
                    var dataTyped = list.Select(r =>
                    {
                        var id = (r.Cdtptabela ?? string.Empty).Trim();
                        return new
                        {
                            r.Cdtptabela,
                            r.Dctabela,
                            // 👉 Tokens específicos por operação
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

                // Fallback genérico com reflexão (caso o service retorne projeção)
                var raw = (result.Data as IEnumerable<object>) ?? Enumerable.Empty<object>();
                var data = raw.Select(o =>
                {
                    string id = GetString(o, "Cdtptabela")?.Trim() ?? string.Empty;
                    return new
                    {
                        Cdtptabela = id,
                        Dctabela = GetString(o, "Dctabela"),
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
                _logger.LogError(ex, "Falha ao montar retorno do GetData de Taux1.");
                // Em caso de falha, devolve o data bruto para não quebrar o grid
                return Json(new { data = result.Data });
            }
        }

        // GET: /SYS/Taux1/SafeEdit?token=...
        // Editar => (A)
        [HttpGet]
        [RequirePermission("SYS", "SYS_TAUX1", "A")]
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

        // POST: /SYS/Taux1/DeleteByToken
        // Excluir => (E)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("SYS", "SYS_TAUX1", "E")]
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
                _logger.LogError(ex, "Erro em DeleteByToken (Taux1). token={Token} user={User}", dto.Token, userId);
                return StatusCode(500, ApiResponse.Fail("Erro interno ao excluir."));
            }
        }

        // POST: /SYS/Taux1/DeleteBatch
        // Excluir em lote => (E)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("SYS", "SYS_TAUX1", "E")]
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

        // GET: /SYS/Taux1/Create
        // Incluir => (I)
        [HttpGet]
        [RequirePermission("SYS", "SYS_TAUX1", "I")]
        public IActionResult Create() => View();

        // POST: /SYS/Taux1/Create
        // Incluir => (I)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("SYS", "SYS_TAUX1", "I")]
        public async Task<IActionResult> Create([Bind("Cdtptabela,Dctabela")] Taux1 entidade)
        {
            var resp = await _service.CreateAsync(entidade, ModelState);
            return Json(resp);
        }

        // GET: /SYS/Taux1/Edit/{id}
        // Carrega o formulário de edição no modal (DataTables → AppModal.form)
        [HttpGet]
        [RequirePermission("SYS", "SYS_TAUX1", "A")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Id inválido.");

            var db = HttpContext.RequestServices.GetRequiredService<RhSensoWeb.Data.ApplicationDbContext>();
            var entity = await db.Taux1.AsNoTracking()
                                       .FirstOrDefaultAsync(x => x.Cdtptabela == id);

            if (entity is null)
                return NotFound();

            return View(entity); // retorna a View "Edit"
        }

        // POST: /SYS/Taux1/Edit/{id}
        // Alterar => (A)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("SYS", "SYS_TAUX1", "A")]
        public async Task<IActionResult> Edit(string id, [Bind("Cdtptabela,Dctabela")] Taux1 entidade)
        {
            var resp = await _service.EditAsync(id, entidade, ModelState);
            return Json(resp);
        }

        // GET: /SYS/Taux1/Delete/{id} (confirmação)
        // Excluir => (E)
        [HttpGet]
        [RequirePermission("SYS", "SYS_TAUX1", "E")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var db = HttpContext.RequestServices.GetRequiredService<RhSensoWeb.Data.ApplicationDbContext>();
            var e = await db.Taux1.AsNoTracking().FirstOrDefaultAsync(m => m.Cdtptabela == id);
            if (e is null) return NotFound();
            return View(e);
        }

        // POST: /SYS/Taux1/Delete/{id} (confirmação)
        // Excluir => (E)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RequirePermission("SYS", "SYS_TAUX1", "E")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                var resp = await _service.DeleteByIdAsync(id);
                if (resp.Success)
                    TempData["SuccessMessage"] = "Registro excluído com sucesso!";
                else
                    TempData["ErrorMessage"] = resp.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir Taux1 {Id}", id);
                TempData["ErrorMessage"] = "Erro ao excluir o registro. Verifique dependências.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /SYS/Taux1/HealthCheck
        // Consultar => (C)
        [HttpGet]
        [RequirePermission("SYS", "SYS_TAUX1", "C")]
        public async Task<IActionResult> HealthCheck()
        {
            var resp = await _service.HealthCheckAsync();
            return Json(resp.Success
                ? new { success = true, message = resp.Message, totalRegistros = resp.Data }
                : new { success = false, message = resp.Message });
        }

        // ===== Helpers =====
        private static string? GetString(object obj, string prop)
        {
            var p = obj.GetType().GetProperty(prop, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            return p?.GetValue(obj)?.ToString();
        }
    }
}
