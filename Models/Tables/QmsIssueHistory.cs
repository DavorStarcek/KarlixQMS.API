using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class QmsIssueHistory
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid IssueId { get; set; }

    public string? FromStatusCode { get; set; }

    public string ToStatusCode { get; set; } = null!;

    public string Phase { get; set; } = null!;

    public DateTime ChangedAt { get; set; }

    public Guid? ChangedByUserId { get; set; }

    public string? ChangedByName { get; set; }
}
