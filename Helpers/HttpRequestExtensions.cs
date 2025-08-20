using Microsoft.Net.Http.Headers;

namespace RhSensoWeb.Helpers
{
    public static class HttpRequestExtensions
    {
        /// <summary>
        /// Detecta se o cliente espera JSON (API) com base no header Accept
        /// ou se é uma chamada AJAX (X-Requested-With).
        /// </summary>
        public static bool ExpectsJson(this HttpRequest request)
        {
            var accept = request.Headers[HeaderNames.Accept].ToString();
            if (!string.IsNullOrWhiteSpace(accept) && accept.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                return true;

            var xr = request.Headers["X-Requested-With"].ToString();
            if (!string.IsNullOrWhiteSpace(xr) && xr.Equals("XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
                return true;

            // Também considere fetch() sem Accept explícito mas com JSON
            var contentType = request.Headers[HeaderNames.ContentType].ToString();
            if (!string.IsNullOrWhiteSpace(contentType) && contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }
    }
}
