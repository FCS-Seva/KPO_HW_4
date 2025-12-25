using Shared.Contracts.Messaging;

namespace PaymentsService.Abstractions;

public interface IMessagePublisher
{
    Task PublishPaymentResultAsync(PaymentResultEvent evt, CancellationToken ct);
}
