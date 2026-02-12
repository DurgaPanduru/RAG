using MediatR;
using Microsoft.Extensions.Logging;
using RAG.Domain.Entities;
using RAG.Domain.Interfaces;

namespace RAG.Application.Commands;

public class ProcessDocumentCommandHandler : IRequestHandler<ProcessDocumentCommand, ProcessDocumentResult>
{
    private readonly IDocumentStorageService _storageService;
    private readonly ILangChainService _langChainService;
    private readonly ILogger<ProcessDocumentCommandHandler> _logger;

    public ProcessDocumentCommandHandler(
        IDocumentStorageService storageService,
        ILangChainService langChainService,
        ILogger<ProcessDocumentCommandHandler> logger)
    {
        _storageService = storageService;
        _langChainService = langChainService;
        _logger = logger;
    }

    public async Task<ProcessDocumentResult> Handle(ProcessDocumentCommand request, CancellationToken cancellationToken)
    {
        var documentId = Guid.NewGuid().ToString();

        try
        {
            _logger.LogInformation("Processing document {FileName} with ID {DocumentId}",
                request.FileName, documentId);

            // Step 1: Store document in MinIO
            _logger.LogDebug("Uploading document to storage");
            await _storageService.UploadDocumentAsync(
                documentId,
                request.Content,
                request.ContentType,
                request.FileName,
                cancellationToken);

            // Step 2: Process document through LangChain RAG pipeline
            _logger.LogDebug("Sending document to LangChain for processing");
            var processingResult = await _langChainService.ProcessDocumentAsync(
                documentId,
                request.Content,
                request.ContentType,
                request.FileName,
                cancellationToken);

            if (!processingResult.Success)
            {
                _logger.LogError("Document processing failed: {Message}", processingResult.Message);
                return new ProcessDocumentResult
                {
                    DocumentId = documentId,
                    FileName = request.FileName,
                    Status = DocumentStatus.Failed,
                    Message = processingResult.Message,
                    ChunksCreated = 0
                };
            }

            _logger.LogInformation("Document {DocumentId} processed successfully. Created {ChunkCount} chunks",
                documentId, processingResult.ChunksCreated);

            return new ProcessDocumentResult
            {
                DocumentId = documentId,
                FileName = request.FileName,
                Status = DocumentStatus.Completed,
                Message = "Document processed successfully",
                ChunksCreated = processingResult.ChunksCreated
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document {DocumentId}", documentId);
            return new ProcessDocumentResult
            {
                DocumentId = documentId,
                FileName = request.FileName,
                Status = DocumentStatus.Failed,
                Message = $"Error: {ex.Message}",
                ChunksCreated = 0
            };
        }
    }
}
