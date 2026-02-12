using MediatR;
using Microsoft.AspNetCore.Mvc;
using RAG.Application.Commands;

namespace RAG.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DocumentController> _logger;

    public DocumentController(IMediator mediator, ILogger<DocumentController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("upload")]
    [ProducesResponseType(typeof(ProcessDocumentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(52428800)] // 50MB
    public async Task<ActionResult<ProcessDocumentResult>> UploadDocument(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        if (!file.ContentType.Contains("pdf"))
            return BadRequest(new { error = "Only PDF files are supported" });

        try
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);

            var command = new ProcessDocumentCommand
            {
                FileName = file.FileName,
                Content = memoryStream.ToArray(),
                ContentType = file.ContentType
            };

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            return StatusCode(500, new { error = "An error occurred uploading the document" });
        }
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetDocuments()
    {
        // This would query a database of documents
        // For now, return empty array
        return Ok(new { documents = new object[] { } });
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetDocument(string id)
    {
        // This would query a database
        return NotFound(new { error = "Document not found" });
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteDocument(string id)
    {
        // This would delete from database and storage
        return Ok(new { message = "Document deleted successfully" });
    }
}
