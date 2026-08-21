namespace PetShop.Data.Diagnostics;

/// <summary>Identifies which architectural layer a traced call belongs to.</summary>
public enum LayerKind
{
    Service,
    Data,

    /// <summary>Outbound integrations owned by the service layer: SMTP, partner APIs.</summary>
    External,

    /// <summary>Raw SQL commands issued by EF Core. Verbose — off by default.</summary>
    Sql
}
