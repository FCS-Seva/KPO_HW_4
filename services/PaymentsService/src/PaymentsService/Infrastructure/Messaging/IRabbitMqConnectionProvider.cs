using RabbitMQ.Client;

namespace PaymentsService.Infrastructure.Messaging;

public interface IRabbitMqConnectionProvider
{
    IConnection GetConnection();
}
