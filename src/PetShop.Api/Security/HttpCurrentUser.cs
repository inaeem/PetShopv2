using System.Security.Claims;
using PetShop.Service.Security;

namespace PetShop.Api.Security;

/// <summary>
/// <see cref="ICurrentUser"/> backed by the ASP.NET Core request principal. Lives in the
/// API (the composition root) so the service layer gets the caller's identity without
/// taking any dependency on <c>HttpContext</c>. Registered as scoped alongside
/// <c>AddHttpContextAccessor()</c>.
/// </summary>
public class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    // JwtBearer maps the inbound "email" claim to ClaimTypes.Email by default; fall back
    // to the raw "email" claim in case inbound claim mapping is turned off.
    public string? Email =>
        Principal?.FindFirstValue(ClaimTypes.Email)
        ?? Principal?.FindFirstValue("email");
}
