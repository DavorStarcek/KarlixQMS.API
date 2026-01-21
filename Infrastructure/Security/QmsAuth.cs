using System.Security.Claims;

namespace KarlixQMS.API.Infrastructure.Security;

public static class QmsAuth
{
    public static bool IsAdmin(ClaimsPrincipal user)
        => user.IsInRole("GlobalAdmin") || user.IsInRole("TenantAdmin");

    public static bool HasPerm(ClaimsPrincipal user, string perm)
        => user.HasClaim("perm", perm);

    public static bool CanReadQms(ClaimsPrincipal user)
        => IsAdmin(user) || HasPerm(user, QmsPerms.QmsRead) || HasPerm(user, QmsPerms.QmsAdmin);

    public static bool CanWriteCasePhase(ClaimsPrincipal user, string entityType, string statusCode)
    {
        // Admin = sve
        if (IsAdmin(user)) return true;

        // Ako ima qms.admin – tretiramo kao “full write”
        if (HasPerm(user, QmsPerms.QmsAdmin)) return true;

        entityType = (entityType ?? "").Trim().ToUpperInvariant();
        statusCode = (statusCode ?? "").Trim().ToUpperInvariant();

        // Mapiranje: tip + status -> perm
        return (entityType, statusCode) switch
        {
            ("COMPLAINT", "RECEIVED") => HasPerm(user, QmsPerms.RinWriteReceived),
            ("COMPLAINT", "IN_PROGRESS") => HasPerm(user, QmsPerms.RinWriteInProgress),
            ("COMPLAINT", "CLOSED") => HasPerm(user, QmsPerms.RinWriteClosed),

            ("NONCONFORMITY", "RECEIVED") => HasPerm(user, QmsPerms.UnWriteReceived),
            ("NONCONFORMITY", "IN_PROGRESS") => HasPerm(user, QmsPerms.UnWriteInProgress),
            ("NONCONFORMITY", "CLOSED") => HasPerm(user, QmsPerms.UnWriteClosed),

            _ => false
        };
    }
}
