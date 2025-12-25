namespace OrdersService.Api.Models;

public sealed record CreateOrderResponse(Guid OrderId, string Status);
