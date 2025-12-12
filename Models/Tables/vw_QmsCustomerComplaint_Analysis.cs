using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class vw_QmsCustomerComplaint_Analysis
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Number { get; set; } = null!;

    public DateOnly ComplaintDate { get; set; }

    public DateOnly ReceivedDate { get; set; }

    public string? CustomerCode { get; set; }

    public string? CustomerName { get; set; }

    public string? CustomerAddress { get; set; }

    public string? CustomerPhone { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string SourceType { get; set; } = null!;

    public string? LegacySystem { get; set; }

    public int? LegacyId { get; set; }

    public string? WorkflowCode { get; set; }

    public string? WorkflowName { get; set; }

    public string? WorkflowEntityType { get; set; }

    public bool? IsInitial { get; set; }

    public bool? IsFinal { get; set; }

    public bool? IsCancelled { get; set; }
}
