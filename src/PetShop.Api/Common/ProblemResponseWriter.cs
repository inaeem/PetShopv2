using System.Text.Json;
using PetShop.Service.Common;

namespace PetShop.Api.Common;

/// <summary>
/// Writes the uniform <see cref="ApiResponse{T}"/> envelope directly to the
/// HTTP response. Used by the authentication pipeline (401/403), which runs as
/// middleware and therefore cannot rely on MVC's result formatting.
/// </summary>
public static class ProblemResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static Task WriteAsync(HttpContext context, int statusCode, string message,
        IReadOnlyList<string>? errors = null)
    {
        // Don't try to write twice if something already started the response.
        if (context.Response.HasStarted) return Task.CompletedTask;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = ApiResponse<object>.Fail(message, errors);
        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
