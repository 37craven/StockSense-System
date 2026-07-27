using StockSense.Application.DTOs;

namespace StockSense.Infrastructure.Services;

public interface IAiChatProvider
{
    bool IsConfigured { get; }

    Task<string?> GenerateGroundedAnswerAsync(
        string userQuestion,
        string intent,
        IReadOnlyList<RagMatch> matches,
        string localAnswer,
        CancellationToken cancellationToken = default);

    Task<string?> GenerateGeneralMotorcycleAnswerAsync(
        string userQuestion,
        string intent,
        CancellationToken cancellationToken = default);
}
