using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace KarlixQMS.API.Infrastructure.Security;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        // Global bypass: admin permission
        if (HasPerm(context.User, QmsPermissions.Admin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Any-of match
        foreach (var perm in requirement.RequiredAnyOf)
        {
            if (HasPerm(context.User, perm))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }

    private static bool HasPerm(ClaimsPrincipal user, string perm)
    {
        if (string.IsNullOrWhiteSpace(perm)) return false;

        // ClaimType = "perm" (kako seedamo u KarlixID)
        return user.Claims.Any(c =>
            string.Equals(c.Type, "perm", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.Value, perm, StringComparison.OrdinalIgnoreCase));
    }
}
