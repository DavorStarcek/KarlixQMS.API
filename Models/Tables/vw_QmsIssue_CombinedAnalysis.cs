using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class vw_QmsIssue_CombinedAnalysis
{
    public string IssueKind { get; set; } = null!;

    public Guid IssueId { get; set; }

    public Guid TenantId { get; set; }

    public string Number { get; set; } = null!;

    public DateOnly IssueDate { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? OrgUnitCode { get; set; }

    public string? OrgUnitName { get; set; }

    public string? CustomerCode { get; set; }

    public string? CustomerName { get; set; }

    public string? CustomerAddress { get; set; }

    public string? CustomerPhone { get; set; }

    public string? SourceType { get; set; }

    public string? WorkflowCode { get; set; }

    public string? WorkflowName { get; set; }

    public string? WorkflowEntityType { get; set; }

    public bool? IsInitial { get; set; }

    public bool? IsFinal { get; set; }

    public bool? IsCancelled { get; set; }
}
