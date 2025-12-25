namespace Frontend.Models;

public sealed record OrderListItem(Guid OrderId, decimal Amount, string Status, DateTime CreatedAtUtc);
