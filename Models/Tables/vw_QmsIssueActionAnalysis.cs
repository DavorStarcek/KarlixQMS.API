using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class vw_QmsIssueActionAnalysis
{
    public Guid ActionId { get; set; }

    public Guid? IssueId { get; set; }

    public string EntityType { get; set; } = null!;

    public string? IssueNumber { get; set; }

    public string? IssueTitle { get; set; }

    public DateOnly? IssueRaisedAt { get; set; }

    public string? WorkflowStatusCode { get; set; }

    public string? WorkflowStatusName { get; set; }

    public string? ActionTitle { get; set; }

    public string? ActionTypeCode { get; set; }

    public string? ActionTypeName { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateOnly? CompletedDate { get; set; }

    public int IsCompleted { get; set; }

    public int? DaysLate { get; set; }

    public string? EffectivenessCode { get; set; }

    public string? EffectivenessName { get; set; }

    public bool IsActive { get; set; }
}
