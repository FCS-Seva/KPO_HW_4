using OrdersService.Abstractions;
using OrdersService.Domain;

namespace OrdersService.Infrastructure.Persistence;

public sealed class OrderCreationStore : IOrderCreationStore
{
    private readonly OrdersDbContext _db;

    public OrderCreationStore(OrdersDbContext db)
    {
        _db = db;
    }

    public async Task CreateOrderWithOutboxAsync(Order order, OutboxMessage outboxMessage, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        _db.Orders.Add(order);
        _db.OutboxMessages.Add(outboxMessage);

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
