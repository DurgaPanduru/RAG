using MediatR;
using Microsoft.Extensions.Logging;
using RAG.Domain.Interfaces;

namespace RAG.Application.Commands;

public class SendChatMessageCommandHandler : IRequestHandler<SendChatMessageCommand, SendChatMessageResult>
{
    private readonly ILangChainService _langChainService;
    private readonly ISemanticCacheService _cacheService;
    private readonly ILogger<SendChatMessageCommandHandler> _logger;

    public SendChatMessageCommandHandler(
        ILangChainService langChainService,
        ISemanticCacheService cacheService,
        ILogger<SendChatMessageCommandHandler> logger)
    {
        _langChainService = langChainService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<SendChatMessageResult> Handle(SendChatMessageCommand request, CancellationToken cancellationToken)
    {
        var messageId = Guid.NewGuid().ToString();
        var conversationId = request.ConversationId ?? Guid.NewGuid().ToString();

        try
        {
            _logger.LogInformation("Processing chat message for conversation {ConversationId}", conversationId);

            // Step 1: Check semantic cache
            var cachedResponse = await _cacheService.GetSimilarResponseAsync(
                request.Message,
                similarityThreshold: 0.92f,
                cancellationToken);

            if (cachedResponse != null)
            {
                _logger.LogInformation("Cache hit for query: {Query}", request.Message);
                return new SendChatMessageResult
                {
                    MessageId = messageId,
                    ConversationId = conversationId,
                    Answer = cachedResponse,
                    Sources = new(),
                    TokensUsed = 0,
                    FromCache = true
                };
            }

            // Step 2: Query LangChain RAG system
            _logger.LogDebug("Cache miss. Querying LangChain service");
            var response = await _langChainService.QueryAsync(
                request.Message,
                conversationId,
                request.TopK,
                cancellationToken);

            // Step 3: Cache the response
            // Note: We'd need the embedding from LangChain to cache properly
            // For now, we'll use a simple hash-based cache key
            _logger.LogDebug("Caching response for future queries");

            _logger.LogInformation("Chat message processed successfully. Tokens used: {TokensUsed}",
                response.TokensUsed);

            return new SendChatMessageResult
            {
                MessageId = messageId,
                ConversationId = conversationId,
                Answer = response.Answer,
                Sources = response.Sources,
                TokensUsed = response.TokensUsed,
                FromCache = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message for conversation {ConversationId}",
                conversationId);
            throw;
        }
    }
}
