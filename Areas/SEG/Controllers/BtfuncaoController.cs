using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Reflection;

using RhSensoWeb.Common;
using RhSensoWeb.Models;
using RhSensoWeb.Filters;                 // RequirePermission
using RhSensoWeb.Services.Security;       // IRowTokenService
using RhSensoWeb.Areas.SEG.Services;      // IBtfuncaoService
using RhSensoWeb.Support;                 // RowTokenServiceExtensions (TryUnprotect se usar aqui em algum momento)

namespace RhSensoWeb.Areas.SEG.Controllers
{
    [Area("SEG")]
    [Authorize]
    public class BtfuncaoController : Controller
    {
        private readonly IBtfuncaoService _service;
        private readonly ILogger<BtfuncaoController> _logger;
        private readonly IRowTokenService _rowToken;
        private readonly IMemoryCache _cache;

        public BtfuncaoController(
            IBtfuncaoService service,
            ILogger<BtfuncaoController> logger,
            IRowTokenService rowToken,
            IMemoryCache cache)
        {
            _service = service;
            _logger = logger;
            _rowToken = rowToken;
            _cache = cache;
        }

        // ===== Payload do token p/ ações de linha =====
        public sealed record RowKeys(string Cdsistema, string Cdfuncao, string Nmbotao);

        // GET: /SEG/Btfuncao
        // Abrir tela => Consultar (C)
        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "C")]
        public IActionResult Index() => View();

        // GET: /SEG/Btfuncao/GetData
        // Retorno para DataTables: { data: [...] }
        // Listar => Consultar (C)
        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "C")]
        public async Task<IActionResult> GetData()
        {
            var userId = User?.Identity?.Name ?? "anon";
            var (resp, list) = await _service.GetDataAsync(userId);

            if (!resp.Success)
                return Json(new { data = new List<object>(), error = resp.Message });

            try
            {
                var dataTyped = list.Select(r =>
                {
                    var key = new RowKeys(
                        (r.Cdsistema ?? string.Empty).Trim(),
                        (r.Cdfuncao ?? string.Empty).Trim(),
                        (r.Nmbotao ?? string.Empty).Trim()
                    );

                    return new
                    {
                        r.Cdsistema,
                        r.Cdfuncao,
                        r.Nmbotao,
                        r.Dcbotao,
                        r.Cdacao,
                        deleteToken = _rowToken.Protect(
                            payload: key,
                            purpose: "Delete",
                            userId: userId,
                            ttl: TimeSpan.FromMinutes(10)),
                        editToken = _rowToken.Protect(
                            payload: key,
                            purpose: "Edit",
                            userId: userId,
                            ttl: TimeSpan.FromMinutes(10))
                    };
                });

                return Json(new { data = dataTyped });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao montar retorno do GetData de Btfuncao.");
                // Em caso de falha, devolve o data bruto para não quebrar o grid
                return Json(new { data = list });
            }
        }

        // GET: /SEG/Btfuncao/SafeEdit?token=...
        // Editar => (A)
        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "A")]
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

        // POST: /SEG/Btfuncao/DeleteByToken
        // Excluir => (E)
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
                return BadRequest(ApiResponse.Fail(string.IsNullOrWhiteSpace(resp.Message)
                    ? "Não foi possível excluir o registro." : resp.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro em DeleteByToken (Btfuncao). token={Token} user={User}", dto.Token, userId);
                return StatusCode(500, ApiResponse.Fail("Erro interno ao excluir."));
            }
        }

        // POST: /SEG/Btfuncao/DeleteBatch
        // Excluir em lote => (E)
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

        // ===== Ações padrão (Create/Edit tradicionais) =====

        // GET: /SEG/Btfuncao/Create
        // Incluir => (I)
        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "I")]
        public IActionResult Create() => View();

        // POST: /SEG/Btfuncao/Create
        // Incluir => (I)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("SEG", "SEG_USUARIOS", "I")]
        public async Task<IActionResult> Create([Bind("Cdsistema,Cdfuncao,Nmbotao,Dcbotao,Cdacao")] Btfuncao entidade)
        {
            var resp = await _service.CreateAsync(entidade, ModelState);
            return Json(resp);
        }

        // GET: /SEG/Btfuncao/Edit/{cdsistema}/{cdfuncao}/{nmbotao}
        // Carrega o form de edição em modal (sem token, opcional)
        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "A")]
        public async Task<IActionResult> Edit(string cdsistema, string cdfuncao, string nmbotao)
        {
            if (string.IsNullOrWhiteSpace(cdsistema) || string.IsNullOrWhiteSpace(cdfuncao) || string.IsNullOrWhiteSpace(nmbotao))
                return BadRequest("Chave inválida.");

            var db = HttpContext.RequestServices.GetRequiredService<RhSensoWeb.Data.ApplicationDbContext>();
            var entity = await db.Btfuncao.AsNoTracking()
                                          .FirstOrDefaultAsync(x =>
                                              x.Cdsistema == cdsistema &&
                                              x.Cdfuncao == cdfuncao &&
                                              x.Nmbotao == nmbotao);

            if (entity is null)
                return NotFound();

            return View(entity);
        }

        // POST: /SEG/Btfuncao/Edit
        // Alterar => (A)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("SEG", "SEG_USUARIOS", "A")]
        public async Task<IActionResult> Edit(
            string cdsistema, string cdfuncao, string nmbotao,
            [Bind("Cdsistema,Cdfuncao,Nmbotao,Dcbotao,Cdacao")] Btfuncao entidade)
        {
            if (!string.Equals(cdsistema, entidade.Cdsistema, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(cdfuncao, entidade.Cdfuncao, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(nmbotao, entidade.Nmbotao, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse.Fail("Chave do registro alterada. Edição não permitida."));
            }

            var resp = await _service.EditAsync((cdsistema, cdfuncao, nmbotao), entidade, ModelState);
            return Json(resp);
        }

        // GET: /SEG/Btfuncao/HealthCheck
        // Consultar => (C)
        [HttpGet]
        [RequirePermission("SEG", "SEG_USUARIOS", "C")]
        public async Task<IActionResult> HealthCheck()
        {
            var resp = await _service.HealthCheckAsync();
            return Json(resp.Success
                ? new { success = true, message = resp.Message }
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
