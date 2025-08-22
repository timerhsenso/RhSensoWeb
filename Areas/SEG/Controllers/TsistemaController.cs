using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using RhSensoWeb.Common;
using RhSensoWeb.Models;
using RhSensoWeb.Areas.SEG.Services;

namespace RhSensoWeb.Areas.SEG.Controllers
{
    [Area("SEG")]
    [Authorize]
    public class TsistemaController : Controller
    {
        private readonly ITsistemaService _service;
        private readonly ILogger<TsistemaController> _logger;

        public TsistemaController(ITsistemaService service, ILogger<TsistemaController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> GetData()
        {
            var userId = User?.Identity?.Name ?? "anon";
            var result = await _service.GetDataAsync(userId);
            if (!result.Success)
                return Json(new { data = new List<object>(), error = result.Message });

            return Json(new { data = result.Data });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("UpdateAtivoPolicy")]
        public async Task<IActionResult> UpdateAtivo([FromForm] string id, [FromForm] bool ativo)
        {
            var userId = User?.Identity?.Name ?? "anon";
            var resp = await _service.UpdateAtivoAsync(id, ativo, userId);
            return Json(resp);
        }

        [HttpGet]
        public async Task<IActionResult> SafeEdit([FromQuery] string token)
        {
            var userId = User?.Identity?.Name ?? "anon";
            var (resp, entidade) = await _service.GetForSafeEditAsync(token, userId);
            if (!resp.Success) return Forbid();
            return View("Edit", entidade!);
        }

        public sealed class DeleteByTokenDto { public string Token { get; set; } = ""; }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteByToken([FromBody] DeleteByTokenDto dto)
        {
            var userId = User?.Identity?.Name ?? "anon";
            var resp = await _service.DeleteByTokenAsync(dto.Token, userId);
            if (!resp.Success) return StatusCode(500, resp);
            return Ok(ApiResponse.Ok("Excluído com sucesso."));
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Cdsistema,Dcsistema,Ativo")] Tsistema sistema)
        {
            var resp = await _service.CreateAsync(sistema, ModelState);
            return Json(resp);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var db = HttpContext.RequestServices.GetRequiredService<RhSensoWeb.Data.ApplicationDbContext>();
            var sistema = await db.Tsistema.FindAsync(id);
            if (sistema is null) return NotFound();
            return View(sistema);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Cdsistema,Dcsistema,Ativo")] Tsistema sistema)
        {
            var resp = await _service.EditAsync(id, sistema, ModelState);
            return Json(resp);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var db = HttpContext.RequestServices.GetRequiredService<RhSensoWeb.Data.ApplicationDbContext>();
            var sistema = await db.Tsistema.AsNoTracking().FirstOrDefaultAsync(m => m.Cdsistema == id);
            if (sistema is null) return NotFound();
            return View(sistema);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
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

        [HttpGet]
        public async Task<IActionResult> HealthCheck()
        {
            var resp = await _service.HealthCheckAsync();
            return Json(resp.Success
                ? new { success = true, message = resp.Message, totalSistemas = resp.Data }
                : new { success = false, message = resp.Message });
        }
    }
}
