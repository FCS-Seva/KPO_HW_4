using PaymentsService.Domain;

namespace PaymentsService.Abstractions;

public interface IInboxStore
{
    Task<InboxMessage?> GetAsync(Guid messageId, CancellationToken ct);
    Task AddIfMissingAsync(InboxMessage message, CancellationToken ct);
    Task MarkProcessedAsync(Guid messageId, DateTime processedAtUtc, CancellationToken ct);
}
