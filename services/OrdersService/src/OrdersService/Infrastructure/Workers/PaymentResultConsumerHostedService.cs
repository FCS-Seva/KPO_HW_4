using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OrdersService.Abstractions;
using OrdersService.Domain;
using OrdersService.Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Messaging;

namespace OrdersService.Infrastructure.Workers;

public sealed class PaymentResultConsumerHostedService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceProvider _sp;
    private readonly IRabbitMqConnectionProvider _connProvider;
    private readonly MessagingOptions _options;
    private readonly ILogger<PaymentResultConsumerHostedService> _logger;

    private IConnection? _connection;
    private IModel? _channel;

    public PaymentResultConsumerHostedService(
        IServiceProvider sp,
        IRabbitMqConnectionProvider connProvider,
        IOptions<MessagingOptions> options,
        ILogger<PaymentResultConsumerHostedService> logger)
    {
        _sp = sp;
        _connProvider = connProvider;
        _options = options.Value;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _connection = _connProvider.GetConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(_options.ResultsExchange, ExchangeType.Direct, durable: true, autoDelete: false);
        _channel.QueueDeclare(_options.ResultsQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(_options.ResultsQueue, _options.ResultsExchange, _options.ResultsRoutingKey);
        _channel.BasicQos(0, 10, false);

        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel is null) throw new InvalidOperationException("RabbitMQ channel not initialized.");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnMessageAsync;

        _channel.BasicConsume(queue: _options.ResultsQueue, autoAck: false, consumer: consumer);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

    }

    private async Task OnMessageAsync(object sender, BasicDeliverEventArgs ea)
    {
        if (_channel is null) return;

        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var evt = JsonSerializer.Deserialize<PaymentResultEvent>(json, JsonOptions);
            if (evt is null)
            {
                _channel.BasicAck(ea.DeliveryTag, multiple: false);
                return;
            }

            using var scope = _sp.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IOrdersRepository>();

            var newStatus = evt.Status switch
            {
                PaymentResultStatus.Succeeded => OrderStatus.Finished,
                PaymentResultStatus.Failed => OrderStatus.Cancelled,
                _ => OrderStatus.Cancelled
            };

            await repo.TryUpdateStatusAsync(evt.OrderId, newStatus, CancellationToken.None);
_channel.BasicAck(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process payment result message.");
            _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _channel?.Close();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to close RabbitMQ channel.");
        }

        try
        {
            _channel?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to dispose RabbitMQ channel.");
        }
        return base.StopAsync(cancellationToken);
    }
}
