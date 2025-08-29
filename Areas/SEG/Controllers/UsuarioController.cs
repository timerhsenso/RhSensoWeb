using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Reflection;

using RhSensoWeb.Common;
using RhSensoWeb.Common.Tokens;          // << usa RowKey genérico
using RhSensoWeb.Models;                 // Tuse1 (modelo de usuário)
using RhSensoWeb.Filters;                // RequirePermission
using RhSensoWeb.Services.Security;      // IRowTokenService
using RhSensoWeb.Areas.SEG.Services;     // IUsuarioService

namespace RhSensoWeb.Areas.SEG.Controllers
{
    [Area("SEG")]
    [Authorize]
    public class UsuarioController : Controller
    {
        private readonly IUsuarioService _service;
        private readonly ILogger<UsuarioController> _logger;
        private readonly IRowTokenService _rowToken;
        private readonly IMemoryCache _cache;

        public UsuarioController(
            IUsuarioService service,
            ILogger<UsuarioController> logger,
            IRowTokenService rowToken,
            IMemoryCache memoryCache)
        {
            _service = service;
            _logger = logger;
            _rowToken = rowToken;
            _cache = memoryCache;
        }

        // GET: /SEG/Usuario
        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "C")]
        public IActionResult Index() => View();

        // GET: /SEG/Usuario/GetData
        // Retorno esperado pelo DataTables: { data: [...] }
        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "C")]
        public async Task<IActionResult> GetData()
        {
            var userId = User?.Identity?.Name ?? "anon";
            var result = await _service.GetDataAsync(userId);
            if (!result.Success)
                return Json(new { data = new List<object>(), error = result.Message });

            try
            {
                // Tentativa tipada
                if (result.Data is IEnumerable<Tuse1> typed)
                {
                    var dataTyped = typed.Select(x => new
                    {
                        cdusuario = x.Cdusuario,
                        dcusuario = x.Dcusuario,
                        email_usuario = x.Email_usuario,
                        tpusuario = x.Tpusuario,
                        tipo_desc = (x.Tpusuario?.ToString() == "1") ? "Empregado" : "Terceiro",
                        ativo = x.Ativo,
                        token = _rowToken.Protect(
                            payload: new RowKey((x.Cdusuario ?? string.Empty).Trim()),
                            purpose: "Delete",
                            userId: userId,
                            ttl: TimeSpan.FromMinutes(10))
                    });
                    return Json(new { data = dataTyped });
                }

                // Fallback usando reflexão (caso venha projeção anônima)
                var raw = (result.Data as IEnumerable<object>) ?? Enumerable.Empty<object>();
                var data = raw.Select(r =>
                {
                    string id = GetString(r, "cdusuario") ?? GetString(r, "Cdusuario") ?? string.Empty;
                    return new
                    {
                        cdusuario = id,
                        dcusuario = GetString(r, "dcusuario") ?? GetString(r, "Dcusuario"),
                        email_usuario = GetString(r, "email_usuario") ?? GetString(r, "Email_usuario"),
                        tpusuario = GetString(r, "tpusuario") ?? GetString(r, "Tpusuario"),
                        tipo_desc = ((GetString(r, "tpusuario") ?? GetString(r, "Tpusuario")) == "1") ? "Empregado" : "Terceiro",
                        ativo = GetBool(r, "ativo") || GetBool(r, "Ativo"),
                        token = _rowToken.Protect(
                            payload: new RowKey((id ?? string.Empty).Trim()),
                            purpose: "Delete",
                            userId: userId,
                            ttl: TimeSpan.FromMinutes(10))
                    };
                });
                return Json(new { data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao montar retorno do GetData de Usuario.");
                return Json(new { data = result.Data });
            }
        }

        // POST: /SEG/Usuario/UpdateAtivo
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("UpdateAtivoPolicy")]
        [RequirePermission("SEG", "SEG_USUARIOS", "A")]
        public async Task<IActionResult> UpdateAtivo([FromForm] string id, [FromForm] bool ativo)
        {
            id = (id ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(id))
                return Json(new { success = false, message = "ID do usuário é obrigatório." });

            // Anti-double click local (se quiser manter aqui também)
            var userId = User?.Identity?.Name ?? "anon";
            var cooldownKey = $"SEG:Usuario:UpdateAtivo:{userId}:{id}";
            if (_cache.TryGetValue(cooldownKey, out _))
                return Json(new { success = false, message = "Aguarde um instante antes de alterar novamente." });

            _cache.Set(cooldownKey, 1, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2)
            });

            var resp = await _service.UpdateAtivoAsync(id, ativo, userId);

            // Detecta NO-OP pela mensagem padronizada do service
            var noop = string.Equals(resp?.Message, "Status já estava atualizado.", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(resp?.Message, "Sem alteração (já estava nesse estado).", StringComparison.OrdinalIgnoreCase);

            return Json(new
            {
                success = resp?.Success ?? false,
                message = resp?.Message ?? "Erro ao processar a solicitação.",
                noop
            });
        }



        // GET: /SEG/Usuario/SafeEdit?token=...
        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "A")]
        public async Task<IActionResult> SafeEdit([FromQuery] string token)
        {
            var userId = User?.Identity?.Name ?? "anon";
            var (resp, entidade) = await _service.GetForSafeEditAsync(token, userId);
            if (!resp.Success) return Forbid();
            return View("Edit", entidade!);
        }

        // ==== Exclusões via token ====
        public sealed class DeleteByTokenDto { public string Token { get; set; } = string.Empty; }
        public sealed class DeleteBatchDto { public List<string> Tokens { get; set; } = new(); }

        // POST: /SEG/Usuario/DeleteByToken
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("SEG", "SEG_USUARIOS", "E")]
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
                _logger.LogError(ex, "Erro em DeleteByToken (Usuario). token={Token} user={User}", dto.Token, userId);
                return StatusCode(500, ApiResponse.Fail("Erro interno ao excluir."));
            }
        }

        // POST: /SEG/Usuario/DeleteBatch
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("SEG", "SEG_USUARIOS", "E")]
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
                catch { fail++; }
            }

            var allOk = fail == 0;
            var message = allOk
                ? $"Exclusão concluída. OK {ok}."
                : $"Exclusão concluída com falhas. OK {ok} | Falhas {fail}.";

            return Ok(new { success = allOk, ok, fail, message });
        }

        // ===== CRUD tradicionais =====
        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "I")]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("SEG", "SEG_USUARIOS", "I")]
        public async Task<IActionResult> Create([Bind("Cdusuario,Dcusuario,Email_usuario,Tpusuario,Ativo")] Tuse1 usuario)
            => Json(await _service.CreateAsync(usuario, ModelState));

        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "A")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Id inválido.");

            var db = HttpContext.RequestServices.GetRequiredService<RhSensoWeb.Data.ApplicationDbContext>();
            var entity = await db.Tuse1.AsNoTracking()
                                       .FirstOrDefaultAsync(x => x.Cdusuario == id);
            if (entity is null) return NotFound();
            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("SEG", "SEG_USUARIOS", "A")]
        public async Task<IActionResult> Edit(string id, [Bind("Cdusuario,Dcusuario,Email_usuario,Tpusuario,Ativo")] Tuse1 usuario)
            => Json(await _service.EditAsync(id, usuario, ModelState));

        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "E")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var db = HttpContext.RequestServices.GetRequiredService<RhSensoWeb.Data.ApplicationDbContext>();
            var usuario = await db.Tuse1.AsNoTracking().FirstOrDefaultAsync(m => m.Cdusuario == id);
            if (usuario is null) return NotFound();
            return View(usuario);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RequirePermission("SEG", "SEG_USUARIOS", "E")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var db = HttpContext.RequestServices.GetRequiredService<RhSensoWeb.Data.ApplicationDbContext>();
            try
            {
                var usuario = await db.Tuse1.FindAsync(id);
                if (usuario != null)
                {
                    db.Tuse1.Remove(usuario);
                    await db.SaveChangesAsync();
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

        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "C")]
        public async Task<IActionResult> HealthCheck()
        {
            var resp = await _service.HealthCheckAsync();
            return Json(resp.Success
                ? new { success = true, message = resp.Message, totalUsuarios = resp.Data }
                : new { success = false, message = resp.Message });
        }

        // ===== Helpers (Reflexão segura) =====
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
            if (bool.TryParse(v?.ToString(), out var parsed)) return parsed;
            return false;
        }
    }
}
