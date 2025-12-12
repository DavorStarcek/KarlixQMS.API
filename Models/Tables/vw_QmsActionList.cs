using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class vw_QmsActionList
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string EntityType { get; set; } = null!;

    public Guid EntityId { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateOnly? CompletedDate { get; set; }

    public string? Responsible { get; set; }

    public string? OrgUnitCode { get; set; }

    public string? OrgUnitName { get; set; }

    public string? ActionTypeCode { get; set; }

    public string? ActionTypeName { get; set; }

    public string? EffectivenessCode { get; set; }

    public string? EffectivenessName { get; set; }

    public string? EntityNumber { get; set; }

    public string? EntityTitle { get; set; }
}
