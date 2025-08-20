using Microsoft.AspNetCore.Mvc;

namespace RhSensoWeb.Controllers
{
    public class DebugController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            // Só retorna a view. Toda a mágica está no Index.cshtml
            return View();
        }
    }
}
