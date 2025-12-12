using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class QmsAudit
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Number { get; set; } = null!;

    public string Name { get; set; } = null!;

    public Guid AuditTypeId { get; set; }

    public string? Scope { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string LeadAuditor { get; set; } = null!;

    public string? Auditee { get; set; }

    public Guid? OrgUnitId { get; set; }

    public Guid WorkflowStatusId { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? LastModifiedBy { get; set; }

    public DateTime? LastModifiedAt { get; set; }

    public virtual QmsAuditType AuditType { get; set; } = null!;

    public virtual QmsOrgUnit? OrgUnit { get; set; }

    public virtual ICollection<QmsAuditAuditor> QmsAuditAuditors { get; set; } = new List<QmsAuditAuditor>();

    public virtual ICollection<QmsAuditStandard> QmsAuditStandards { get; set; } = new List<QmsAuditStandard>();

    public virtual ICollection<QmsNonconformity> QmsNonconformities { get; set; } = new List<QmsNonconformity>();

    public virtual QmsWorkflowStatus WorkflowStatus { get; set; } = null!;
}
