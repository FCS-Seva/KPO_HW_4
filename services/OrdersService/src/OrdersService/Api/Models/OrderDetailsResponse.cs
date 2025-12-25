namespace OrdersService.Api.Models;

public sealed record OrderDetailsResponse(Guid OrderId, decimal Amount, string Description, string Status, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
