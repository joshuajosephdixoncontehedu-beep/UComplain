namespace CommunityIncidentReporting.Api.Authorization;

/// <summary>
/// Named authorization policies matching the role permission matrix in
/// docs/api-contract.md. ReadOnlyAnalyst never needs a dedicated policy — every
/// endpoint it can reach only requires [Authorize] (any authenticated admin).
/// </summary>
public static class Policies
{
    /// <summary>Admin management, settings, audit logs.</summary>
    public const string SuperAdminOnly = "SuperAdminOnly";

    /// <summary>Category writes, assignment, and other IncidentManager-and-up actions.</summary>
    public const string ManagerOrAbove = "ManagerOrAbove";

    /// <summary>Verification decisions and report notes/limited updates.</summary>
    public const string ReviewerOrAbove = "ReviewerOrAbove";

    /// <summary>Any authenticated admin — equivalent to bare [Authorize] on AdminControllerBase, named explicitly for symmetry with RequireReporter.</summary>
    public const string RequireAdmin = "RequireAdmin";

    /// <summary>Mobile reporter endpoints. Requires the "ReporterScheme" JWT scheme (its own signing secret/audience — see Program.cs) so an admin token can never satisfy this.</summary>
    public const string RequireReporter = "RequireReporter";
}
