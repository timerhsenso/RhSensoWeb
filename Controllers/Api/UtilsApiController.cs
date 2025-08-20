using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RhSensoWeb.Data;

namespace RhSensoWeb.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class UtilsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UtilsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> RecarregarConstantes()
        {
            // Carrega as constantes novamente do banco e armazena na session
            var constantes = await _context.Const1.ToListAsync();
            HttpContext.Session.SetObject("Constantes", constantes);
            return Ok(new { success = true, message = "Constantes recarregadas com sucesso!" });
        }

        [HttpPost]
        public IActionResult LimparTudo()
        {
            // Limpa sessão e cookies do usuário
            HttpContext.Session.Clear();
            foreach (var cookie in Request.Cookies.Keys)
                Response.Cookies.Delete(cookie);
            return Ok(new { success = true, message = "Sessão e cookies limpos!" });
        }

        [HttpPost]
        public async Task<IActionResult> RecarregarHabilitacoes()
        {
            // Exemplo: recarregue a session dos UserPermissions conforme seu login
            // Repita aqui sua lógica do login para reprocessar permissões!
            // (abaixo é apenas exemplo vazio)
            //var user = ...; var userGroups = ...; var userPermissions = ...; HttpContext.Session.SetObject("UserPermissions", userPermissions);

            return Ok(new { success = true, message = "Habilitações recarregadas com sucesso!" });
        }
    }
}
