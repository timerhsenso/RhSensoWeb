using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RhSensoWeb.Data;
using RhSensoWeb.Models;

namespace RhSensoWeb.Areas.SEG.Controllers
{
    [Area("SEG")]
    public class ConstantesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDataProtector _protector;
        private readonly ILogger<ConstantesController> _logger;

        public ConstantesController(
            ApplicationDbContext context,
            IDataProtectionProvider dp,
            ILogger<ConstantesController> logger)
        {
            _context = context;
            _protector = dp.CreateProtector("Constantes.DeleteToken.v1");
            _logger = logger;
        }

        // GET: SEG/Constantes
        public IActionResult Index()
        {
            ViewData["Title"] = "Constantes";
            ViewData["SubTitle"] = "Segurança";
            return View();
        }

        // GET: SEG/Constantes/GetData
        [HttpGet]
        public async Task<IActionResult> GetData()
        {
            try
            {
                var data = await _context.Const1
                    .AsNoTracking()
                    .Select(x => new
                    {
                        cdconstante = x.Cdconstante,
                        dcconstante = x.Dcconstante,
                        dcconteudo = x.Dcconteudo,
                        tpcampo = x.Tpcampo,
                        flalterar = x.Flalterar,
                        cdsistema = x.Cdsistema,
                        cdfuncao = x.Cdfuncao,
                        tipo = x.Tipo,
                        config = x.Config,
                        deleteToken = _protector.Protect(x.Cdconstante)
                    })
                    .ToListAsync();

                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao carregar dados das constantes");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFuncoesBySistema(string cdsistema)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cdsistema))
                    return Json(Array.Empty<object>());

                var sistema = cdsistema.Trim().ToUpper();

                var funcoes = await _context.Fucn1
                    .AsNoTracking()
                    .Where(f => f.CdSistema == sistema)
                    .OrderBy(f => f.CdFuncao)
                    .Select(f => new
                    {
                        value = f.CdFuncao,
                        text = $"{f.CdFuncao} - {f.DcFuncao ?? string.Empty}"
                    })
                    .ToListAsync();

                return Json(funcoes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao carregar funções para sistema {cdsistema}");
                return StatusCode(500, new { error = "Erro interno ao carregar funções" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Nova Constante";
            ViewData["SubTitle"] = "Segurança";
            await PopularSelectsAsync(null);
            return View(new Const1());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Const1 model)
        {
            ViewData["Title"] = "Nova Constante";
            ViewData["SubTitle"] = "Segurança";

            try
            {
                if (model == null)
                {
                    ModelState.AddModelError("", "Dados inválidos");
                    await PopularSelectsAsync(null);
                    return View(model);
                }

                Normalizar(model);

                if (!ModelState.IsValid)
                {
                    await PopularSelectsAsync(model.Cdsistema, model.Cdfuncao);
                    return View(model);
                }

                if (await _context.Const1.AnyAsync(c => c.Cdconstante == model.Cdconstante))
                {
                    ModelState.AddModelError(nameof(model.Cdconstante), "Já existe uma constante com este código");
                    await PopularSelectsAsync(model.Cdsistema, model.Cdfuncao);
                    return View(model);
                }

                if (model.Id == Guid.Empty)
                    model.Id = Guid.NewGuid();

                _context.Const1.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Constante criada com sucesso!";
                return RedirectToAction(nameof(Edit), new { area = "SEG", id = model.Cdconstante });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro de banco de dados ao criar constante");
                ModelState.AddModelError("", HandleDbUpdateException(ex));
                await PopularSelectsAsync(model?.Cdsistema, model?.Cdfuncao);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao criar constante");
                ModelState.AddModelError("", "Ocorreu um erro interno. Tente novamente.");
                await PopularSelectsAsync(model?.Cdsistema, model?.Cdfuncao);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            ViewData["Title"] = "Editar Constante";
            ViewData["SubTitle"] = "Segurança";

            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction(nameof(Index));

            try
            {
                var item = await _context.Const1.FindAsync(id);
                if (item == null)
                {
                    TempData["ErrorMessage"] = "Constante não encontrada";
                    return RedirectToAction(nameof(Index));
                }

                await PopularSelectsAsync(item.Cdsistema, item.Cdfuncao);
                return View(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao carregar constante {id} para edição");
                TempData["ErrorMessage"] = "Erro ao carregar constante para edição";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Const1 model)
        {
            ViewData["Title"] = "Editar Constante";
            ViewData["SubTitle"] = "Segurança";

            try
            {
                if (!ModelState.IsValid)
                {
                    await PopularSelectsAsync(model.Cdsistema, model.Cdfuncao);
                    return View(model);
                }

                var existing = await _context.Const1.FindAsync(id);
                if (existing == null)
                {
                    TempData["ErrorMessage"] = "Constante não encontrada";
                    return RedirectToAction(nameof(Index));
                }

                existing.Dcconstante = model.Dcconstante;
                existing.Dcconteudo = model.Dcconteudo;
                existing.Cdsistema = model.Cdsistema;
                existing.Cdfuncao = model.Cdfuncao;
                existing.Tpcampo = model.Tpcampo;
                existing.Flalterar = model.Flalterar;
                existing.Tipo = model.Tipo;
                existing.Txdescricao = model.Txdescricao;
                existing.Config = model.Config;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Alterações salvas com sucesso!";
                return RedirectToAction(nameof(Edit), new { id = model.Cdconstante });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, $"Concorrência ao editar constante {id}");
                if (!await _context.Const1.AnyAsync(e => e.Cdconstante == id))
                {
                    TempData["ErrorMessage"] = "Constante não encontrada";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", "O registro foi modificado por outro usuário. Recarregue a página e tente novamente.");
                await PopularSelectsAsync(model.Cdsistema, model.Cdfuncao);
                return View(model);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"Erro de banco ao editar constante {id}");
                ModelState.AddModelError("", HandleDbUpdateException(ex));
                await PopularSelectsAsync(model.Cdsistema, model.Cdfuncao);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro inesperado ao editar constante {id}");
                ModelState.AddModelError("", "Ocorreu um erro interno. Tente novamente.");
                await PopularSelectsAsync(model.Cdsistema, model.Cdfuncao);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return BadRequest(new { success = false, message = "Id inválido" });

                var item = await _context.Const1.FindAsync(id);
                if (item == null)
                    return NotFound(new { success = false, message = "Registro não encontrado" });

                _context.Const1.Remove(item);
                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"Erro ao deletar constante {id}");
                return StatusCode(500, new
                {
                    success = false,
                    message = HandleDbUpdateException(ex)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro inesperado ao deletar constante {id}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erro interno ao excluir"
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteByToken([FromBody] DeleteTokenDto dto)
        {
            try
            {
                if (dto?.Token == null)
                    return BadRequest(new { success = false, message = "Token inválido" });

                string id;
                try
                {
                    id = _protector.Unprotect(dto.Token);
                }
                catch
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Token inválido ou expirado"
                    });
                }

                var item = await _context.Const1.FindAsync(id);
                if (item == null)
                    return NotFound(new
                    {
                        success = false,
                        message = "Registro não encontrado"
                    });

                _context.Const1.Remove(item);
                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro ao deletar constante por token");
                return StatusCode(500, new
                {
                    success = false,
                    message = HandleDbUpdateException(ex)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao deletar constante por token");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erro interno ao excluir"
                });
            }
        }

        private string HandleDbUpdateException(DbUpdateException ex)
        {
            var sqlEx = ex.InnerException as SqlException;

            if (sqlEx != null)
            {
                switch (sqlEx.Number)
                {
                    case 547: // FK violation
                        return "Não é possível excluir: existem registros dependentes";
                    case 2601:
                    case 2627: // Unique violation
                        return "Já existe um registro com este identificador";
                    default:
                        return "Erro de banco de dados ao salvar";
                }
            }
            return "Erro ao persistir alterações";
        }

        private async Task PopularSelectsAsync(string? cdsistema, string? cdfuncaoSelecionada = null)
        {
            try
            {
                ViewData["Sistemas"] = await _context.Tsistema
                    .Where(s => s.Ativo)
                    .OrderBy(s => s.Cdsistema)
                    .Select(s => new SelectListItem
                    {
                        Value = s.Cdsistema,
                        Text = $"{s.Cdsistema} - {s.Dcsistema}"
                    })
                    .ToListAsync();

                var funcoes = new List<SelectListItem>();
                if (!string.IsNullOrWhiteSpace(cdsistema))
                {
                    funcoes = await _context.Fucn1
                        .Where(f => f.CdSistema == cdsistema.Trim().ToUpper())
                        .OrderBy(f => f.CdFuncao)
                        .Select(f => new SelectListItem
                        {
                            Value = f.CdFuncao,
                            Text = $"{f.CdFuncao} - {f.DcFuncao ?? string.Empty}",
                            Selected = f.CdFuncao == cdfuncaoSelecionada
                        })
                        .ToListAsync();
                }
                ViewData["Funcoes"] = funcoes;

                ViewData["Tipos"] = new[]
                {
                    new SelectListItem("Texto", "T"),
                    new SelectListItem("Número", "N"),
                    new SelectListItem("Data", "D"),
                    new SelectListItem("Hora", "H")
                };

                ViewData["TpCampo"] = ViewData["Tipos"];

                ViewData["FlAlterar"] = new[]
                {
                    new SelectListItem("Sim", "S"),
                    new SelectListItem("Não", "N")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar dados para selects");
                ViewData["Sistemas"] = new List<SelectListItem>();
                ViewData["Funcoes"] = new List<SelectListItem>();
                ViewData["Tipos"] = Array.Empty<SelectListItem>();
                ViewData["FlAlterar"] = Array.Empty<SelectListItem>();
            }
        }

        private void Normalizar(Const1 model)
        {
            if (model == null) return;

            model.Cdconstante = model.Cdconstante?.Trim() ?? "";
            model.Dcconstante = model.Dcconstante?.Trim() ?? "";
            model.Dcconteudo = model.Dcconteudo?.Trim();
            model.Cdsistema = model.Cdsistema?.Trim();
            model.Cdfuncao = model.Cdfuncao?.Trim();
            model.Tpcampo = model.Tpcampo?.Trim();
            model.Flalterar = model.Flalterar?.Trim();
            model.Tipo = model.Tipo?.Trim();
        }

        public class DeleteTokenDto
        {
            public string Token { get; set; }
        }
    }
}