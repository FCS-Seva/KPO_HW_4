using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PaymentsService.Abstractions;
using RabbitMQ.Client;
using Shared.Contracts.Messaging;

namespace PaymentsService.Infrastructure.Messaging;

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

    public Task PublishPaymentResultAsync(PaymentResultEvent evt, CancellationToken ct)
    {
        var conn = _connProvider.GetConnection();
        using var channel = conn.CreateModel();

        channel.ExchangeDeclare(_options.ResultsExchange, ExchangeType.Direct, durable: true, autoDelete: false);
        channel.QueueDeclare(_options.ResultsQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(_options.ResultsQueue, _options.ResultsExchange, _options.ResultsRoutingKey);
channel.ConfirmSelect();

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt, JsonOptions));
        var props = channel.CreateBasicProperties();
        props.Persistent = true;
        props.MessageId = evt.MessageId.ToString();
        props.Type = nameof(PaymentResultEvent);

        channel.BasicPublish(
            exchange: _options.ResultsExchange,
            routingKey: _options.ResultsRoutingKey,
            basicProperties: props,
            body: body);

        channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
        return Task.CompletedTask;
    }
}
