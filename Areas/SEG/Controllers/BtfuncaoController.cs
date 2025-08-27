using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using RhSensoWeb.Data;
using RhSensoWeb.Models;

namespace RhSensoWeb.Areas.SEG.Controllers
{
    [Area("SEG")]
    [Authorize] // ajuste se usar políticas específicas
    public class BtfuncaoController : Controller
    {
        private readonly ApplicationDbContext _db;

        public BtfuncaoController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: SEG/Btfuncao
        public IActionResult Index()
        {
            // A view deve carregar DataTable via AJAX em /SEG/Btfuncao/GetData
            return View();
        }

        // GET: SEG/Btfuncao/GetData
        // Endpoint para DataTables (retorna todos os botões/funções)
        [HttpGet]
        public async Task<IActionResult> GetData()
        {
            // Projeção simples; ajuste nomes de propriedades conforme seu Model
            var query = from b in _db.Btfuncao
                        join s in _db.Tsistema on b.Cdsistema equals s.Cdsistema into sys
                        from s in sys.DefaultIfEmpty()
                        join f in _db.Fucn1 on new { b.Cdsistema, b.Cdfuncao } equals new { f.Cdsistema, f.Cdfuncao } into fun
                        from f in fun.DefaultIfEmpty()
                        select new
                        {
                            b.Cdsistema,
                            Sistema = s != null ? s.Dcsistema : "",
                            b.Cdfuncao,
                            Funcao = f != null ? f.Dcfuncao : "",
                            b.Nmbotcao,
                            b.Dcacao,      // se existir no seu modelo
                            b.Ativo        // se existir no seu modelo
                        };

            var data = await query.AsNoTracking().ToListAsync();
            return Json(new { data });
        }

        // GET: SEG/Btfuncao/Create
        public async Task<IActionResult> Create()
        {
            await CarregarCombosAsync();
            return View();
        }

        // POST: SEG/Btfuncao/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Btfuncao model)
        {
            if (!ModelState.IsValid)
            {
                await CarregarCombosAsync(model.Cdsistema);
                return View(model);
            }

            var exists = await _db.Btfuncao
                .AnyAsync(x => x.Cdsistema == model.Cdsistema
                            && x.Cdfuncao == model.Cdfuncao
                            && x.Nmbotcao == model.Nmbotcao);

            if (exists)
            {
                ModelState.AddModelError(string.Empty, "Já existe um registro com esta combinação de (Sistema, Função, Botão).");
                await CarregarCombosAsync(model.Cdsistema);
                return View(model);
            }

            _db.Btfuncao.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: SEG/Btfuncao/Edit/5?cdfuncao=10&nmbotcao=BTN_SALVAR
        public async Task<IActionResult> Edit(int cdsistema, int cdfuncao, string nmbotcao)
        {
            var entity = await _db.Btfuncao
                .FirstOrDefaultAsync(x => x.Cdsistema == cdsistema
                                       && x.Cdfuncao == cdfuncao
                                       && x.Nmbotcao == nmbotcao);
            if (entity == null) return NotFound();

            await CarregarCombosAsync(entity.Cdsistema);
            return View(entity);
        }

        // POST: SEG/Btfuncao/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int cdsistema, int cdfuncao, string nmbotcao, Btfuncao model)
        {
            // Garante edição do mesmo registro
            if (cdsistema != model.Cdsistema || cdfuncao != model.Cdfuncao || nmbotcao != model.Nmbotcao)
            {
                return BadRequest("Chave do registro foi alterada. Edição não permitida.");
            }

            if (!ModelState.IsValid)
            {
                await CarregarCombosAsync(model.Cdsistema);
                return View(model);
            }

            var entity = await _db.Btfuncao
                .FirstOrDefaultAsync(x => x.Cdsistema == cdsistema
                                       && x.Cdfuncao == cdfuncao
                                       && x.Nmbotcao == nmbotcao);
            if (entity == null) return NotFound();

            // Atualize apenas os campos editáveis (evite sobrescrever chaves)
            entity.Dcacao = model.Dcacao; // se existir
            entity.Ativo = model.Ativo;  // se existir
            // adicione outros campos não-chave aqui

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: SEG/Btfuncao/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int cdsistema, int cdfuncao, string nmbotcao)
        {
            var entity = await _db.Btfuncao
                .FirstOrDefaultAsync(x => x.Cdsistema == cdsistema
                                       && x.Cdfuncao == cdfuncao
                                       && x.Nmbotcao == nmbotcao);
            if (entity == null) return NotFound();

            _db.Btfuncao.Remove(entity);
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // Opcional: endpoint para combos dependentes (Funções por Sistema)
        [HttpGet]
        public async Task<IActionResult> GetFuncoesBySistema(int cdsistema)
        {
            var funcoes = await _db.Fucn1
                .Where(f => f.Cdsistema == cdsistema)
                .OrderBy(f => f.Dcfuncao)
                .Select(f => new SelectListItem
                {
                    Value = f.Cdfuncao.ToString(),
                    Text = f.Dcfuncao
                })
                .ToListAsync();

            return Json(funcoes);
        }

        #region Helpers
        private async Task CarregarCombosAsync(int? cdsistemaSelecionado = null)
        {
            // Sistemas
            var sistemas = await _db.Tsistema
                .OrderBy(s => s.Dcsistema)
                .Select(s => new SelectListItem
                {
                    Value = s.Cdsistema.ToString(),
                    Text = s.Dcsistema
                })
                .ToListAsync();

            ViewData["Sistemas"] = sistemas;

            // Funções (se houver sistema selecionado)
            var funcoes = Enumerable.Empty<SelectListItem>();
            if (cdsistemaSelecionado.HasValue)
            {
                funcoes = await _db.Fucn1
                    .Where(f => f.Cdsistema == cdsistemaSelecionado.Value)
                    .OrderBy(f => f.Dcfuncao)
                    .Select(f => new SelectListItem
                    {
                        Value = f.Cdfuncao.ToString(),
                        Text = f.Dcfuncao
                    })
                    .ToListAsync();
            }
            ViewData["Funcoes"] = funcoes;

            // Ações possíveis do botão (se sua tabela tiver)
            // Ajuste se existir uma tabela/enum de ações
            ViewData["Acoes"] = new[]
            {
                new SelectListItem { Value = "VISUALIZAR", Text = "Visualizar" },
                new SelectListItem { Value = "CRIAR",      Text = "Criar"      },
                new SelectListItem { Value = "EDITAR",     Text = "Editar"     },
                new SelectListItem { Value = "EXCLUIR",    Text = "Excluir"    }
            };
        }
        #endregion
    }
}
