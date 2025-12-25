using OrdersService.Domain;

namespace OrdersService.Abstractions;

public interface IOrdersRepository
{
    Task AddAsync(Order order, CancellationToken ct);
    Task<IReadOnlyList<Order>> GetByUserAsync(string userId, CancellationToken ct);
    Task<Order?> GetByIdAsync(Guid orderId, string userId, CancellationToken ct);
    Task<bool> TryUpdateStatusAsync(Guid orderId, OrderStatus newStatus, CancellationToken ct);
}
