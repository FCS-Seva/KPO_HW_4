using Frontend.Models;

namespace Frontend.Abstractions;

public interface IGozonApiClient
{
    Task<ApiResult> CreateAccountAsync(string userId, CancellationToken ct);
    Task<ApiResult> TopUpAsync(string userId, decimal amount, CancellationToken ct);
    Task<ApiResult<decimal>> GetBalanceAsync(string userId, CancellationToken ct);

    Task<ApiResult<Guid>> CreateOrderAsync(string userId, decimal amount, string? description, CancellationToken ct);
    Task<ApiResult<IReadOnlyList<OrderListItem>>> GetOrdersAsync(string userId, CancellationToken ct);
    Task<ApiResult<OrderDetails>> GetOrderAsync(string userId, Guid orderId, CancellationToken ct);
}
