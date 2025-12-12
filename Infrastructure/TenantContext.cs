using System.Security.Claims;

namespace KarlixQMS.API.Infrastructure;

public class TenantContext : ITenantContext
{
    public Guid TenantId { get; }
    public bool IsGlobal => TenantId == Guid.Empty;

    public TenantContext(IHttpContextAccessor accessor)
    {
        var raw =
            accessor.HttpContext?.User?.FindFirst("tenant")?.Value ??
            accessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;

        TenantId = Guid.TryParse(raw, out var t) ? t : Guid.Empty;
    }
}
