using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class vw_QmsNonconformity_Analysis
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Number { get; set; } = null!;

    public DateOnly RaisedAt { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public Guid? OrgUnitId { get; set; }

    public string? OrgUnitCode { get; set; }

    public string? OrgUnitName { get; set; }

    public Guid? EffectivenessId { get; set; }

    public string? EffectivenessCode { get; set; }

    public string? EffectivenessName { get; set; }

    public Guid WorkflowStatusId { get; set; }

    public string? WorkflowCode { get; set; }

    public string? WorkflowName { get; set; }

    public string? WorkflowEntityType { get; set; }

    public bool? IsInitial { get; set; }

    public bool? IsFinal { get; set; }

    public bool? IsCancelled { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedByUserId { get; set; }
}
