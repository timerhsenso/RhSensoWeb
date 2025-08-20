using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RhSensoWeb.Data;
using RhSensoWeb.Models;
using RhSensoWeb.Services.Security;
using System.ComponentModel.DataAnnotations;

namespace RhSensoWeb.Areas.SEG.Controllers
{
    [Area("SEG")]
    public class FuncoesSistemaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FuncoesSistemaController> _logger;
        private readonly IRowTokenService _rowToken;
        private readonly IMemoryCache _cache;

        public FuncoesSistemaController(
            ApplicationDbContext context,
            ILogger<FuncoesSistemaController> logger,
            IRowTokenService rowToken,
            IMemoryCache memoryCache)
        {
            _context = context;
            _logger = logger;
            _rowToken = rowToken;
            _cache = memoryCache;
        }

        // ===================== HELPER (SELECT DE SISTEMAS) =====================
        /// <summary>
        /// Preenche ViewBag.Sistemas:
        /// - Create: apenas sistemas ativos
        /// - Edit: somente o sistema já selecionado (não permite trocar a chave)
        /// </summary>
        private async Task PopulateSistemasSelectList(string? selected = null, bool onlySelected = false)
        {
            IQueryable<Tsistema> q = _context.Set<Tsistema>().AsNoTracking();

            if (onlySelected && !string.IsNullOrWhiteSpace(selected))
                q = q.Where(s => s.Cdsistema == selected);
            else
                q = q.Where(s => s.Ativo);

            var items = await q
                .OrderBy(s => s.Dcsistema)
                .Select(s => new
                {
                    s.Cdsistema,
                    Text = s.Cdsistema + " - " + s.Dcsistema
                })
                .ToListAsync();

            ViewBag.Sistemas = new SelectList(items, "Cdsistema", "Text", selected);
        }
        // ======================================================================

        // GET: /SEG/FuncoesSistema
        [HttpGet]
        public IActionResult Index() => View();

        // GET: /SEG/FuncoesSistema/GetData
        [HttpGet]
        public async Task<IActionResult> GetData()
        {
            try
            {
                var userId = User?.Identity?.Name ?? "anon";

                var rows = await _context.Set<Fucn1>()
                    .AsNoTracking()
                    .OrderBy(x => x.CdSistema)
                    .ThenBy(x => x.CdFuncao)
                    .Select(x => new
                    {
                        cdsistema = x.CdSistema,
                        cdfuncao = x.CdFuncao,
                        dcfuncao = x.DcFuncao,
                        dcmodulo = x.DcModulo,
                        descricaomodulo = x.DescricaoModulo
                    })
                    .ToListAsync();

                var data = rows.Select(r =>
                {
                    var s = (r.cdsistema ?? string.Empty).Trim();
                    var f = (r.cdfuncao ?? string.Empty).Trim();

                    return new
                    {
                        cdsistema = s,
                        cdfuncao = f,
                        r.dcfuncao,
                        r.dcmodulo,
                        r.descricaomodulo,
                        editToken = _rowToken.Protect(new RowKeys(s, f), "Edit", userId, TimeSpan.FromMinutes(10)),
                        deleteToken = _rowToken.Protect(new RowKeys(s, f), "Delete", userId, TimeSpan.FromMinutes(10))
                    };
                });

                return Json(new { data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar dados de funções (fucn1)");
                return Json(new { data = new List<object>(), error = "Erro ao carregar dados do servidor" });
            }
        }

        // ====== Edição com token opaco (opcional) ======
        [HttpGet]
        public async Task<IActionResult> SafeEdit([FromQuery] string token)
        {
            var userId = User?.Identity?.Name ?? "anon";
            var (keys, purpose, tokenUser) = _rowToken.Unprotect<RowKeys>(token);

            if (purpose != "Edit" || tokenUser != userId)
                return Forbid();

            var s = (keys.CdSistema ?? string.Empty).Trim();
            var f = (keys.CdFuncao ?? string.Empty).Trim();

            var entidade = await _context.Set<Fucn1>()
                .FirstOrDefaultAsync(x => x.CdSistema == s && x.CdFuncao == f);

            if (entidade is null) return NotFound();

            await PopulateSistemasSelectList(entidade.CdSistema, onlySelected: true);
            return View("Edit", entidade);
        }

        public sealed class DeleteByTokenDto { public string Token { get; set; } = ""; }

        // ====== Exclusão por token (corrigido: Where + Trim nas chaves) ======
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteByToken([FromBody] DeleteByTokenDto dto)
        {
            var userId = User?.Identity?.Name ?? "anon";
            var (keys, purpose, tokenUser) = _rowToken.Unprotect<RowKeys>(dto.Token);

            if (purpose != "Delete" || tokenUser != userId)
                return Forbid();

            var s = (keys.CdSistema ?? string.Empty).Trim();
            var f = (keys.CdFuncao ?? string.Empty).Trim();

            try
            {
                var entidade = await _context.Set<Fucn1>()
                    .FirstOrDefaultAsync(x => x.CdSistema == s && x.CdFuncao == f);

                if (entidade is null)
                    return NotFound("Função não encontrada.");

                _context.Set<Fucn1>().Remove(entidade);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Função excluída: {Sistema}/{Funcao}", s, f);
                return Ok();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro ao excluir função {Sistema}/{Funcao}", s, f);
                return StatusCode(500, "Erro ao excluir. Verifique dependências.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao excluir função {Sistema}/{Funcao}", s, f);
                return StatusCode(500, "Erro interno do servidor.");
            }
        }

        // ====== CRUD ======

        [HttpGet]
        public async Task<IActionResult> Details([Required] string cdsistema, [Required] string cdfuncao)
        {
            if (string.IsNullOrWhiteSpace(cdsistema) || string.IsNullOrWhiteSpace(cdfuncao))
                return NotFound();

            var s = cdsistema.Trim();
            var f = cdfuncao.Trim();

            var entidade = await _context.Set<Fucn1>()
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.CdSistema == s && m.CdFuncao == f);

            if (entidade == null) return NotFound();
            await PopulateSistemasSelectList(entidade.CdSistema, onlySelected: true);
            return View(entidade);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateSistemasSelectList(); // só ativos
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CdSistema,CdFuncao,DcFuncao,DcModulo,DescricaoModulo")] Fucn1 entidade)
        {
            entidade.CdSistema = (entidade.CdSistema ?? string.Empty).Trim().ToUpperInvariant();
            entidade.CdFuncao = (entidade.CdFuncao ?? string.Empty).Trim().ToUpperInvariant();

            if (!ModelState.IsValid)
            {
                await PopulateSistemasSelectList(entidade.CdSistema);
                return View(entidade);
            }

            try
            {
                var existe = await _context.Set<Fucn1>()
                    .AsNoTracking()
                    .AnyAsync(x => x.CdSistema == entidade.CdSistema && x.CdFuncao == entidade.CdFuncao);

                if (existe)
                {
                    ModelState.AddModelError("CdFuncao", "Já existe essa função para o sistema informado.");
                    await PopulateSistemasSelectList(entidade.CdSistema);
                    return View(entidade);
                }

                _context.Add(entidade);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Função criada com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar função {Sistema}/{Funcao}", entidade.CdSistema, entidade.CdFuncao);
                ModelState.AddModelError("", "Erro ao salvar. Tente novamente.");
                await PopulateSistemasSelectList(entidade.CdSistema);
                return View(entidade);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit([Required] string cdsistema, [Required] string cdfuncao)
        {
            if (string.IsNullOrWhiteSpace(cdsistema) || string.IsNullOrWhiteSpace(cdfuncao))
                return NotFound();

            var s = cdsistema.Trim();
            var f = cdfuncao.Trim();

            var entidade = await _context.Set<Fucn1>()
                .FirstOrDefaultAsync(x => x.CdSistema == s && x.CdFuncao == f);

            if (entidade == null) return NotFound();

            await PopulateSistemasSelectList(entidade.CdSistema, onlySelected: true);
            return View(entidade);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Required] string cdsistema, [Required] string cdfuncao,
            [Bind("CdSistema,CdFuncao,DcFuncao,DcModulo,DescricaoModulo")] Fucn1 entidade)
        {
            var sRoute = (cdsistema ?? string.Empty).Trim();
            var fRoute = (cdfuncao ?? string.Empty).Trim();
            var sForm = (entidade.CdSistema ?? string.Empty).Trim().ToUpperInvariant(); 
            var fForm = (entidade.CdFuncao ?? string.Empty).Trim().ToUpperInvariant(); 

            if (!string.Equals(sRoute, sForm, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(fRoute, fForm, StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await PopulateSistemasSelectList(entidade.CdSistema, onlySelected: true);
                return View(entidade);
            }

            try
            {
                // Atualiza somente os campos editáveis
                var db = await _context.Set<Fucn1>()
                    .FirstOrDefaultAsync(x => x.CdSistema == sForm && x.CdFuncao == fForm);

                if (db == null) return NotFound();

                db.DcFuncao = entidade.DcFuncao;
                db.DcModulo = entidade.DcModulo;
                db.DescricaoModulo = entidade.DescricaoModulo;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Função atualizada com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var exists = await _context.Set<Fucn1>()
                    .AnyAsync(e => e.CdSistema == sForm && e.CdFuncao == fForm);

                if (!exists) return NotFound();

                _logger.LogError(ex, "Erro de concorrência ao editar função {Sistema}/{Funcao}", sForm, fForm);
                ModelState.AddModelError("", "O registro foi modificado por outro usuário. Recarregue a página.");
                await PopulateSistemasSelectList(entidade.CdSistema, onlySelected: true);
                return View(entidade);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao editar função {Sistema}/{Funcao}", sForm, fForm);
                ModelState.AddModelError("", "Erro ao salvar as alterações. Tente novamente.");
                await PopulateSistemasSelectList(entidade.CdSistema, onlySelected: true);
                return View(entidade);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete([Required] string cdsistema, [Required] string cdfuncao)
        {
            if (string.IsNullOrWhiteSpace(cdsistema) || string.IsNullOrWhiteSpace(cdfuncao))
                return NotFound();

            var s = cdsistema.Trim();
            var f = cdfuncao.Trim();

            var entidade = await _context.Set<Fucn1>()
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.CdSistema == s && m.CdFuncao == f);

            if (entidade == null) return NotFound();
            await PopulateSistemasSelectList(entidade.CdSistema, onlySelected: true);
            return View(entidade);
        }

        // ====== Exclusão tradicional (corrigida: Where + Trim nas chaves) ======
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed([Required] string cdsistema, [Required] string cdfuncao)
        {
            var s = (cdsistema ?? string.Empty).Trim();
            var f = (cdfuncao ?? string.Empty).Trim();

            try
            {
                var entidade = await _context.Set<Fucn1>()
                    .FirstOrDefaultAsync(x => x.CdSistema == s && x.CdFuncao == f);

                if (entidade != null)
                {
                    _context.Set<Fucn1>().Remove(entidade);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Função excluída com sucesso!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Função não encontrada.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir função {Sistema}/{Funcao}", s, f);
                TempData["ErrorMessage"] = "Erro ao excluir. Verifique dependências.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                var count = await _context.Set<Fucn1>().CountAsync();
                return Json(new { success = true, message = "Conexão com banco OK", totalFuncoes = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no health check (fucn1)");
                return Json(new { success = false, message = "Erro na conexão com o banco de dados" });
            }
        }

        public sealed record RowKeys(string CdSistema, string CdFuncao);
    }
}
