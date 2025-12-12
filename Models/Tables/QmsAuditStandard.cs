using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class QmsAuditStandard
{
    public Guid Id { get; set; }

    public Guid AuditId { get; set; }

    public Guid StandardId { get; set; }

    public virtual QmsAudit Audit { get; set; } = null!;

    public virtual QmsStandard Standard { get; set; } = null!;
}
