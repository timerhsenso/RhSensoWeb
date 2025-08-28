namespace RhSensoWeb.Common
{
    /// <summary>
    /// Representa uma resposta padronizada de API.
    /// </summary>
    public class ApiResponse
    {
        public bool Success { get; init; }
        public string? Message { get; init; }
        public IDictionary<string, string[]>? Errors { get; init; }

        public static ApiResponse Ok(string? message = null)
            => new() { Success = true, Message = message };

        public static ApiResponse Fail(string? message = null, IDictionary<string, string[]>? errors = null)
            => new() { Success = false, Message = message, Errors = errors };

        public static ApiResponse FromException(Exception ex, string? message = null)
            => new()
            {
                Success = false,
                Message = message ?? "Ocorreu um erro interno no servidor.",
                Errors = new Dictionary<string, string[]> { ["_error"] = new[] { ex.Message } }
            };
    }

    /// <summary>
    /// Representa uma resposta padronizada de API com payload.
    /// </summary>
    public class ApiResponse<T> : ApiResponse
    {
        public T? Data { get; init; }

        public static ApiResponse<T> Ok(T data, string? message = null)
            => new() { Success = true, Data = data, Message = message };

        public static ApiResponse<T> Fail(string? message = null, IDictionary<string, string[]>? errors = null)
            => new() { Success = false, Message = message, Errors = errors };

        public static ApiResponse<T> FromException(Exception ex, string? message = null)
            => new()
            {
                Success = false,
                Message = message ?? "Ocorreu um erro interno no servidor.",
                Errors = new Dictionary<string, string[]> { ["_error"] = new[] { ex.Message } }
            };
    }
}
