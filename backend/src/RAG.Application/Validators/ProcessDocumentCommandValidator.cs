using FluentValidation;
using RAG.Application.Commands;

namespace RAG.Application.Validators;

public class ProcessDocumentCommandValidator : AbstractValidator<ProcessDocumentCommand>
{
    private static readonly string[] AllowedContentTypes = new[]
    {
        "application/pdf",
        "application/x-pdf"
    };

    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50MB

    public ProcessDocumentCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required")
            .Must(BeValidFileName).WithMessage("Invalid file name")
            .Must(BePdfFile).WithMessage("Only PDF files are supported");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("File content is required")
            .Must(content => content.Length <= MaxFileSizeBytes)
            .WithMessage($"File size must not exceed {MaxFileSizeBytes / 1024 / 1024}MB");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("Content type is required")
            .Must(ct => AllowedContentTypes.Contains(ct.ToLowerInvariant()))
            .WithMessage("Only PDF files are supported");
    }

    private bool BeValidFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        // Check for invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        return !fileName.Any(c => invalidChars.Contains(c));
    }

    private bool BePdfFile(string fileName)
    {
        return fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
    }
}
