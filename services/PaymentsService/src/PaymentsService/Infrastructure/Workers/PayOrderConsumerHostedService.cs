using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PaymentsService.Abstractions;
using PaymentsService.Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Messaging;

namespace PaymentsService.Infrastructure.Workers;

public sealed class PayOrderConsumerHostedService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IRabbitMqConnectionProvider _connProvider;
    private readonly MessagingOptions _options;
    private readonly IServiceProvider _sp;
    private readonly ILogger<PayOrderConsumerHostedService> _logger;

    private IConnection? _connection;
    private IModel? _channel;

    public PayOrderConsumerHostedService(
        IRabbitMqConnectionProvider connProvider,
        IOptions<MessagingOptions> options,
        IServiceProvider sp,
        ILogger<PayOrderConsumerHostedService> logger)
    {
        _connProvider = connProvider;
        _options = options.Value;
        _sp = sp;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _connection = _connProvider.GetConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(_options.CommandsExchange, ExchangeType.Direct, durable: true, autoDelete: false);
        _channel.QueueDeclare(_options.CommandsQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(_options.CommandsQueue, _options.CommandsExchange, _options.CommandsRoutingKey);
        _channel.BasicQos(0, 10, false);

        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel is null) throw new InvalidOperationException("RabbitMQ channel not initialized.");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnMessageAsync;

        _channel.BasicConsume(queue: _options.CommandsQueue, autoAck: false, consumer: consumer);

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
            var cmd = JsonSerializer.Deserialize<PayOrderCommand>(json, JsonOptions);
            if (cmd is null)
            {
                _channel.BasicAck(ea.DeliveryTag, multiple: false);
                return;
            }

            using var scope = _sp.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IPaymentProcessor>();
            await processor.ProcessAsync(cmd, CancellationToken.None);

            _channel.BasicAck(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process PayOrderCommand.");
            _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        try { _channel?.Close(); }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to close RabbitMQ channel.");
        }

        try { _channel?.Dispose(); }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to dispose RabbitMQ channel.");
        }

        return base.StopAsync(cancellationToken);
    }
}
