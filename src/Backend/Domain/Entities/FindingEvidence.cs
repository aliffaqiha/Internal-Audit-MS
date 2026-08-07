using IAMS.Domain.Common;

namespace IAMS.Domain.Entities;

/// <summary>Immutable, versioned evidence file attached to a finding and stored in object storage.</summary>
public sealed class FindingEvidence : BaseEntity
{
    public Guid FindingId { get; set; }
    public Finding? Finding { get; set; }

    /// <summary>Sanitized, original display filename shown to the user.</summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>Object key in object storage (MinIO). Never derived from user input directly.</summary>
    public string StoredObjectName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }

    /// <summary>1-based version number, incremented per upload for the same finding.</summary>
    public int Version { get; set; } = 1;
}