namespace RhSensoWeb.Common;

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public Dictionary<string, string[]>? Errors { get; set; }

    public static ApiResponse Ok(string? message = null)
        => new ApiResponse { Success = true, Message = message ?? "" };

    public static ApiResponse Fail(string message, Dictionary<string, string[]>? errors = null)
        => new ApiResponse { Success = false, Message = message, Errors = errors };
}

public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null)
        => new ApiResponse<T> { Success = true, Data = data, Message = message ?? "" };

    public static ApiResponse<T> Fail(string message, Dictionary<string, string[]>? errors = null)
        => new ApiResponse<T> { Success = false, Message = message, Errors = errors };
}
