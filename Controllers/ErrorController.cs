using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Net;

namespace RhSensoWeb.Controllers
{
    [Route("Error")]
    public class ErrorController : Controller
    {
        // /Error/500 -> exceções não tratadas
        [Route("500")]
        public IActionResult Handle500()
        {
            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerFeature>();
            if (exceptionFeature is not null)
            {
                var ex = exceptionFeature.Error;
                var path = HttpContext.Features.Get<IExceptionHandlerPathFeature>()?.Path;
                Log.Error(ex, "Erro não tratado em {Path}", path);
            }

            // Se for requisição de API/JSON, retorna ProblemDetails
            if (Request.ExpectsJson())
            {
                var traceId = HttpContext.TraceIdentifier;
                return Problem(
                    title: "Erro interno do servidor",
                    detail: "Ocorreu um erro inesperado ao processar sua solicitação.",
                    statusCode: (int)HttpStatusCode.InternalServerError,
                    instance: HttpContext.Request?.Path.Value
                );
            }

            Response.StatusCode = 500;
            ViewData["RequestId"] = HttpContext.TraceIdentifier;
            return View("Error"); // Views/Error/Error.cshtml
        }

        // /Error/{statusCode} -> 404, 403, 401 etc.
        [Route("{statusCode:int}")]
        public IActionResult HandleHttpStatusCode(int statusCode)
        {
            // Para APIs/JSON, devolve ProblemDetails
            if (Request.ExpectsJson())
            {
                var title = statusCode switch
                {
                    404 => "Recurso não encontrado",
                    403 => "Acesso negado",
                    401 => "Não autenticado",
                    _ => "Erro ao processar a solicitação"
                };

                return Problem(
                    title: title,
                    statusCode: statusCode,
                    instance: HttpContext.Request?.Path.Value
                );
            }

            // Para navegação web, renderiza views AdminLTE-friendly
            switch (statusCode)
            {
                case 404:
                    Response.StatusCode = 404;
                    return View("NotFound");
                case 403:
                    Response.StatusCode = 403;
                    return View("Forbidden");
                case 401:
                    Response.StatusCode = 401;
                    return View("Unauthorized");
                default:
                    Response.StatusCode = statusCode;
                    ViewData["StatusCode"] = statusCode;
                    return View("StatusCode");
            }
        }
    }

    // Extensão disponível no mesmo namespace para resolver em tempo de compilação
    internal static class RequestJsonDetectExtension
    {
        public static bool ExpectsJson(this HttpRequest request)
            => RhSensoWeb.Helpers.HttpRequestExtensions.ExpectsJson(request);
    }
}
