using Microsoft.EntityFrameworkCore;
using PaymentsService.Abstractions;
using PaymentsService.Domain;

namespace PaymentsService.Infrastructure.Persistence;

public sealed class InboxStore : IInboxStore
{
    private readonly PaymentsDbContext _db;

    public InboxStore(PaymentsDbContext db)
    {
        _db = db;
    }

    public Task<InboxMessage?> GetAsync(Guid messageId, CancellationToken ct)
    {
        return _db.InboxMessages.FirstOrDefaultAsync(x => x.Id == messageId, ct);
    }

    public async Task AddIfMissingAsync(InboxMessage message, CancellationToken ct)
    {
        var existing = await _db.InboxMessages.FirstOrDefaultAsync(x => x.Id == message.Id, ct);
        if (existing is not null) return;

        _db.InboxMessages.Add(message);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            return;
        }
    }

    public async Task MarkProcessedAsync(Guid messageId, DateTime processedAtUtc, CancellationToken ct)
    {
        var msg = await _db.InboxMessages.FirstOrDefaultAsync(x => x.Id == messageId, ct);
        if (msg is null) return;

        msg.ProcessedAtUtc = processedAtUtc;
        msg.Status = InboxMessageStatus.Processed;
        await _db.SaveChangesAsync(ct);
    }
}
