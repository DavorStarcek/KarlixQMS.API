using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class QmsAction
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string EntityType { get; set; } = null!;

    public Guid EntityId { get; set; }

    public Guid ActionTypeId { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateOnly? CompletedDate { get; set; }

    public string? Responsible { get; set; }

    public Guid? OrgUnitId { get; set; }

    public Guid? EffectivenessId { get; set; }

    public string? EffectivenessComment { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? LastModifiedBy { get; set; }

    public DateTime? LastModifiedAt { get; set; }

    public virtual QmsActionType ActionType { get; set; } = null!;

    public virtual QmsEffectiveness? Effectiveness { get; set; }

    public virtual QmsOrgUnit? OrgUnit { get; set; }
}
