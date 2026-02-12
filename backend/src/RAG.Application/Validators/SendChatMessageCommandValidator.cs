using FluentValidation;
using RAG.Application.Commands;

namespace RAG.Application.Validators;

public class SendChatMessageCommandValidator : AbstractValidator<SendChatMessageCommand>
{
    private const int MaxMessageLength = 4000;
    private const int MinTopK = 1;
    private const int MaxTopK = 20;

    public SendChatMessageCommandValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required")
            .MaximumLength(MaxMessageLength)
            .WithMessage($"Message must not exceed {MaxMessageLength} characters")
            .Must(NotContainPII).WithMessage("Message appears to contain personally identifiable information");

        RuleFor(x => x.TopK)
            .InclusiveBetween(MinTopK, MaxTopK)
            .WithMessage($"TopK must be between {MinTopK} and {MaxTopK}");
    }

    private bool NotContainPII(string message)
    {
        // Basic PII detection patterns
        var patterns = new[]
        {
            @"\b\d{3}-\d{2}-\d{4}\b", // SSN
            @"\b\d{16}\b", // Credit card (simple)
            @"\b\d{3}-\d{3}-\d{4}\b" // Phone number
        };

        return !patterns.Any(pattern =>
            System.Text.RegularExpressions.Regex.IsMatch(message, pattern));
    }
}
