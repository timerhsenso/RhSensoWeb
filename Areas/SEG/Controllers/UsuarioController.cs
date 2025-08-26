using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Caching.Memory;

using RhSensoWeb.Data;
using RhSensoWeb.Models;
using RhSensoWeb.Filters;              // RequirePermission
using RhSensoWeb.Services.Security;    // IRowTokenService

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

        // ===== Payload do token =====
        public sealed record RowKeys(string Cdusuario);

        // GET: /SEG/Usuario
        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "I")]
        public IActionResult Index() => View();

        // GET: /SEG/Usuario/GetData
        // Retorno para DataTables: { data: [...] }
        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "I")]
        public async Task<IActionResult> GetData()
        {
            try
            {
                var userId = User?.Identity?.Name ?? "anon";

                // Busque os dados necessários para a grid
                var rows = await _context.Tuse1
                    .AsNoTracking()
                    .OrderBy(x => x.Cdusuario)
                    .Select(x => new
                    {
                        cdusuario = x.Cdusuario,
                        dcusuario = x.Dcusuario,
                        email_usuario = x.Email_usuario,
                        tpusuario = x.Tpusuario,
                        ativo = x.Ativo
                    })
                    .ToListAsync();

                // Anexe o token esperado pelo front (nome do campo "token")
                var data = rows.Select(r =>
                {
                    var id = (r.cdusuario ?? string.Empty).Trim();

                    var delToken = _rowToken.Protect(
                        payload: new RowKeys(id),
                        purpose: "Delete",
                        userId: userId,
                        ttl: TimeSpan.FromMinutes(10));

                    // Retorne exatamente o que a view espera
                    return new
                    {
                        r.cdusuario,
                        r.dcusuario,
                        r.email_usuario,          // << agora existe no JSON
                        r.tpusuario,
                        tipo_desc = (r.tpusuario?.ToString() == "1") ? "Empregado" : "Terceiro", // << AQUI
                        r.ativo,
                        token = delToken        // << importante para o tokenField: 'token'
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
        // Recebe x-www-form-urlencoded: id=...&ativo=true|false
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

                // Anti-bounce simples
                var userId = User?.Identity?.Name ?? "anon";
                var cooldownKey = $"SEG:Usuario:UpdateAtivo:{userId}:{id}";
                if (_cache.TryGetValue(cooldownKey, out _))
                    return Json(new { success = false, message = "Aguarde um instante antes de alterar novamente." });

                _cache.Set(cooldownKey, 1, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2)
                });

                var entidade = await _context.Tuse1.FirstOrDefaultAsync(x => x.Cdusuario == id);
                if (entidade is null)
                    return Json(new { success = false, message = "Usuário não encontrado." });

                if (entidade.Ativo == ativo)
                    return Json(new { success = true, message = "Status já estava atualizado." });

                // A propriedade Ativo deve aplicar conversão para Flativ internamente (conforme seu modelo)
                entidade.Ativo = ativo;

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Atualizado com sucesso." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar status de usuário {Id}", id);
                return Json(new { success = false, message = "Erro ao atualizar." });
            }
        }

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

        // ==== Exclusão por token (1 a 1) ====

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

        // ==== Exclusão em lote por token (opcional, para bulk delete do grid) ====

        public sealed class DeleteBatchDto { public List<string> Tokens { get; set; } = new(); }

        // POST: /SEG/Usuario/DeleteBatch
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("SEG", "SEG_USUARIOS", "E")]
        public async Task<IActionResult> DeleteBatch([FromBody] DeleteBatchDto dto)
        {
            if (dto?.Tokens == null || dto.Tokens.Count == 0)
                return BadRequest(new { success = false, message = "Nenhum token informado." });

            var userId = User?.Identity?.Name ?? "anon";
            int ok = 0, fail = 0;

            foreach (var token in dto.Tokens)
            {
                try
                {
                    var (keys, purpose, tokenUser) = _rowToken.Unprotect<RowKeys>(token);
                    if (purpose != "Delete" || tokenUser != userId) { fail++; continue; }

                    var entity = await _context.Tuse1.FindAsync(keys.Cdusuario);
                    if (entity is null) { fail++; continue; }

                    _context.Tuse1.Remove(entity);
                    ok++;
                }
                catch
                {
                    fail++;
                }
            }

            if (ok > 0)
            {
                try { await _context.SaveChangesAsync(); }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao confirmar exclusão em lote de usuários");
                    return StatusCode(500, new { success = false, message = "Erro ao gravar exclusões." });
                }
            }

            return Ok(new { success = fail == 0, ok, fail });
        }

        // ==== Ações padrão (opcionais para navegação direta) ====

        // GET: /SEG/Usuario/Details/{id}
        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "C")]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var entity = await _context.Tuse1.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Cdusuario == id);

            if (entity == null) return NotFound();
            return View(entity);
        }

        // GET: /SEG/Usuario/Edit/{id}
        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "A")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var entity = await _context.Tuse1.FindAsync(id);
            if (entity == null) return NotFound();
            return View(entity);
        }

        // GET: /SEG/Usuario/Delete/{id}
        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "E")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var entity = await _context.Tuse1.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Cdusuario == id);

            if (entity == null) return NotFound();
            return View(entity);
        }

        // POST: /SEG/Usuario/Delete (form padrão)
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
