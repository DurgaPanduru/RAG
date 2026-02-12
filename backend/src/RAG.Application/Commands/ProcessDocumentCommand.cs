using MediatR;
using RAG.Domain.Entities;

namespace RAG.Application.Commands;

public class ProcessDocumentCommand : IRequest<ProcessDocumentResult>
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
}

public class ProcessDocumentResult
{
    public string DocumentId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ChunksCreated { get; set; }
}
