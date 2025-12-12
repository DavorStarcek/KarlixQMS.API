using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class QmsAuditAuditor
{
    public Guid Id { get; set; }

    public Guid AuditId { get; set; }

    public string Name { get; set; } = null!;

    public string? Role { get; set; }

    public virtual QmsAudit Audit { get; set; } = null!;
}
