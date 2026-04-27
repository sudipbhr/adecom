namespace WeatherAPI.DTOs;

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public IEnumerable<string> Errors { get; set; } = [];

    public static ApiResponse SuccessResponse(string message) => new()
    {
        Success = true,
        Message = message
    };

    public static ApiResponse Failure(string message, IEnumerable<string>? errors = null) => new()
    {
        Success = false,
        Message = message,
        Errors = errors ?? []
    };
}

public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, string message = "") => new()
    {
        Success = true,
        Message = message,
        Data = data
    };

    public static new ApiResponse<T> Failure(string message, IEnumerable<string>? errors = null) => new()
    {
        Success = false,
        Message = message,
        Errors = errors ?? []
    };
}
