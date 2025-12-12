namespace KarlixQMS.API.Infrastructure;

public interface ITenantContext
{
    Guid TenantId { get; }
    bool IsGlobal { get; }
}
