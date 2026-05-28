namespace PetShop.Service.Common;

/// <summary>
/// Domain/business error raised by the service layer. The API translates this
/// into a clean HTTP status code (see ApiExceptionFilter).
/// </summary>
public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(string message, int statusCode = 400) : base(message)
        => StatusCode = statusCode;

    public static AppException NotFound(string what) => new($"{what} was not found.", 404);
    public static AppException Conflict(string message) => new(message, 409);
    public static AppException Unauthorized(string message = "Invalid credentials.") => new(message, 401);
}
