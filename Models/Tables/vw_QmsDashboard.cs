using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class vw_QmsDashboard
{
    public string? EntityType { get; set; }

    public int? TotalIssues { get; set; }

    public int? ClosedIssues { get; set; }

    public int? CancelledIssues { get; set; }

    public int? OpenIssues { get; set; }

    public int? TotalActions { get; set; }

    public int? CompletedActions { get; set; }

    public int? EvaluatedActions { get; set; }
}
