using OrdersService.Api.Models;

namespace OrdersService.Application;

public interface IOrdersApplicationService
{
    Task<CreateOrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct);
    Task<IReadOnlyList<OrderListItemResponse>> GetOrdersAsync(CancellationToken ct);
    Task<OrderDetailsResponse?> GetOrderAsync(Guid orderId, CancellationToken ct);
}
