using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class vw_QmsIssueKpiMonthly
{
    public Guid TenantId { get; set; }

    public string? EntityType { get; set; }

    public int? Year { get; set; }

    public int? Month { get; set; }

    public int? TotalCount { get; set; }

    public int? ClosedCount { get; set; }

    public int? CancelledCount { get; set; }

    public int? OpenCount { get; set; }
}
