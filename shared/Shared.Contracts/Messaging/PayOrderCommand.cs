namespace Shared.Contracts.Messaging;

public sealed record PayOrderCommand(
    Guid MessageId,
    Guid OrderId,
    string UserId,
    decimal Amount,
    DateTime CreatedAtUtc
);
