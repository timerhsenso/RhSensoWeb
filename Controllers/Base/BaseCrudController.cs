using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RhSensoWeb.Common;
using RhSensoWeb.Services.Base;
using RhSensoWeb.Support;

namespace RhSensoWeb.Controllers.Base
{
    /// <summary>
    /// Controller base genérico para CRUD.
    /// - Sem try/catch genérico: exceções caem no ExceptionHandlingMiddleware.
    /// - Usa ApiResponse<T> nos POSTs.
    /// - Hooks: ConfigureViewBag / ValidateModelForSave / OnBeforeSaveAsync.
    /// </summary>
    public abstract class BaseCrudController<TEntity, TKey> : Controller
        where TEntity : class, new()
    {
        protected readonly IBaseCrudService<TEntity, TKey> _service;
        protected readonly ILogger _logger;

        protected BaseCrudController(IBaseCrudService<TEntity, TKey> service, ILogger logger)
        {
            _service = service;
            _logger = logger;
        }

        #region Hooks
        protected virtual void ConfigureViewBag() { }
        protected virtual void ValidateModelForSave(TEntity model) { }
        protected virtual Task OnBeforeSaveAsync(TEntity model, bool isEdit, CancellationToken ct) => Task.CompletedTask;
        #endregion

        [HttpGet]
        public virtual IActionResult Index()
        {
            ConfigureViewBag();
            return View();
        }

        /// <summary>Endpoint para DataTables/Grids (formato: { success, data, total })</summary>
        [HttpGet]
        public virtual async Task<IActionResult> GetData(int page = 1, int pageSize = 10, CancellationToken ct = default)
        {
            var (items, total) = await _service.GetPagedAsync(page, pageSize, ct: ct);
            return Json(new { success = true, data = items, total });
        }

        [HttpGet]
        public virtual async Task<IActionResult> Details(TKey id, CancellationToken ct)
        {
            var entity = await _service.GetByIdAsync(id, ct);
            if (entity is null) return NotFound();
            return View(entity);
        }

        [HttpGet]
        public virtual IActionResult Create() => View(new TEntity());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual async Task<IActionResult> Create(TEntity model, CancellationToken ct)
        {
            ValidateModelForSave(model);
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Erros de validação.", ModelState.ToErrorsDictionary()));

            await OnBeforeSaveAsync(model, isEdit: false, ct);
            var created = await _service.CreateAsync(model, ct);
            return Json(ApiResponse<object>.Ok(created, "Registro criado com sucesso."));
        }

        [HttpGet]
        public virtual async Task<IActionResult> Edit(TKey id, CancellationToken ct)
        {
            var entity = await _service.GetByIdAsync(id, ct);
            if (entity is null) return NotFound();
            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual async Task<IActionResult> Edit(TKey id, TEntity model, CancellationToken ct)
        {
            ValidateModelForSave(model);
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Erros de validação.", ModelState.ToErrorsDictionary()));

            await OnBeforeSaveAsync(model, isEdit: true, ct);
            var updated = await _service.UpdateAsync(id, model, ct);
            return Json(ApiResponse<object>.Ok(updated, "Registro atualizado com sucesso."));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual async Task<IActionResult> Delete(TKey id, CancellationToken ct)
        {
            var ok = await _service.DeleteAsync(id, ct);
            if (!ok)
                return NotFound(ApiResponse<object>.Fail("Registro não encontrado."));

            return Json(ApiResponse.Ok("Registro excluído com sucesso."));
        }
    }
}
