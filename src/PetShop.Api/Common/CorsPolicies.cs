namespace PetShop.Api.Common;

/// <summary>Named CORS policies, shared between registration and the controllers.</summary>
public static class CorsPolicies
{
    /// <summary>
    /// The pipeline-wide policy, built from the "Cors" configuration section
    /// (see <see cref="CorsSettings"/>) and applied by <c>UseCors</c>. Controllers can
    /// name it explicitly with <c>[EnableCors(CorsPolicies.Default)]</c>.
    /// </summary>
    public const string Default = "PetShopCors";
}
