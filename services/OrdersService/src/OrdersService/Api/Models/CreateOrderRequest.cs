namespace OrdersService.Api.Models;

public sealed record CreateOrderRequest(decimal Amount, string? Description);
