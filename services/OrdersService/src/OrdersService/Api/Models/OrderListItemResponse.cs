namespace OrdersService.Api.Models;

public sealed record OrderListItemResponse(Guid OrderId, decimal Amount, string Status, DateTime CreatedAtUtc);
