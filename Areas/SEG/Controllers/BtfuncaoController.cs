using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using RhSensoWeb.Data;
using RhSensoWeb.Models;

namespace RhSensoWeb.Areas.SEG.Controllers
{
    [Area("SEG")]
    public class BtfuncaoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDataProtector _protector;

        public BtfuncaoController(ApplicationDbContext context, IDataProtectionProvider dp)
        {
            _context = context;
            _protector = dp.CreateProtector("Btfuncao.DeleteToken.v1");
        }

        // GET: /SEG/Btfuncao
        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.Areas = "SEG";
            ViewBag.Views = "Btfuncao";
            ViewBag.Controller = "Btfuncao";
            ViewBag.HabilitaBtnNovo = true;
            ViewBag.HabilitaBtnExportar = true;

            ViewBag.Title = "Botões por Função";
            ViewBag.SubTitle = "Segurança";
            return View();
        }

        // GET: /SEG/Btfuncao/GetData
        [HttpGet]
        public async Task<IActionResult> GetData()
        {
            try
            {
                var data = await _context.Btfuncao
                    .AsNoTracking()
                    .Select(b => new
                    {
                        cdsistema = b.Cdsistema,
                        cdfuncao = b.Cdfuncao,
                        nmbotao = b.Nmbotao,
                        dcbotao = b.Dcbotao,
                        cdacao = b.Cdacao,
                        deleteToken = _protector.Protect($"{b.Cdsistema}|{b.Cdfuncao}|{b.Nmbotao}")
                    })
                    .ToListAsync();

                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new { error = "Erro ao carregar dados: " + ex.Message });
            }
        }

        // GET: /SEG/Btfuncao/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopularSelectsAsync(null, null);
            ViewData["IsEdit"] = false;
            return View(new Btfuncao());
        }

        // POST: /SEG/Btfuncao/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Btfuncao model)
        {
            Normalizar(model);

            if (!ModelState.IsValid)
            {
                await PopularSelectsAsync(model.Cdsistema, model.Cdfuncao);
                ViewData["IsEdit"] = false;
                return View(model);
            }

            try
            {
                _context.Btfuncao.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Registro incluído com sucesso.";
                return RedirectToAction(nameof(Edit), new { cdsistema = model.Cdsistema, cdfuncao = model.Cdfuncao, nmbotao = model.Nmbotao });
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "Conflito de concorrência. Recarregue a página.");
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, HandleDbUpdateException(ex));
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Falha ao salvar o registro.");
            }

            await PopularSelectsAsync(model.Cdsistema, model.Cdfuncao);
            ViewData["IsEdit"] = false;
            return View(model);
        }

        // GET: /SEG/Btfuncao/Edit?cdsistema=...&cdfuncao=...&nmbotao=...
        [HttpGet]
        public async Task<IActionResult> Edit(string cdsistema, string cdfuncao, string nmbotao)
        {
            cdsistema = (cdsistema ?? "").Trim().ToUpper();
            cdfuncao = (cdfuncao ?? "").Trim().ToUpper();
            nmbotao = (nmbotao ?? "").Trim().ToUpper();

            if (string.IsNullOrWhiteSpace(cdsistema) || string.IsNullOrWhiteSpace(cdfuncao) || string.IsNullOrWhiteSpace(nmbotao))
                return NotFound();

            var entity = await _context.Btfuncao.FindAsync(cdsistema, cdfuncao, nmbotao);
            if (entity == null) return NotFound();

            await PopularSelectsAsync(entity.Cdsistema, entity.Cdfuncao);
            ViewData["IsEdit"] = true;
            return View(entity);
        }

        // POST: /SEG/Btfuncao/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Btfuncao model)
        {
            // Chaves não podem mudar (PK composta)
            var cdsistema = (model.Cdsistema ?? "").Trim().ToUpper();
            var cdfuncao = (model.Cdfuncao ?? "").Trim().ToUpper();
            var nmbotao = (model.Nmbotao ?? "").Trim().ToUpper();

            var entity = await _context.Btfuncao.FindAsync(cdsistema, cdfuncao, nmbotao);
            if (entity == null) return NotFound();

            // Atualiza apenas campos editáveis
            entity.Dcbotao = (model.Dcbotao ?? "").Trim();
            entity.Cdacao = (model.Cdacao ?? "").Trim().ToUpper();

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Registro atualizado com sucesso.";
                return RedirectToAction(nameof(Edit), new { cdsistema, cdfuncao, nmbotao });
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "Conflito de concorrência. Recarregue a página.");
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, HandleDbUpdateException(ex));
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Falha ao salvar o registro.");
            }

            await PopularSelectsAsync(cdsistema, cdfuncao);
            ViewData["IsEdit"] = true;
            return View(entity);
        }

        // POST: /SEG/Btfuncao/Delete (fallback sem token)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string cdsistema, string cdfuncao, string nmbotao)
        {
            cdsistema = (cdsistema ?? "").Trim().ToUpper();
            cdfuncao = (cdfuncao ?? "").Trim().ToUpper();
            nmbotao = (nmbotao ?? "").Trim().ToUpper();

            try
            {
                var entity = await _context.Btfuncao.FindAsync(cdsistema, cdfuncao, nmbotao);
                if (entity == null)
                    return Json(new { success = false, message = "Registro não encontrado." });

                _context.Btfuncao.Remove(entity);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (DbUpdateException ex)
            {
                return Json(new { success = false, message = HandleDbUpdateException(ex) });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Erro ao excluir registro." });
            }
        }

        // POST: /SEG/Btfuncao/DeleteByToken  (preferencial)
        public class DeleteTokenDto { public string? Token { get; set; } }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteByToken([FromBody] DeleteTokenDto dto)
        {
            try
            {
                if (dto?.Token == null) return BadRequest(new { success = false, message = "Token inválido." });

                var raw = _protector.Unprotect(dto.Token);
                var parts = raw.Split('|');
                if (parts.Length != 3) return BadRequest(new { success = false, message = "Token inválido." });

                var cdsistema = (parts[0] ?? "").Trim().ToUpper();
                var cdfuncao = (parts[1] ?? "").Trim().ToUpper();
                var nmbotao = (parts[2] ?? "").Trim().ToUpper();

                var entity = await _context.Btfuncao.FindAsync(cdsistema, cdfuncao, nmbotao);
                if (entity == null) return Json(new { success = false, message = "Registro não encontrado." });

                _context.Btfuncao.Remove(entity);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (DbUpdateException ex)
            {
                return Json(new { success = false, message = HandleDbUpdateException(ex) });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Erro ao excluir registro." });
            }
        }

        // AJAX: Funções por Sistema (combo dependente)
        // GET: /SEG/Btfuncao/GetFuncoesBySistema?cdsistema=ERP
        [HttpGet]
        public async Task<IActionResult> GetFuncoesBySistema(string cdsistema)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cdsistema))
                    return Json(Array.Empty<object>());

                var sis = cdsistema.Trim().ToUpper();

                var funcoes = await _context.Fucn1
                    .AsNoTracking()
                    .Where(f => f.CdSistema.Trim().ToUpper() == sis)
                    .OrderBy(f => f.CdFuncao)
                    .Select(f => new
                    {
                        value = f.CdFuncao,
                        text = string.IsNullOrWhiteSpace(f.DcFuncao)
                                ? f.CdFuncao
                                : $"{f.CdFuncao} - {f.DcFuncao}"
                    })
                    .ToListAsync();

                return Json(funcoes);
            }
            catch
            {
                return StatusCode(500, new { error = "Erro ao carregar funções." });
            }
        }


        // ------------ Helpers ------------

        private static void Normalizar(Btfuncao m)
        {
            m.Cdsistema = (m.Cdsistema ?? "").Trim().ToUpper();
            m.Cdfuncao = (m.Cdfuncao ?? "").Trim().ToUpper();
            m.Nmbotao = (m.Nmbotao ?? "").Trim().ToUpper();

            m.Dcbotao = (m.Dcbotao ?? "").Trim();
            m.Cdacao = (m.Cdacao ?? "").Trim().ToUpper();
        }

        private async Task PopularSelectsAsync(string? cdsistema, string? cdfuncao)
        {
            // Sistemas ATIVOS
            var sistemas = await _context.Tsistema
                .AsNoTracking()
                .Where(s => s.Ativo)
                .OrderBy(s => s.Cdsistema)
                .Select(s => new SelectListItem
                {
                    Value = s.Cdsistema.Trim().ToUpper(),
                    Text = $"{s.Cdsistema.Trim().ToUpper()} - {s.Dcsistema}"
                })
                .ToListAsync();

            ViewData["Sistemas"] = sistemas;

            // Funções (se já há sistema selecionado)
            IEnumerable<SelectListItem> funcoes = Enumerable.Empty<SelectListItem>();
            var sis = (cdsistema ?? "").Trim().ToUpper();
            if (!string.IsNullOrWhiteSpace(sis))
            {
                funcoes = await _context.Fucn1
                    .AsNoTracking()
                    .Where(f => f.CdSistema.Trim().ToUpper() == sis)
                    .OrderBy(f => f.CdFuncao)
                    .Select(f => new SelectListItem
                    {
                        Value = f.CdFuncao,
                        Text = string.IsNullOrWhiteSpace(f.DcFuncao)
                               ? f.CdFuncao
                               : $"{f.CdFuncao} - {f.DcFuncao}",
                        Selected = f.CdFuncao.Trim().ToUpper() == (cdfuncao ?? "").Trim().ToUpper()
                    })
                    .ToListAsync();
            }
            ViewData["Funcoes"] = funcoes;

            // Opcional: lista de ações válidas (se quiser um select para Cdacao)
            ViewData["Acoes"] = new List<SelectListItem>
            {
                new("Criar (C)", "C"),
                new("Ler (R)",   "R"),
                new("Atualizar (U)", "U"),
                new("Excluir (D)", "D")
            };
        }

        private static string HandleDbUpdateException(DbUpdateException ex)
        {
            if (ex.InnerException is SqlException sql)
            {
                return sql.Number switch
                {
                    547 => "Não é possível excluir/alterar: existem registros dependentes.",
                    2601 or 2627 => "Registro duplicado (chave única).",
                    _ => "Erro de banco de dados."
                };
            }
            return "Erro ao persistir alterações.";
        }
    }
}
