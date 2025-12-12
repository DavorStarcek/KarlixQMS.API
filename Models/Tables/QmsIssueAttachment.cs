using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class QmsIssueAttachment
{
    public Guid Id { get; set; }

    public Guid? TenantId { get; set; }

    public Guid IssueId { get; set; }

    public string FileName { get; set; } = null!;

    public string? ContentType { get; set; }

    public long? FileSizeBytes { get; set; }

    public string StorageProvider { get; set; } = null!;

    public string? StoragePath { get; set; }

    public byte[]? FileContent { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public virtual QmsIssue Issue { get; set; } = null!;
}
