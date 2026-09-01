namespace PetShop.Api.Common;

/// <summary>Named output-cache policies, shared between registration and the controllers.</summary>
public static class OutputCachePolicies
{
    /// <summary>
    /// The configured-endpoints page. Its data comes from the startup configuration
    /// snapshot and cannot change while the process runs, so the rendered HTML is held
    /// for a long window; a restart starts a fresh (empty) cache.
    /// </summary>
    public const string ConfiguredEndpoints = "ConfiguredEndpoints";
}
