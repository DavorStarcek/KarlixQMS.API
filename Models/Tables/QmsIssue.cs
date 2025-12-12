using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class QmsIssue
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string EntityType { get; set; } = null!;

    public Guid? NonconformityId { get; set; }

    public Guid? ComplaintId { get; set; }

    public Guid WorkflowStatusId { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public virtual QmsCustomerComplaint? Complaint { get; set; }

    public virtual QmsNonconformity? Nonconformity { get; set; }

    public virtual ICollection<QmsIssueAction> QmsIssueActions { get; set; } = new List<QmsIssueAction>();

    public virtual ICollection<QmsIssueAttachment> QmsIssueAttachments { get; set; } = new List<QmsIssueAttachment>();

    public virtual ICollection<QmsIssueLink> QmsIssueLinkFromIssues { get; set; } = new List<QmsIssueLink>();

    public virtual ICollection<QmsIssueLink> QmsIssueLinkToIssues { get; set; } = new List<QmsIssueLink>();

    public virtual QmsWorkflowStatus WorkflowStatus { get; set; } = null!;
}
