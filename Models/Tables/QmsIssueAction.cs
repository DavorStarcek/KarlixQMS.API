using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class QmsIssueAction
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid? NonconformityId { get; set; }

    public Guid? CustomerComplaintId { get; set; }

    public Guid ActionTypeId { get; set; }

    public string? Title { get; set; }

    public string Description { get; set; } = null!;

    public DateOnly? DueDate { get; set; }

    public DateOnly? CompletedDate { get; set; }

    public string? ResponsibleName { get; set; }

    public Guid? ResponsibleOrgUnitId { get; set; }

    public Guid? EffectivenessId { get; set; }

    public DateOnly? VerificationDate { get; set; }

    public string? VerificationNotes { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public Guid? IssueId { get; set; }

    public bool IsActive { get; set; }

    public virtual QmsActionType ActionType { get; set; } = null!;

    public virtual QmsCustomerComplaint? CustomerComplaint { get; set; }

    public virtual QmsEffectiveness? Effectiveness { get; set; }

    public virtual QmsIssue? Issue { get; set; }

    public virtual QmsNonconformity? Nonconformity { get; set; }

    public virtual QmsOrgUnit? ResponsibleOrgUnit { get; set; }
}
