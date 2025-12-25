using Shared.Contracts.Messaging;

namespace PaymentsService.Abstractions;

public interface IPaymentProcessor
{
    Task ProcessAsync(PayOrderCommand command, CancellationToken ct);
}
