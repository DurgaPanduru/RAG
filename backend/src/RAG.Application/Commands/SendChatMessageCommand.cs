using MediatR;
using RAG.Domain.Entities;

namespace RAG.Application.Commands;

public class SendChatMessageCommand : IRequest<SendChatMessageResult>
{
    public string Message { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public int TopK { get; set; } = 5;
}

public class SendChatMessageResult
{
    public string MessageId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public List<SourceReference> Sources { get; set; } = new();
    public int TokensUsed { get; set; }
    public bool FromCache { get; set; }
}
