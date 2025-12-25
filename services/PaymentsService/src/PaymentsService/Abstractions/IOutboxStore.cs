using PaymentsService.Domain;

namespace PaymentsService.Abstractions;

public interface IOutboxStore
{
    Task AddAsync(OutboxMessage message, CancellationToken ct);
    Task<IReadOnlyList<OutboxMessage>> GetUnpublishedAsync(int batchSize, CancellationToken ct);
    Task MarkPublishedAsync(Guid messageId, DateTime publishedAtUtc, CancellationToken ct);
    Task IncrementAttemptsAsync(Guid messageId, string? lastError, CancellationToken ct);
}
