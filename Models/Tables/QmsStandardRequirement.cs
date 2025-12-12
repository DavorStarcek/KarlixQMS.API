using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class QmsStandardRequirement
{
    public Guid Id { get; set; }

    public Guid? TenantId { get; set; }

    public Guid StandardId { get; set; }

    public string Code { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int? DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<QmsNonconformity> QmsNonconformities { get; set; } = new List<QmsNonconformity>();

    public virtual QmsStandard Standard { get; set; } = null!;
}
