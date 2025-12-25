namespace Frontend.Models;

public sealed record OrderDetails(Guid OrderId, decimal Amount, string Description, string Status, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
