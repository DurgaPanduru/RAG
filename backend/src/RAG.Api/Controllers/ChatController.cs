using MediatR;
using Microsoft.AspNetCore.Mvc;
using RAG.Application.Commands;

namespace RAG.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IMediator mediator, ILogger<ChatController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("message")]
    [ProducesResponseType(typeof(SendChatMessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SendChatMessageResult>> SendMessage(
        [FromBody] SendChatMessageCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return StatusCode(500, new { error = "An error occurred processing your message" });
        }
    }

    [HttpGet("history/{conversationId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetChatHistory(string conversationId)
    {
        // This would query a database of chat history
        // For now, return empty array
        return Ok(new { conversationId, messages = new object[] { } });
    }
}
