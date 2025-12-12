using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class QmsAuditType
{
    public Guid Id { get; set; }

    public Guid? TenantId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int? DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<QmsAudit> QmsAudits { get; set; } = new List<QmsAudit>();
}
