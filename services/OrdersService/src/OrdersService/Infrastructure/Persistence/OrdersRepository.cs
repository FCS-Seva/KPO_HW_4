using Microsoft.EntityFrameworkCore;
using OrdersService.Abstractions;
using OrdersService.Domain;

namespace OrdersService.Infrastructure.Persistence;

public sealed class OrdersRepository : IOrdersRepository
{
    private readonly OrdersDbContext _db;

    public OrdersRepository(OrdersDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Order order, CancellationToken ct)
    {
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Order>> GetByUserAsync(string userId, CancellationToken ct)
    {
        return await _db.Orders
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<Order?> GetByIdAsync(Guid orderId, string userId, CancellationToken ct)
    {
        return await _db.Orders.FirstOrDefaultAsync(x => x.Id == orderId && x.UserId == userId, ct);
    }

    public async Task<bool> TryUpdateStatusAsync(Guid orderId, OrderStatus newStatus, CancellationToken ct)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == orderId, ct);
        if (order is null) return false;

        if (order.Status != OrderStatus.New) return true;

        order.Status = newStatus;
        order.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
