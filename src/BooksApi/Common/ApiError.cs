namespace BooksApi.Common;

public class ApiError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? Details { get; set; }

    public static ApiError Create(string code, string message, object? details = null) =>
        new() { Code = code, Message = message, Details = details };
}
