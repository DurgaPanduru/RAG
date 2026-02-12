namespace RAG.Domain.Entities;

/// <summary>
/// Represents a document uploaded to the system
/// </summary>
public class Document
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public int PageCount { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public int ChunksCreated { get; set; }
    public string? ErrorMessage { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public enum DocumentStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
