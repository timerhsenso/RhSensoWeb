using Microsoft.AspNetCore.Mvc;
using RhSensoWeb.Filters;

namespace RhSensoWeb.Areas.RHU.Controllers
{
    [Area("RHU")]
    [CustomAuthorize("RHU_BASE")] // Exemplo de permissão base para a área
    public class BaseRhuController : Controller
    {
        // Métodos comuns ou propriedades para todos os controllers de RHU
    }
}


