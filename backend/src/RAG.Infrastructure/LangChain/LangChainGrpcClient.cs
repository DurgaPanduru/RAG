using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RAG.Domain.Entities;
using RAG.Domain.Interfaces;
using RAG.Infrastructure.LangChain.Protos;

namespace RAG.Infrastructure.LangChain;

public class LangChainGrpcClient : ILangChainService
{
    private readonly GrpcChannel _channel;
    private readonly LangChainService.LangChainServiceClient _client;
    private readonly ILogger<LangChainGrpcClient> _logger;

    public LangChainGrpcClient(IConfiguration configuration, ILogger<LangChainGrpcClient> logger)
    {
        _logger = logger;
        var grpcUrl = configuration["LangChain:GrpcUrl"] ?? "http://langchain:50051";

        _channel = GrpcChannel.ForAddress(grpcUrl, new GrpcChannelOptions
        {
            MaxReceiveMessageSize = 100 * 1024 * 1024, // 100MB
            MaxSendMessageSize = 100 * 1024 * 1024
        });

        _client = new LangChainService.LangChainServiceClient(_channel);
        _logger.LogInformation("LangChain gRPC client initialized with URL: {Url}", grpcUrl);
    }

    public async Task<DocumentProcessingResult> ProcessDocumentAsync(
        string documentId,
        byte[] content,
        string contentType,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Sending document {DocumentId} to LangChain for processing", documentId);

            var request = new DocumentRequest
            {
                DocumentId = documentId,
                DocumentContent = Google.Protobuf.ByteString.CopyFrom(content),
                ContentType = contentType,
                Filename = fileName
            };

            var response = await _client.ProcessDocumentAsync(request, cancellationToken: cancellationToken);

            return new DocumentProcessingResult
            {
                Success = response.Success,
                Message = response.Message,
                ChunksCreated = response.ChunksCreated
            };
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error processing document {DocumentId}", documentId);
            return new DocumentProcessingResult
            {
                Success = false,
                Message = $"gRPC error: {ex.Status.Detail}",
                ChunksCreated = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<ChatResponse> QueryAsync(
        string query,
        string? conversationId = null,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Querying LangChain: {Query}", query);

            var request = new QueryRequest
            {
                Query = query,
                TopK = topK,
                ConversationId = conversationId ?? string.Empty
            };

            var response = await _client.QueryRAGAsync(request, cancellationToken: cancellationToken);

            return new ChatResponse
            {
                Answer = response.Answer,
                Sources = response.Sources.Select(s => new SourceReference
                {
                    DocumentId = s.DocumentId,
                    ChunkId = s.ChunkId,
                    PageNumber = s.PageNumber,
                    RelevanceScore = s.RelevanceScore,
                    Preview = s.Preview
                }).ToList(),
                TokensUsed = response.TokensUsed
            };
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error querying LangChain");
            throw new InvalidOperationException($"Failed to query LangChain: {ex.Status.Detail}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying LangChain");
            throw;
        }
    }

    public async IAsyncEnumerable<string> StreamQueryAsync(
        string query,
        string? conversationId = null,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        var request = new QueryRequest
        {
            Query = query,
            TopK = topK,
            ConversationId = conversationId ?? string.Empty
        };

        using var stream = _client.StreamQueryRAG(request, cancellationToken: cancellationToken);

        await foreach (var response in stream.ResponseStream.ReadAllAsync(cancellationToken))
        {
            yield return response.Chunk;

            if (response.IsFinal)
                break;
        }
    }
}
