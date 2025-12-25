using RabbitMQ.Client;

namespace OrdersService.Infrastructure.Messaging;

public interface IRabbitMqConnectionProvider
{
    IConnection GetConnection();
}
