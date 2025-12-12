using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class QmsWorkflowStatus
{
    public Guid Id { get; set; }

    public Guid? TenantId { get; set; }

    public string EntityType { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int? DisplayOrder { get; set; }

    public bool IsInitial { get; set; }

    public bool IsFinal { get; set; }

    public bool IsCancelled { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<QmsAudit> QmsAudits { get; set; } = new List<QmsAudit>();

    public virtual ICollection<QmsCustomerComplaint> QmsCustomerComplaints { get; set; } = new List<QmsCustomerComplaint>();

    public virtual ICollection<QmsIssue> QmsIssues { get; set; } = new List<QmsIssue>();

    public virtual ICollection<QmsNonconformity> QmsNonconformities { get; set; } = new List<QmsNonconformity>();
}
