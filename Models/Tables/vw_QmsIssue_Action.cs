using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class vw_QmsIssue_Action
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string IssueKind { get; set; } = null!;

    public string? IssueNumber { get; set; }

    public DateOnly? NonconformityDate { get; set; }

    public DateOnly? ComplaintDate { get; set; }

    public DateOnly? IssueDate { get; set; }

    public string? NonconformityTitle { get; set; }

    public string? ComplaintTitle { get; set; }

    public string? IssueTitle { get; set; }

    public string? ActionTypeCode { get; set; }

    public string? ActionTypeName { get; set; }

    public string? ActionTitle { get; set; }

    public string ActionDescription { get; set; } = null!;

    public DateOnly? DueDate { get; set; }

    public DateOnly? CompletedDate { get; set; }

    public string? ResponsibleName { get; set; }

    public string? ResponsibleOrgUnitCode { get; set; }

    public string? ResponsibleOrgUnitName { get; set; }

    public string? EffectivenessCode { get; set; }

    public string? EffectivenessName { get; set; }

    public DateOnly? VerificationDate { get; set; }

    public string? VerificationNotes { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedByUserId { get; set; }
}
