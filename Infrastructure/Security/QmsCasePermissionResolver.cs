namespace KarlixQMS.API.Infrastructure.Security;

public static class QmsCasePermissionResolver
{
    /// <summary>
    /// Mapira (EntityType, StatusCode) -> permission string koji mora postojati u "perm" claimu.
    /// EntityType: "COMPLAINT" (RIN) ili "NONCONFORMITY" (UN)
    /// StatusCode: npr. "RECEIVED", "IN_PROGRESS", "CLOSED"
    /// </summary>
    public static string? ResolveWritePerm(string? entityType, string? statusCode)
    {
        entityType = (entityType ?? "").Trim().ToUpperInvariant();
        statusCode = (statusCode ?? "").Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(entityType) || string.IsNullOrWhiteSpace(statusCode))
            return null;

        if (entityType == "COMPLAINT")
        {
            return statusCode switch
            {
                "RECEIVED" => QmsPerms.RinWriteReceived,
                "IN_PROGRESS" => QmsPerms.RinWriteInProgress,
                "CLOSED" => QmsPerms.RinWriteClosed,
                _ => null
            };
        }

        if (entityType == "NONCONFORMITY")
        {
            return statusCode switch
            {
                "RECEIVED" => QmsPerms.UnWriteReceived,
                "IN_PROGRESS" => QmsPerms.UnWriteInProgress,
                "CLOSED" => QmsPerms.UnWriteClosed,
                _ => null
            };
        }

        return null;
    }
}
