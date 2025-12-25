using Microsoft.EntityFrameworkCore;
using PaymentsService.Abstractions;
using PaymentsService.Domain;

namespace PaymentsService.Infrastructure.Persistence;

public sealed class OutboxStore : IOutboxStore
{
    private readonly PaymentsDbContext _db;

    public OutboxStore(PaymentsDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(OutboxMessage message, CancellationToken ct)
    {
        _db.OutboxMessages.Add(message);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetUnpublishedAsync(int batchSize, CancellationToken ct)
    {
        return await _db.OutboxMessages
            .Where(x => x.PublishedAtUtc == null)
            .OrderBy(x => x.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public async Task MarkPublishedAsync(Guid messageId, DateTime publishedAtUtc, CancellationToken ct)
    {
        var msg = await _db.OutboxMessages.FirstOrDefaultAsync(x => x.Id == messageId, ct);
        if (msg is null) return;

        msg.PublishedAtUtc = publishedAtUtc;
        await _db.SaveChangesAsync(ct);
    }

    public async Task IncrementAttemptsAsync(Guid messageId, string? lastError, CancellationToken ct)
    {
        var msg = await _db.OutboxMessages.FirstOrDefaultAsync(x => x.Id == messageId, ct);
        if (msg is null) return;

        msg.Attempts += 1;
        msg.LastError = lastError;
        await _db.SaveChangesAsync(ct);
    }
}
