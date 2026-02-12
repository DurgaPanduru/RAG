namespace RAG.Domain.Interfaces;

/// <summary>
/// Interface for document storage (Data Lake)
/// </summary>
public interface IDocumentStorageService
{
    Task<string> UploadDocumentAsync(
        string documentId,
        byte[] content,
        string contentType,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<byte[]> DownloadDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default);

    Task<bool> DocumentExistsAsync(
        string documentId,
        CancellationToken cancellationToken = default);
}
