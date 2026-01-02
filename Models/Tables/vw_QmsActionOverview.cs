using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class vw_QmsActionOverview
{
    public Guid ActionId { get; set; }

    public Guid? IssueId { get; set; }

    public string? EntityType { get; set; }

    public string IssueNumber { get; set; } = null!;

    public string? IssueTitle { get; set; }

    public DateOnly IssueDate { get; set; }

    public string? ActionTitle { get; set; }

    public string ActionDescription { get; set; } = null!;

    public string? ActionTypeCode { get; set; }

    public string? ActionTypeName { get; set; }

    public string? ResponsibleName { get; set; }

    public Guid? ResponsibleOrgUnitId { get; set; }

    public string? ResponsibleOrgUnitCode { get; set; }

    public string? ResponsibleOrgUnitName { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateOnly? CompletedDate { get; set; }

    public string? EffectivenessCode { get; set; }

    public string? EffectivenessName { get; set; }

    public string ActionStatus { get; set; } = null!;

    public DateOnly? VerificationDate { get; set; }

    public string? VerificationNotes { get; set; }

    public Guid WorkflowStatusId { get; set; }

    public string? IssueStatusCode { get; set; }

    public string? IssueStatusName { get; set; }

    public Guid TenantId { get; set; }

    public bool IsActive { get; set; }
}
