using OrdersService.Domain;

namespace OrdersService.Abstractions;

public interface IOrderCreationStore
{
    Task CreateOrderWithOutboxAsync(Order order, OutboxMessage outboxMessage, CancellationToken ct);
}
