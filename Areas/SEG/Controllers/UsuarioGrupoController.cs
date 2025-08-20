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

        // POST vindo da lista de Usuários
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Abrir(string cdUsuarioID, string dcUsuario)
        {
            if (string.IsNullOrWhiteSpace(cdUsuarioID))
                return BadRequest("cdUsuarioID é obrigatório.");

            return RedirectToAction(nameof(Index), new { cdUsuarioID, dcUsuario });
        }

        // GET da página
        [HttpGet]
        public IActionResult Index(string cdUsuarioID, string dcUsuario)
        {
            if (string.IsNullOrWhiteSpace(cdUsuarioID))
                return BadRequest("cdUsuarioID é obrigatório.");

            ViewBag.CdUsuarioID = cdUsuarioID.Trim();
            ViewBag.DcUsuario = dcUsuario ?? string.Empty;
            return View();
        }

        // GET usado pelo DataTable
        [HttpGet]
        public async Task<IActionResult> GetData(string cdUsuarioID)
        {
            if (string.IsNullOrWhiteSpace(cdUsuarioID))
            {
                return Json(new { error = "cdUsuarioID é obrigatório" });
            }

            try
            {
                var user = cdUsuarioID.Trim().ToUpper();

                var query = from u in _db.Usrh1.AsNoTracking()
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
                _logger.LogError(ex, "Erro em GetData");
                return Json(new { error = ex.Message });
            }
        }

        // POST para incluir novo vínculo em USRH1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string cdUsuarioID, string cdSistema, string cdGrUser, DateTime? dtIniVal, DateTime? dtFimVal)
        {
            if (string.IsNullOrWhiteSpace(cdUsuarioID) ||
                string.IsNullOrWhiteSpace(cdSistema) ||
                string.IsNullOrWhiteSpace(cdGrUser))
                return BadRequest("Parâmetros obrigatórios ausentes.");

            var exists = await _db.Usrh1.AnyAsync(x =>
                x.CdUsuario.Trim().ToUpper() == cdUsuarioID.Trim().ToUpper() &&
                x.CdSistema.Trim().ToUpper() == cdSistema.Trim().ToUpper() &&
                x.CdGrUser.Trim().ToUpper() == cdGrUser.Trim().ToUpper());

            if (exists)
                return Conflict("Registro já existe para este usuário/grupo/sistema.");

            var novo = new Usrh1
            {
                CdUsuario = cdUsuarioID.Trim(),
                CdSistema = cdSistema.Trim(),
                CdGrUser = cdGrUser.Trim(),
                DtIniVal = dtIniVal,
                DtFimVal = dtFimVal
            };

            _db.Usrh1.Add(novo);
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // POST para atualizar vínculo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(string cdUsuarioID, string cdSistema, string cdGrUser,
            string origCdSistema, string origCdGrUser, DateTime? dtIniVal, DateTime? dtFimVal)
        {
            if (string.IsNullOrWhiteSpace(cdUsuarioID) ||
                string.IsNullOrWhiteSpace(cdSistema) ||
                string.IsNullOrWhiteSpace(cdGrUser) ||
                string.IsNullOrWhiteSpace(origCdSistema) ||
                string.IsNullOrWhiteSpace(origCdGrUser))
                return BadRequest("Parâmetros obrigatórios ausentes.");

            var registro = await _db.Usrh1.FirstOrDefaultAsync(x =>
                x.CdUsuario.Trim().ToUpper() == cdUsuarioID.Trim().ToUpper() &&
                x.CdSistema.Trim().ToUpper() == origCdSistema.Trim().ToUpper() &&
                x.CdGrUser.Trim().ToUpper() == origCdGrUser.Trim().ToUpper());

            if (registro == null)
                return NotFound("Registro não encontrado.");

            // Se mudou sistema/grupo, verifica se já existe
            if (cdSistema.Trim().ToUpper() != origCdSistema.Trim().ToUpper() ||
                cdGrUser.Trim().ToUpper() != origCdGrUser.Trim().ToUpper())
            {
                var exists = await _db.Usrh1.AnyAsync(x =>
                    x.CdUsuario.Trim().ToUpper() == cdUsuarioID.Trim().ToUpper() &&
                    x.CdSistema.Trim().ToUpper() == cdSistema.Trim().ToUpper() &&
                    x.CdGrUser.Trim().ToUpper() == cdGrUser.Trim().ToUpper());

                if (exists)
                    return Conflict("Já existe um registro com este sistema/grupo para o usuário.");
            }

            registro.CdSistema = cdSistema.Trim();
            registro.CdGrUser = cdGrUser.Trim();
            registro.DtIniVal = dtIniVal;
            registro.DtFimVal = dtFimVal;

            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // POST para excluir vínculo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string cdUsuario, string cdSistema, string cdGrUser)
        {
            if (string.IsNullOrWhiteSpace(cdUsuario) ||
                string.IsNullOrWhiteSpace(cdSistema) ||
                string.IsNullOrWhiteSpace(cdGrUser))
                return BadRequest("Parâmetros obrigatórios ausentes.");

            var registro = await _db.Usrh1.FirstOrDefaultAsync(x =>
                x.CdUsuario.Trim().ToUpper() == cdUsuario.Trim().ToUpper() &&
                x.CdSistema.Trim().ToUpper() == cdSistema.Trim().ToUpper() &&
                x.CdGrUser.Trim().ToUpper() == cdGrUser.Trim().ToUpper());

            if (registro == null)
                return NotFound("Registro não encontrado.");

            _db.Usrh1.Remove(registro);
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }
    }
}