using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class QmsNonconformity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Number { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public Guid? RelationTypeId { get; set; }

    public Guid? AuditId { get; set; }

    public Guid? StandardRequirementId { get; set; }

    public Guid? OrgUnitId { get; set; }

    public string Source { get; set; } = null!;

    public string? RaisedBy { get; set; }

    public DateOnly RaisedAt { get; set; }

    public DateOnly? DetectionDate { get; set; }

    public string? RootCause { get; set; }

    public string? RootCauseInvestigatedBy { get; set; }

    public Guid? EffectivenessId { get; set; }

    public string? EffectivenessComment { get; set; }

    public DateOnly? VerificationDate { get; set; }

    public string? VerifiedBy { get; set; }

    public Guid WorkflowStatusId { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? LastModifiedBy { get; set; }

    public DateTime? LastModifiedAt { get; set; }

    public string? LegacySystem { get; set; }

    public int? LegacyId { get; set; }

    public string? Code { get; set; }

    public Guid? ProductId { get; set; }

    public Guid? ProductStateId { get; set; }

    public DateOnly? RequestDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateOnly? CloseDate { get; set; }

    public string? Cause { get; set; }

    public string? ImmediateCorrection { get; set; }

    public string? CorrectiveAction { get; set; }

    public string? PreventiveAction { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public virtual QmsAudit? Audit { get; set; }

    public virtual QmsEffectiveness? Effectiveness { get; set; }

    public virtual QmsOrgUnit? OrgUnit { get; set; }

    public virtual ICollection<QmsIssueAction> QmsIssueActions { get; set; } = new List<QmsIssueAction>();

    public virtual ICollection<QmsIssue> QmsIssues { get; set; } = new List<QmsIssue>();

    public virtual QmsNonconformityRelationType? RelationType { get; set; }

    public virtual QmsStandardRequirement? StandardRequirement { get; set; }

    public virtual QmsWorkflowStatus WorkflowStatus { get; set; } = null!;
}
