using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OrdersService.Abstractions;
using RabbitMQ.Client;
using Shared.Contracts.Messaging;

namespace OrdersService.Infrastructure.Messaging;

public sealed class RabbitMqPublisher : IMessagePublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IRabbitMqConnectionProvider _connProvider;
    private readonly MessagingOptions _options;

    public RabbitMqPublisher(IRabbitMqConnectionProvider connProvider, IOptions<MessagingOptions> options)
    {
        _connProvider = connProvider;
        _options = options.Value;
    }

    public Task PublishPayOrderAsync(PayOrderCommand command, CancellationToken ct)
    {
        var conn = _connProvider.GetConnection();
        using var channel = conn.CreateModel();

        channel.ExchangeDeclare(_options.CommandsExchange, ExchangeType.Direct, durable: true, autoDelete: false);
        channel.QueueDeclare(_options.CommandsQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(_options.CommandsQueue, _options.CommandsExchange, _options.CommandsRoutingKey);
channel.ConfirmSelect();

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(command, JsonOptions));
        var props = channel.CreateBasicProperties();
        props.Persistent = true;
        props.MessageId = command.MessageId.ToString();
        props.Type = nameof(PayOrderCommand);

        channel.BasicPublish(
            exchange: _options.CommandsExchange,
            routingKey: _options.CommandsRoutingKey,
            basicProperties: props,
            body: body);

        channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
        return Task.CompletedTask;
    }
}
