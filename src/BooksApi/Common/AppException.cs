namespace BooksApi.Common;

public class AppException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }

    public AppException(int statusCode, string code, string message) : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }

    public static AppException NotFound(string message = "Not found") =>
        new(404, "NOT_FOUND", message);

    public static AppException BadRequest(string message) =>
        new(400, "BAD_REQUEST", message);

    public static AppException Unauthorized(string message = "Unauthorized") =>
        new(401, "UNAUTHORIZED", message);

    public static AppException Forbidden(string message = "Forbidden") =>
        new(403, "FORBIDDEN", message);

    public static AppException Conflict(string message) =>
        new(409, "CONFLICT", message);
}
