using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RhSensoWeb.Data;
using RhSensoWeb.Models;
using RhSensoWeb.Services.Security;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;

namespace RhSensoWeb.Areas.SEG.Controllers
{
    [Area("SEG")]
    public class TsistemaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TsistemaController> _logger;
        private readonly IRowTokenService _rowToken;
        private readonly IMemoryCache _cache;

        public TsistemaController(
            ApplicationDbContext context,
            ILogger<TsistemaController> logger,
            IRowTokenService rowToken,
            IMemoryCache memoryCache) // << INJETA AQUI (na lista de parâmetros)
        {
            _context = context;
            _logger = logger;
            _rowToken = rowToken;
            _cache = memoryCache;  // << ATRIBUI AQUI
        }

        // GET: /SEG/Tsistema
        [HttpGet]
        public IActionResult Index() => View();

        // GET: /SEG/Tsistema/GetData
        // DataTables: retorna { data: [...] }
        [HttpGet]
        public async Task<IActionResult> GetData()
        {
            try
            {
                var userId = User?.Identity?.Name ?? "anon";
                var rows = await _context.Tsistema
                    .AsNoTracking()
                    .OrderBy(x => x.Cdsistema)
                    .Select(x => new
                    {
                        cdsistema = x.Cdsistema,
                        dcsistema = x.Dcsistema,
                        ativo = x.Ativo
                    })
                    .ToListAsync();

                // tokens opacos por linha (propósito + usuário + expira)
                var data = rows.Select(r =>
                {
                    var id = (r.cdsistema ?? string.Empty).Trim(); // normaliza o código
                    return new
                    {
                        cdsistema = id,
                        r.dcsistema,
                        r.ativo,
                        editToken = _rowToken.Protect(
                            payload: new RowKeys(id),
                            purpose: "Edit",
                            userId: userId,
                            ttl: TimeSpan.FromMinutes(10)),
                        deleteToken = _rowToken.Protect(
                            payload: new RowKeys(id),
                            purpose: "Delete",
                            userId: userId,
                            ttl: TimeSpan.FromMinutes(10))
                    };
                });


                return Json(new { data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar dados dos sistemas");
                return Json(new
                {
                    data = new List<object>(),
                    error = "Erro ao carregar dados do servidor"
                });
            }
        }

        // Toggle do switch (Ativo) via AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("UpdateAtivoPolicy")] // aplica a policy configurada no Program.cs
        public async Task<IActionResult> UpdateAtivo([Required] string id, bool ativo)
        {
            try
            {
                id = (id ?? string.Empty).Trim(); // normaliza ID
                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogWarning("Tentativa de atualização com ID inválido ou vazio");
                    return Json(new { success = false, message = "ID do sistema é obrigatório." });
                }

                // Cooldown por usuário + registro (evita flip-flop)
                var userId = User?.Identity?.Name ?? "anon";
                var cooldownKey = $"SEG:Tsistema:UpdateAtivo:{userId}:{id}";
                if (_cache.TryGetValue(cooldownKey, out _))
                {
                    return Json(new { success = false, message = "Aguarde um instante antes de alterar novamente." });
                }
                _cache.Set(cooldownKey, 1, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2)
                });

                var entidade = await _context.Tsistema.FirstOrDefaultAsync(x => x.Cdsistema == id);
                if (entidade == null)
                {
                    _logger.LogWarning("Sistema não encontrado: {Id}", id);
                    return Json(new { success = false, message = "Sistema não encontrado." });
                }

                if (entidade.Ativo == ativo)
                    return Json(new { success = true, message = "Status já estava atualizado." });

                entidade.Ativo = ativo;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = ativo ? "Sistema ativado com sucesso!" : "Sistema desativado com sucesso!"
                });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Erro de concorrência ao atualizar sistema {Id}", id);
                return Json(new { success = false, message = "Registro modificado por outro usuário. Recarregue a página." });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro de banco ao atualizar sistema {Id}", id);
                return Json(new { success = false, message = "Erro ao salvar no banco de dados. Tente novamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao atualizar sistema {Id}", id);
                return Json(new { success = false, message = "Erro interno do servidor." });
            }
        }


        // ====== Abertura de EDIÇÃO com token opaco ======

        // GET: /SEG/Tsistema/SafeEdit?token=...
        // Renderiza a MESMA View "Edit" sem expor o id na URL.
        [HttpGet]
        public async Task<IActionResult> SafeEdit([FromQuery] string token)
        {
            var userId = User?.Identity?.Name ?? "anon";
            var (keys, purpose, tokenUser) = _rowToken.Unprotect<RowKeys>(token);

            if (purpose != "Edit" || tokenUser != userId)
                return Forbid();

            var sistema = await _context.Tsistema.FindAsync(keys.Cdsistema);
            if (sistema is null) return NotFound();

            return View("Edit", sistema); // usa sua view Edit padrão
        }

        // ====== Exclusão com token opaco via AJAX ======

        public sealed class DeleteByTokenDto { public string Token { get; set; } = ""; }

        // POST: /SEG/Tsistema/DeleteByToken
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteByToken([FromBody] DeleteByTokenDto dto)
        {
            var userId = User?.Identity?.Name ?? "anon";
            var (keys, purpose, tokenUser) = _rowToken.Unprotect<RowKeys>(dto.Token);

            if (purpose != "Delete" || tokenUser != userId)
                return Forbid();

            try
            {
                var sistema = await _context.Tsistema.FindAsync(keys.Cdsistema);
                if (sistema is null)
                    return NotFound("Sistema não encontrado.");

                _context.Tsistema.Remove(sistema);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Sistema excluído com sucesso: {Id}", keys.Cdsistema);
                return Ok();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro ao excluir sistema {Id}", keys.Cdsistema);
                return StatusCode(500, "Erro ao excluir. Verifique dependências.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao excluir sistema {Id}", keys.Cdsistema);
                return StatusCode(500, "Erro interno do servidor.");
            }
        }

        // ====== CRUD padrão (inalterado) ======

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var sistema = await _context.Tsistema
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Cdsistema == id);

            if (sistema == null) return NotFound();
            return View(sistema);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Cdsistema,Dcsistema,Ativo")] Tsistema sistema)
        {
            if (!ModelState.IsValid) return View(sistema);

            try
            {
                var existente = await _context.Tsistema
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Cdsistema == sistema.Cdsistema);

                if (existente != null)
                {
                    ModelState.AddModelError("Cdsistema", "Já existe um sistema com este código.");
                    return View(sistema);
                }

                _context.Add(sistema);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Sistema criado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar sistema {Id}", sistema.Cdsistema);
                ModelState.AddModelError("", "Erro ao salvar o sistema. Tente novamente.");
                return View(sistema);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var sistema = await _context.Tsistema.FindAsync(id);
            if (sistema == null) return NotFound();

            return View(sistema);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Cdsistema,Dcsistema,Ativo")] Tsistema sistema)
        {
            if (id != sistema.Cdsistema) return NotFound();

            if (!ModelState.IsValid) return View(sistema);

            try
            {
                _context.Update(sistema);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Sistema atualizado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!_context.Tsistema.Any(e => e.Cdsistema == sistema.Cdsistema))
                    return NotFound();

                _logger.LogError(ex, "Erro de concorrência ao editar sistema {Id}", id);
                ModelState.AddModelError("", "O registro foi modificado por outro usuário. Recarregue a página.");
                return View(sistema);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao editar sistema {Id}", id);
                ModelState.AddModelError("", "Erro ao salvar as alterações. Tente novamente.");
                return View(sistema);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var sistema = await _context.Tsistema
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Cdsistema == id);

            if (sistema == null) return NotFound();
            return View(sistema);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                var sistema = await _context.Tsistema.FindAsync(id);
                if (sistema != null)
                {
                    _context.Tsistema.Remove(sistema);
                    await _context.SaveChangesAsync();
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

        // HealthCheck mantém
        [HttpGet]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                var count = await _context.Tsistema.CountAsync();
                return Json(new { success = true, message = "Conexão com banco OK", totalSistemas = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no health check");
                return Json(new { success = false, message = "Erro na conexão com o banco de dados" });
            }
        }



        // ====== Tipo auxiliar (payload do token) ======
        public sealed record RowKeys(string Cdsistema);
    }
}
