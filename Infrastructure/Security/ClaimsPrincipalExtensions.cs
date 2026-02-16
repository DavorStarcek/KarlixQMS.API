using System;
using System.Security.Claims;

namespace KarlixQMS.API.Infrastructure.Security;

public static class ClaimsPrincipalExtensions
{
    public static Guid? TryGetUserId(this ClaimsPrincipal user)
    {
        if (user == null) return null;

        // OpenIddict najčešće: sub
        var sub = user.FindFirstValue("sub");
        if (!string.IsNullOrWhiteSpace(sub) && Guid.TryParse(sub, out var g1))
            return g1;

        // fallback
        var nameId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(nameId) && Guid.TryParse(nameId, out var g2))
            return g2;

        return null;
    }

    public static string? TryGetDisplayName(this ClaimsPrincipal user)
    {
        if (user == null) return null;

        // OpenID standard: name
        var name = user.FindFirstValue("name");
        if (!string.IsNullOrWhiteSpace(name)) return name;

        // fallback
        name = user.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(name)) return name;

        // fallback
        name = user.FindFirstValue(ClaimTypes.Name);
        if (!string.IsNullOrWhiteSpace(name)) return name;

        return null;
    }
}
