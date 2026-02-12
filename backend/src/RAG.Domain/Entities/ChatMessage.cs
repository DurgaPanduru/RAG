namespace RAG.Domain.Entities;

/// <summary>
/// Represents a chat message in a conversation
/// </summary>
public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ConversationId { get; set; } = string.Empty;
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public List<SourceReference> Sources { get; set; } = new();
    public int? TokensUsed { get; set; }
    public bool FromCache { get; set; }
}

public enum MessageRole
{
    User,
    Assistant,
    System
}

/// <summary>
/// Represents a source document reference for a chat response
/// </summary>
public class SourceReference
{
    public string DocumentId { get; set; } = string.Empty;
    public string ChunkId { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public float RelevanceScore { get; set; }
    public string? Preview { get; set; }
}
