using RAG.Domain.Entities;

namespace RAG.Domain.Interfaces;

/// <summary>
/// Interface for communication with LangChain service
/// </summary>
public interface ILangChainService
{
    /// <summary>
    /// Process a document through the RAG pipeline
    /// </summary>
    Task<DocumentProcessingResult> ProcessDocumentAsync(
        string documentId,
        byte[] content,
        string contentType,
        string fileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Query the RAG system
    /// </summary>
    Task<ChatResponse> QueryAsync(
        string query,
        string? conversationId = null,
        int topK = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stream query response
    /// </summary>
    IAsyncEnumerable<string> StreamQueryAsync(
        string query,
        string? conversationId = null,
        int topK = 5,
        CancellationToken cancellationToken = default);
}

public class DocumentProcessingResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ChunksCreated { get; set; }
}

public class ChatResponse
{
    public string Answer { get; set; } = string.Empty;
    public List<SourceReference> Sources { get; set; } = new();
    public int TokensUsed { get; set; }
}
