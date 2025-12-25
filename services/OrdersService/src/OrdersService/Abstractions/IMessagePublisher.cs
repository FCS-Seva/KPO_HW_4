using Shared.Contracts.Messaging;

namespace OrdersService.Abstractions;

public interface IMessagePublisher
{
    Task PublishPayOrderAsync(PayOrderCommand command, CancellationToken ct);
}
