using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using RhSensoWeb.Areas.SYS.Taux1.Services;
using RhSensoWeb.Areas.SYS.Taux1.DTOs;
using RhSensoWeb.Common; // OkResp/BadResp/InvalidModel

namespace RhSensoWeb.Areas.SYS.Taux1.Controllers
{
    [Area("SYS")]
    public class Taux1Controller : Controller
    {
        private readonly ITaux1Service _service;

        public Taux1Controller(ITaux1Service service)
        {
            _service = service;
        }

        // View principal (grid)
        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.Areas = "SYS";
            ViewBag.Views = "Taux1";
            ViewBag.Controller = "Taux1";
            ViewBag.Title = "Tabelas Auxiliares";
            ViewBag.SubTitle = "Comum";
            ViewBag.HabilitaBtnNovo = true;
            ViewBag.HabilitaBtnExportar = true;
            ViewBag.gridName = "#gridTaux1";
            return View();
        }

        // DataTables (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetData()
        {
            var draw = int.TryParse(Request.Query["draw"], out var d) ? d : 1;
            var start = int.TryParse(Request.Query["start"], out var s) ? s : 0;
            var len = int.TryParse(Request.Query["length"], out var l) ? l : 10;
            var search = Request.Query["search[value]"].ToString();

            var orderColIndex = Request.Query["order[0][column]"].ToString();
            var orderColName = Request.Query[$"columns[{orderColIndex}][data]"].ToString();
            var orderDir = Request.Query["order[0][dir]"].ToString();

            var req = new DataTableRequest
            {
                Draw = draw,
                Start = start,
                Length = len,
                Search = search,
                OrderColumn = string.IsNullOrWhiteSpace(orderColName) ? "Cdtptabela" : orderColName,
                OrderDir = string.IsNullOrWhiteSpace(orderDir) ? "asc" : orderDir
            };

            var (items, total, filtered) = await _service.GetPageAsync(req);

            var resp = new DataTableResponse<Taux1Dto>
            {
                Draw = draw,
                RecordsTotal = total,
                RecordsFiltered = filtered,
                Data = items
            };
            return Json(resp);
        }

        // CREATE (form em modal)
        [HttpGet]
        public IActionResult Create()
        {
            return PartialView("Create", new Taux1Dto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Taux1Dto dto)
        {
            if (!ModelState.IsValid) return this.InvalidModel();
            await _service.CreateAsync(dto);
            return this.OkResp("Registro criado com sucesso.");
        }

        // EDIT (carrega form)
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var dto = await _service.GetAsync(id);
            if (dto == null) return NotFound();
            return PartialView("Edit", dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Taux1Dto dto)
        {
            if (!ModelState.IsValid) return this.InvalidModel();
            await _service.UpdateAsync(id, dto);
            return this.OkResp("Registro atualizado com sucesso.");
        }

        // DELETE (single)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            await _service.DeleteAsync(id);
            return this.OkResp("Registro excluído com sucesso.");
        }

        // DELETE em lote (ids[])
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBatch([FromForm] string[] ids)
        {
            if (ids == null || ids.Length == 0) return this.BadResp("Nenhum registro selecionado.");
            await _service.DeleteBatchAsync(ids);
            return this.OkResp("Registros excluídos com sucesso.");
        }
    }
}
