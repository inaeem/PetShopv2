namespace PetShop.Service.Security;

/// <summary>
/// The authenticated caller for the current request, as seen by the service layer.
/// <para>
/// Defined here (not in the API) on purpose: the service layer depends only on this
/// abstraction and stays free of any HTTP/ASP.NET Core coupling. The implementation
/// lives in the API composition root (<c>HttpCurrentUser</c>, over
/// <c>IHttpContextAccessor</c>).
/// </para>
/// </summary>
public interface ICurrentUser
{
    /// <summary>The caller's email claim, or <c>null</c> if unauthenticated / no email claim.</summary>
    string? Email { get; }

    /// <summary>True when the request carries an authenticated identity.</summary>
    bool IsAuthenticated { get; }
}
