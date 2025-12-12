using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class vw_QmsIssueList
{
    public Guid EntityId { get; set; }

    public Guid TenantId { get; set; }

    public string? EntityType { get; set; }

    public string Number { get; set; } = null!;

    public string? Title { get; set; }

    public DateOnly IssueDate { get; set; }

    public DateOnly? ReceivedDate { get; set; }

    public string? CustomerName { get; set; }

    public Guid? OrgUnitId { get; set; }

    public Guid WorkflowStatusId { get; set; }

    public string? StatusCode { get; set; }

    public string? StatusName { get; set; }
}
