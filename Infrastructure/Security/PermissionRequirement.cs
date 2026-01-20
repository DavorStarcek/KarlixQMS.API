using Microsoft.AspNetCore.Authorization;

namespace KarlixQMS.API.Infrastructure.Security;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(params string[] requiredAnyOf)
    {
        RequiredAnyOf = requiredAnyOf ?? Array.Empty<string>();
    }

    public IReadOnlyList<string> RequiredAnyOf { get; }
}
