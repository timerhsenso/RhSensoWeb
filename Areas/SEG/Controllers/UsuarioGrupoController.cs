using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RhSensoWeb.Data;
using RhSensoWeb.Models;

namespace RhSensoWeb.Areas.SEG.Controllers
{
    [Area("SEG")]
    public class UsuarioGrupoController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<UsuarioGrupoController> _logger;

        public UsuarioGrupoController(ApplicationDbContext db, ILogger<UsuarioGrupoController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ===== NOVO: Modal para ser carregado dentro do FormModal =====
        [HttpGet]
        public IActionResult Modal(string cdUsuario, string? dcUsuario)
        {
            if (string.IsNullOrWhiteSpace(cdUsuario))
                return BadRequest("cdUsuario é obrigatório.");

            ViewBag.CdUsuario = cdUsuario.Trim();
            ViewBag.DcUsuario = dcUsuario ?? string.Empty;
            return PartialView("Modal"); // Areas/SEG/Views/UsuarioGrupo/Modal.cshtml
        }

        // ===== Abertura da tela "cheia" (já existia) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Abrir(string cdUsuarioID, string dcUsuario)
        {
            if (string.IsNullOrWhiteSpace(cdUsuarioID))
                return BadRequest("cdUsuarioID é obrigatório.");

            return RedirectToAction(nameof(Index), new { cdUsuarioID, dcUsuario });
        }

        [HttpGet]
        public IActionResult Index(string cdUsuarioID, string dcUsuario)
        {
            if (string.IsNullOrWhiteSpace(cdUsuarioID))
                return BadRequest("cdUsuarioID é obrigatório.");

            ViewBag.CdUsuarioID = cdUsuarioID.Trim();
            ViewBag.DcUsuario = dcUsuario ?? string.Empty;
            return View();
        }

        // ===== DataTables / listagem JSON (já existia) =====
        [HttpGet]
        public async Task<IActionResult> GetData(string cdUsuarioID)
        {
            if (string.IsNullOrWhiteSpace(cdUsuarioID))
                return Json(new { error = "cdUsuarioID é obrigatório" });

            try
            {
                var user = cdUsuarioID.Trim().ToUpper();

                var query =
                    from u in _db.Usrh1.AsNoTracking()
                    where u.CdUsuario != null && u.CdUsuario.Trim().ToUpper() == user
                    join g in _db.Gurh1.AsNoTracking()
                        on new { u.CdGrUser, u.CdSistema } equals new { g.CdGrUser, g.CdSistema }
                        into gj
                    from g in gj.DefaultIfEmpty()
                    select new
                    {
                        Sistema = u.CdSistema,
                        Grupo = u.CdGrUser,
                        DescricaoGrupo = g != null ? g.DcGrUser : null,
                        Inicio = u.DtIniVal,
                        Fim = u.DtFimVal
                    };

                var rows = await query.ToListAsync();
                return Json(rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro em GetData para usuário {User}", cdUsuarioID);
                return Json(new { error = "Erro ao carregar dados." });
            }
        }

        // ===== Combos dinâmicos (já existia) =====
        [HttpGet]
        public async Task<IActionResult> GetSistemas()
        {
            try
            {
                var sistemas = await _db.Tsistema
                    .AsNoTracking()
                    .Where(s => s.Ativo)
                    .OrderBy(s => s.Dcsistema)
                    .Select(s => new { id = s.Cdsistema, text = s.Dcsistema })
                    .ToListAsync();

                return Json(sistemas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar sistemas");
                return Json(Array.Empty<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetGruposBySistema(string cdsistema)
        {
            if (string.IsNullOrWhiteSpace(cdsistema))
                return Json(Array.Empty<object>());

            try
            {
                var sis = cdsistema.Trim().ToUpper();

                var grupos = await _db.Gurh1
                    .AsNoTracking()
                    .Where(g => g.CdSistema != null && g.CdSistema.Trim().ToUpper() == sis)
                    .OrderBy(g => g.DcGrUser)
                    .Select(g => new { id = g.CdGrUser, text = g.DcGrUser })
                    .ToListAsync();

                return Json(grupos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar grupos do sistema {Sistema}", cdsistema);
                return Json(Array.Empty<object>());
            }
        }

        // ===== Create / Update / Delete (já existiam) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string cdUsuarioID, string cdSistema, string cdGrUser, DateTime? dtIniVal, DateTime? dtFimVal)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cdUsuarioID) ||
                    string.IsNullOrWhiteSpace(cdSistema) ||
                    string.IsNullOrWhiteSpace(cdGrUser))
                    return BadRequest("Parâmetros obrigatórios ausentes.");

                var user = cdUsuarioID.Trim();
                var sis = cdSistema.Trim();
                var grp = cdGrUser.Trim();

                if (dtIniVal.HasValue && dtFimVal.HasValue && dtFimVal.Value < dtIniVal.Value)
                    return BadRequest("Data fim não pode ser menor que a data início.");

                var grupoExiste = await _db.Gurh1
                    .AsNoTracking()
                    .AnyAsync(g =>
                        g.CdSistema != null && g.CdSistema.Trim().ToUpper() == sis.ToUpper() &&
                        g.CdGrUser != null && g.CdGrUser.Trim().ToUpper() == grp.ToUpper());

                if (!grupoExiste)
                    return BadRequest("Grupo informado não existe para o sistema selecionado.");

                var exists = await _db.Usrh1.AnyAsync(x =>
                    x.CdUsuario.Trim().ToUpper() == user.ToUpper() &&
                    x.CdSistema.Trim().ToUpper() == sis.ToUpper() &&
                    x.CdGrUser.Trim().ToUpper() == grp.ToUpper());

                if (exists)
                    return Conflict("Registro já existe para este usuário / sistema / grupo.");

                var novo = new Usrh1
                {
                    CdUsuario = user,
                    CdSistema = sis,
                    CdGrUser = grp,
                    DtIniVal = dtIniVal,
                    DtFimVal = dtFimVal
                };

                _db.Usrh1.Add(novo);
                await _db.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar vínculo Usuário-Grupo");
                return StatusCode(500, new { success = false, message = "Erro interno ao salvar." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(string cdUsuarioID, string origCdSistema, string origCdGrUser, DateTime? dtIniVal, DateTime? dtFimVal)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cdUsuarioID) ||
                    string.IsNullOrWhiteSpace(origCdSistema) ||
                    string.IsNullOrWhiteSpace(origCdGrUser))
                    return BadRequest("Parâmetros obrigatórios ausentes.");

                var user = cdUsuarioID.Trim();
                var sisOrig = origCdSistema.Trim();
                var grpOrig = origCdGrUser.Trim();

                var registro = await _db.Usrh1.FirstOrDefaultAsync(x =>
                    x.CdUsuario.Trim().ToUpper() == user.ToUpper() &&
                    x.CdSistema.Trim().ToUpper() == sisOrig.ToUpper() &&
                    x.CdGrUser.Trim().ToUpper() == grpOrig.ToUpper());

                if (registro == null)
                    return NotFound("Registro não encontrado.");

                if (dtIniVal.HasValue && dtFimVal.HasValue && dtFimVal.Value < dtIniVal.Value)
                    return BadRequest("Data fim não pode ser menor que a data início.");

                registro.DtIniVal = dtIniVal;
                registro.DtFimVal = dtFimVal;

                await _db.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar vínculo Usuário-Grupo");
                return StatusCode(500, new { success = false, message = "Erro interno ao atualizar." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string cdUsuario, string cdSistema, string cdGrUser)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cdUsuario) ||
                    string.IsNullOrWhiteSpace(cdSistema) ||
                    string.IsNullOrWhiteSpace(cdGrUser))
                    return BadRequest("Parâmetros obrigatórios ausentes.");

                var user = cdUsuario.Trim();
                var sis = cdSistema.Trim();
                var grp = cdGrUser.Trim();

                var registro = await _db.Usrh1.FirstOrDefaultAsync(x =>
                    x.CdUsuario.Trim().ToUpper() == user.ToUpper() &&
                    x.CdSistema.Trim().ToUpper() == sis.ToUpper() &&
                    x.CdGrUser.Trim().ToUpper() == grp.ToUpper());

                if (registro == null)
                    return NotFound("Registro não encontrado.");

                _db.Usrh1.Remove(registro);
                await _db.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir vínculo Usuário-Grupo");
                return StatusCode(500, new { success = false, message = "Erro interno ao excluir." });
            }
        }
    }
}
