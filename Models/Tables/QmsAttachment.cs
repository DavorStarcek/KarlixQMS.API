using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class QmsAttachment
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string EntityType { get; set; } = null!;

    public Guid EntityId { get; set; }

    public string FileName { get; set; } = null!;

    public string? ContentType { get; set; }

    public int? FileSizeBytes { get; set; }

    public byte[]? Blob { get; set; }

    public string? FilePath { get; set; }

    public string? UploadedBy { get; set; }

    public DateTime UploadedAt { get; set; }
}
