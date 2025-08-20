using Microsoft.AspNetCore.Mvc;

namespace RhSensoWeb.Areas.RHU.Controllers
{
    public class HomeController : BaseRhuController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}


