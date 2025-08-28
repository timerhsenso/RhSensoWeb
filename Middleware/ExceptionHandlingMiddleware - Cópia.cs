using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using RhSensoWeb.Common; // ApiResponse

namespace RhSensoWeb.Middleware
{
    /// <summary>
    /// - API/AJAX => retorna ApiResponse JSON 500
    /// - HTML => rethrow p/ DeveloperExceptionPage / ExceptionHandler / ErrorController
    /// - Sempre devolve X-Trace-Id p/ casar com o log
    /// - Em DEV, com ?__debug=1 (ou header X-Debug:1) inclui detalhes no JSON
    /// </summary>
    public sealed class ExceptionHandlingMiddleware : IMiddleware
    {
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionHandlingMiddleware(
            ILogger<ExceptionHandlingMiddleware> logger,
            IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (OperationCanceledException oce) when (context.RequestAborted.IsCancellationRequested)
            {
                // Cliente cancelou (fechou aba, navegou)
                _logger.LogWarning(oce, "Request cancelled by client {Method} {Path}",
                    context.Request?.Method, context.Request?.Path.Value);

                if (!context.Response.HasStarted)
                {
                    context.Response.Clear();
                    context.Response.StatusCode = 499; // Client Closed Request (convenção nginx)
                }
            }
            catch (Exception ex)
            {
                var traceId = context.TraceIdentifier;
                var endpoint = context.GetEndpoint()?.DisplayName;

                _logger.LogError(ex,
                    "Unhandled exception [{TraceId}] {Method} {Path} Endpoint:{Endpoint}",
                    traceId, context.Request?.Method, context.Request?.Path.Value, endpoint);

                // Devolve o TraceId para casar com o log
                if (!context.Response.Headers.ContainsKey("X-Trace-Id"))
                    context.Response.Headers.Add("X-Trace-Id", traceId);

                if (!context.Response.HasStarted)
                {
                    // HTML? Deixa a pipeline padrão renderizar (DEV page/ExceptionHandler/ErrorController)
                    if (!IsApiRequest(context))
                        throw;

                    // API/AJAX => JSON padronizado
                    context.Response.Clear();
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "application/json; charset=utf-8";

                    var message = "Ocorreu um erro interno no servidor.";
                    var includeDebug = _env.IsDevelopment() && WantsDebug(context);

                    // Mantém contrato {Success, Message, Errors} (PascalCase)
                    object payload = includeDebug
                        ? new
                        {
                            Success = false,
                            Message = $"{message} TraceId={traceId}",
                            Errors = new
                            {
                                Exception = ex.GetType().FullName,
                                ex.Message,
                                StackTrace = ex.ToString()
                            }
                        }
                        : ApiResponse.Fail($"{message} TraceId={traceId}");

                    var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = null,
                        WriteIndented = includeDebug
                    });

                    await context.Response.WriteAsync(json);
                }
                else
                {
                    // Resposta já começou; repropaga para não corromper o output
                    throw;
                }
            }
        }

        // Heurística simples pra diferenciar API/AJAX de navegação
        private static bool IsApiRequest(HttpContext ctx)
        {
            var path = ctx.Request.Path.Value ?? string.Empty;
            if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)) return true;

            var accept = ctx.Request.Headers["Accept"].ToString();
            if (accept.Contains("application/json", StringComparison.OrdinalIgnoreCase)) return true;

            var xrw = ctx.Request.Headers["X-Requested-With"].ToString();
            if (string.Equals(xrw, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)) return true;

            var sfm = ctx.Request.Headers["Sec-Fetch-Mode"].ToString();
            if (string.Equals(sfm, "cors", StringComparison.OrdinalIgnoreCase)) return true; // fetch()

            return false;
        }

        private static bool WantsDebug(HttpContext ctx) =>
            string.Equals(ctx.Request.Query["__debug"], "1") ||
            string.Equals(ctx.Request.Headers["X-Debug"], "1");
    }
}
