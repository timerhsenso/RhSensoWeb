using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RhSensoWeb.Common; // ApiResponse

namespace RhSensoWeb.Middleware
{
    /// <summary>
    /// Captura exceções não tratadas no pipeline e retorna uma ApiResponse (HTTP 500).
    /// Mantém detalhes no log (ILogger) e não vaza stack trace para o cliente.
    /// </summary>
    public sealed class _ExceptionHandlingMiddleware : IMiddleware
    {
        private readonly ILogger<_ExceptionHandlingMiddleware> _logger;

        public _ExceptionHandlingMiddleware(ILogger<_ExceptionHandlingMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                // Log completo com método e caminho
                _logger.LogError(ex,
                    "Unhandled exception processing {Method} {Path}",
                    context.Request?.Method, context.Request?.Path.Value);

                if (!context.Response.HasStarted)
                {
                    context.Response.Clear();
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "application/json; charset=utf-8";

                    // Mantém padrão PascalCase do projeto (PropertyNamingPolicy = null)
                    var payload = ApiResponse.Fail("Ocorreu um erro interno no servidor01.");
                    var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = null,
                        WriteIndented = false
                    });

                    await context.Response.WriteAsync(json);
                }
                else
                {
                    // Se já começou a resposta, repropaga (evita InvalidOperationException)
                    throw;
                }
            }
        }
    }
}
