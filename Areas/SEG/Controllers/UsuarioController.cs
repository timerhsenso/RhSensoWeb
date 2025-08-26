using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using System.Reflection;

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

        // ===== Payload do token por linha =====
        public sealed record RowKeys(string Cdusuario);

        // GET: /SEG/Usuario
        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "I")]
        public IActionResult Index() => View();

        // GET: /SEG/Usuario/GetData
        // Retorno esperado pelo DataTables: { data: [...] }
        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "I")]
        public async Task<IActionResult> GetData()
        {
            var userId = User?.Identity?.Name ?? "anon";
            var result = await _service.GetDataAsync(userId);
            if (!result.Success)
                return Json(new { data = new List<object>(), error = result.Message });

            try
            {
                // Tentativa tipada (IEnumerable<Tuse1> ou projeção equivalente)
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
                            payload: new RowKeys((x.Cdusuario ?? string.Empty).Trim()),
                            purpose: "Delete",
                            userId: userId,
                            ttl: TimeSpan.FromMinutes(10))
                    });
                    return Json(new { data = dataTyped });
                }

                // Fallback usando reflexão (caso venha projeção anônima do service)
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
                            payload: new RowKeys((id ?? string.Empty).Trim()),
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
        // Recebe: x-www-form-urlencoded (id=...&ativo=true|false) ou JSON.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("UpdateAtivoPolicy")]
        [RequirePermission("SEG", "SEG_USUARIOS", "A")]
        public async Task<IActionResult> UpdateAtivo([FromForm] string id, [FromForm] bool ativo)
        {
            id = (id ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(id))
                return Json(new { success = false, message = "ID do usuário é obrigatório." });

            // Anti-double click / cooldown curto
            var userId = User?.Identity?.Name ?? "anon";
            var cooldownKey = $"SEG:Usuario:UpdateAtivo:{userId}:{id}";
            if (_cache.TryGetValue(cooldownKey, out _))
                return Json(new { success = false, message = "Aguarde um instante antes de alterar novamente." });

            _cache.Set(cooldownKey, 1, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2)
            });

            var resp = await _service.UpdateAtivoAsync(id, ativo, userId);
            return Json(resp);
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
            var resp = await _service.DeleteByTokenAsync(dto.Token, userId);
            if (!resp.Success) return StatusCode(500, resp);
            return Ok(new { success = true, message = "Excluído com sucesso." });
        }

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
                    var resp = await _service.DeleteByTokenAsync(token, userId);
                    if (resp.Success) ok++; else fail++;
                }
                catch
                {
                    fail++;
                }
            }

            return Ok(new { success = fail == 0, ok, fail });
        }

        // ===== Ações padrão (Create/Edit/Delete tradicionais) =====

        // GET: /SEG/Usuario/Create
        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "A")]
        public IActionResult Create() => View();

        // POST: /SEG/Usuario/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("SEG", "SEG_USUARIOS", "A")]
        public async Task<IActionResult> Create([Bind("Cdusuario,Dcusuario,Email_usuario,Tpusuario,Ativo")] Tuse1 usuario)
        {
            var resp = await _service.CreateAsync(usuario, ModelState);
            return Json(resp);
        }

        // POST: /SEG/Usuario/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("SEG", "SEG_USUARIOS", "A")]
        public async Task<IActionResult> Edit(string id, [Bind("Cdusuario,Dcusuario,Email_usuario,Tpusuario,Ativo")] Tuse1 usuario)
        {
            var resp = await _service.EditAsync(id, usuario, ModelState);
            return Json(resp);
        }

        // GET: /SEG/Usuario/Delete/{id}
        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "E")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var entity = await _service.GetByIdAsync(id);
            if (entity is null) return NotFound();
            return View(entity);
        }

        // POST: /SEG/Usuario/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RequirePermission("SEG", "SEG_USUARIOS", "E")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                var resp = await _service.DeleteByIdAsync(id);
                if (resp.Success)
                {
                    TempData["SuccessMessage"] = "Usuário excluído com sucesso!";
                }
                else
                {
                    TempData["ErrorMessage"] = resp.Message ?? "Usuário não encontrado.";
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
