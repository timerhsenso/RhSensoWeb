using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Reflection;

using RhSensoWeb.Common;
using RhSensoWeb.Models;
using RhSensoWeb.Filters;
using RhSensoWeb.Services.Security;
using RhSensoWeb.Areas.SYS.Services;
using RhSensoWeb.Areas.SYS.DTOs;          // <- ADICIONE ESTA LINHA
using RhSensoWeb.Data;
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

        // Payload do token (apenas ID da linha)
        private sealed record RowKeys(string Cdtptabela);

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
                if (result.Data is IEnumerable<Taux1> list)
                {
                    var dataTyped = list.Select(r =>
                    {
                        var id = (r.Cdtptabela ?? string.Empty).Trim();
                        return new
                        {
                            r.Cdtptabela,
                            r.Dctabela,
                            deleteToken = _rowToken.Protect(new RowKeys(id), "Delete", userId, TimeSpan.FromMinutes(10)),
                            editToken = _rowToken.Protect(new RowKeys(id), "Edit", userId, TimeSpan.FromMinutes(10))
                        };
                    });
                    return Json(new { data = dataTyped });
                }

                // Fallback genérico (projeção anônima)
                var raw = (result.Data as IEnumerable<object>) ?? Enumerable.Empty<object>();
                var data = raw.Select(o =>
                {
                    string id = GetString(o, "Cdtptabela")?.Trim() ?? string.Empty;
                    return new
                    {
                        Cdtptabela = id,
                        Dctabela = GetString(o, "Dctabela"),
                        deleteToken = _rowToken.Protect(new RowKeys(id), "Delete", userId, TimeSpan.FromMinutes(10)),
                        editToken = _rowToken.Protect(new RowKeys(id), "Edit", userId, TimeSpan.FromMinutes(10))
                    };
                });
                return Json(new { data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao montar retorno do GetData de Taux1.");
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
                _logger.LogError(ex, "Erro em DeleteByToken (Taux1). token={Token} user={User}", dto.Token, userId);
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
        public async Task<IActionResult> Create([Bind("Cdtptabela,Dctabela")] Taux1 entity)
            => Json(await _service.CreateAsync(entity, ModelState));

        // ====== EDIT (GET/POST) ======
        // No Controller - corrigir o Edit(GET)
        [HttpGet]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "A")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Id inválido.");

            var db = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var entity = await db.Taux1.AsNoTracking()
                                       .FirstOrDefaultAsync(x => x.Cdtptabela == id);

            if (entity is null)
                return NotFound();

            // Mapeamento manual (ou use AutoMapper)
            var dto = new Taux1Dto
            {
                Cdtptabela = entity.Cdtptabela,
                Dctabela = entity.Dctabela
            };

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "A")]
        public async Task<IActionResult> Edit(string id, [Bind("Cdtptabela,Dctabela")] Taux1 entity)
            => Json(await _service.EditAsync(id, entity, ModelState));

        // ====== DELETE tradicional (confirmação) ======
        [HttpGet]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "E")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var entity = await _service.GetByIdAsync(id);
            if (entity is null) return NotFound();
            return View(entity);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RequirePermission("RHU", "RHU_FM_TAUX1", "E")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var resp = await _service.DeleteByIdAsync(id);
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
