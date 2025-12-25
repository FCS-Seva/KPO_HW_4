namespace Shared.Contracts.Messaging;

public sealed record PaymentResultEvent(
    Guid MessageId,
    Guid OrderId,
    PaymentResultStatus Status,
    string? Reason,
    DateTime CreatedAtUtc
);
