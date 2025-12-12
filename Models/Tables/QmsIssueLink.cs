using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class QmsIssueLink
{
    public Guid Id { get; set; }

    public Guid? TenantId { get; set; }

    public Guid FromIssueId { get; set; }

    public Guid ToIssueId { get; set; }

    public string LinkType { get; set; } = null!;

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public virtual QmsIssue FromIssue { get; set; } = null!;

    public virtual QmsIssue ToIssue { get; set; } = null!;
}
