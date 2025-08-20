using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RhSensoWeb.Data;
using RhSensoWeb.Models;
using RhSensoWeb.Services.Security;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Caching.Memory;
using RhSensoWeb.Filters; // << requer o RequirePermissionAttribute

namespace RhSensoWeb.Areas.SEG.Controllers
{
    [Area("SEG")]
    public class UsuarioController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsuarioController> _logger;
        private readonly IRowTokenService _rowToken;
        private readonly IMemoryCache _cache;

        public UsuarioController(
            ApplicationDbContext context,
            ILogger<UsuarioController> logger,
            IRowTokenService rowToken,
            IMemoryCache memoryCache)
        {
            _context = context;
            _logger = logger;
            _rowToken = rowToken;
            _cache = memoryCache;
        }

        // ===== Tipo auxiliar (payload do token) =====
        public sealed record RowKeys(string Cdusuario);

        // GET: /SEG/Usuario
        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "I")]
        public IActionResult Index() => View();

        // GET: /SEG/Usuario/GetData
        // DataTables: retorna { data: [...] }
        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "I")]
        public async Task<IActionResult> GetData()
        {
            try
            {
                var userId = User?.Identity?.Name ?? "anon";
                var rows = await _context.Tuse1
                    .AsNoTracking()
                    .OrderBy(x => x.Cdusuario)
                    .Select(x => new
                    {
                        cdusuario = x.Cdusuario,
                        dcusuario = x.Dcusuario,
                        tpusuario = x.Tpusuario,
                        email_usuario = x.Email_usuario,
                        ativo = x.Ativo
                    })
                    .ToListAsync();

                var data = rows.Select(r =>
                {
                    var id = (r.cdusuario ?? string.Empty).Trim();
                    return new
                    {
                        cdusuario = id,
                        r.dcusuario,
                        r.tpusuario,
                        r.email_usuario,
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
                _logger.LogError(ex, "Erro ao carregar dados de usuários");
                return Json(new { data = new List<object>(), error = "Erro ao carregar dados do servidor" });
            }
        }

        // POST: /SEG/Usuario/UpdateAtivo
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("UpdateAtivoPolicy")]
        [RequirePermission("SEG", "SEG_USUARIOS", "A")]
        public async Task<IActionResult> UpdateAtivo([Required] string id, bool ativo)
        {
            try
            {
                id = (id ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogWarning("UpdateAtivo com ID vazio/inválido");
                    return Json(new { success = false, message = "ID do usuário é obrigatório." });
                }

                var userId = User?.Identity?.Name ?? "anon";
                var cooldownKey = $"SEG:Usuario:UpdateAtivo:{userId}:{id}";
                if (_cache.TryGetValue(cooldownKey, out _))
                {
                    return Json(new { success = false, message = "Aguarde um instante antes de alterar novamente." });
                }
                _cache.Set(cooldownKey, 1, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2)
                });

                var entidade = await _context.Tuse1.FirstOrDefaultAsync(x => x.Cdusuario == id);
                if (entidade is null)
                    return Json(new { success = false, message = "Usuário não encontrado." });

                if (entidade.Ativo == ativo)
                    return Json(new { success = true, message = "Status já estava atualizado." });

                entidade.Ativo = ativo; // setter converte para Flativo S/N
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = ativo ? "Usuário ativado com sucesso!" : "Usuário desativado com sucesso!"
                });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concorrência ao atualizar usuário {Id}", id);
                return Json(new { success = false, message = "Registro modificado por outro usuário. Recarregue a página." });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro de banco ao atualizar usuário {Id}", id);
                return Json(new { success = false, message = "Erro ao salvar no banco de dados. Tente novamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao atualizar usuário {Id}", id);
                return Json(new { success = false, message = "Erro interno do servidor." });
            }
        }

        // ===== Edição segura com token =====

        // GET: /SEG/Usuario/SafeEdit?token=...
        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "A")]
        public async Task<IActionResult> SafeEdit([FromQuery] string token)
        {
            var userId = User?.Identity?.Name ?? "anon";
            var (keys, purpose, tokenUser) = _rowToken.Unprotect<RowKeys>(token);

            if (purpose != "Edit" || tokenUser != userId)
                return Forbid();

            var entity = await _context.Tuse1.FindAsync(keys.Cdusuario);
            if (entity is null) return NotFound();

            return View("Edit", entity);
        }

        // ===== Exclusão por token via AJAX =====

        public sealed class DeleteByTokenDto { public string Token { get; set; } = ""; }

        // POST: /SEG/Usuario/DeleteByToken
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("SEG", "SEG_USUARIOS", "E")]
        public async Task<IActionResult> DeleteByToken([FromBody] DeleteByTokenDto dto)
        {
            var userId = User?.Identity?.Name ?? "anon";
            var (keys, purpose, tokenUser) = _rowToken.Unprotect<RowKeys>(dto.Token);

            if (purpose != "Delete" || tokenUser != userId)
                return Forbid();

            try
            {
                var entity = await _context.Tuse1.FindAsync(keys.Cdusuario);
                if (entity is null)
                    return NotFound("Usuário não encontrado.");

                _context.Tuse1.Remove(entity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuário excluído: {Id}", keys.Cdusuario);
                return Ok();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro ao excluir usuário {Id}", keys.Cdusuario);
                return StatusCode(500, "Não é possível excluir: registro em uso.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao excluir usuário {Id}", keys.Cdusuario);
                return StatusCode(500, "Erro interno do servidor.");
            }
        }

        // ===== CRUD padrão =====

        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "C")]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var entity = await _context.Tuse1
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Cdusuario == id);

            if (entity == null) return NotFound();
            return View(entity);
        }

        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "I")]
        public IActionResult Create() => View(new Tuse1());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("SEG", "SEG_USUARIOS", "I")]
        public async Task<IActionResult> Create([Bind("Cdusuario,Dcusuario,Senhauser,Nmimpcche,Tpusuario,Nomatric,Cdempresa,Cdfilial,Nouser,Email_usuario,Ativo,Id,NormalizedUsername,IdFuncionario,NaoRecebeEmail")] Tuse1 model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var existente = await _context.Tuse1
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Cdusuario == model.Cdusuario);

                if (existente != null)
                {
                    ModelState.AddModelError("Cdusuario", "Já existe um usuário com este código.");
                    return View(model);
                }

                _context.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Usuário criado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar usuário {Id}", model.Cdusuario);
                ModelState.AddModelError("", "Erro ao salvar o usuário. Tente novamente.");
                return View(model);
            }
        }

        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "A")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var entity = await _context.Tuse1.FindAsync(id);
            if (entity == null) return NotFound();

            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("SEG", "SEG_USUARIOS", "A")]
        public async Task<IActionResult> Edit(string id, [Bind("Cdusuario,Dcusuario,Senhauser,Nmimpcche,Tpusuario,Nomatric,Cdempresa,Cdfilial,Nouser,Email_usuario,Ativo,Id,NormalizedUsername,IdFuncionario,NaoRecebeEmail")] Tuse1 model)
        {
            if (id != model.Cdusuario) return NotFound();
            if (!ModelState.IsValid) return View(model);

            try
            {
                _context.Update(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Usuário atualizado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!_context.Tuse1.Any(e => e.Cdusuario == model.Cdusuario))
                    return NotFound();

                _logger.LogError(ex, "Concorrência ao editar usuário {Id}", id);
                ModelState.AddModelError("", "O registro foi modificado por outro usuário. Recarregue a página.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao editar usuário {Id}", id);
                ModelState.AddModelError("", "Erro ao salvar as alterações. Tente novamente.");
                return View(model);
            }
        }

        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "E")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var entity = await _context.Tuse1
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Cdusuario == id);

            if (entity == null) return NotFound();
            return View(entity);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RequirePermission("SEG", "SEG_USUARIOS", "E")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                var entity = await _context.Tuse1.FindAsync(id);
                if (entity != null)
                {
                    _context.Tuse1.Remove(entity);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Usuário excluído com sucesso!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Usuário não encontrado.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir usuário {Id}", id);
                TempData["ErrorMessage"] = "Erro ao excluir o usuário. Verifique dependências.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /SEG/Usuario/HealthCheck
        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "C")]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                var count = await _context.Tuse1.CountAsync();
                return Json(new { success = true, message = "Conexão com banco OK", totalUsuarios = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no health check (Usuario)");
                return Json(new { success = false, message = "Erro na conexão com o banco de dados" });
            }
        }
    }
}
