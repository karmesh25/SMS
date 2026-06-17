namespace ABR.Application.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? new List<string>() };
}

public class HealthDto
{
    public string Status { get; set; } = "healthy";
    public string Version { get; set; } = "1.0.0";
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
