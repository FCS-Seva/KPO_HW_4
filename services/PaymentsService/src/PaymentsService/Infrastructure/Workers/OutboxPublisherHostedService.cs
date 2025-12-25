using System.Text.Json;
using Microsoft.Extensions.Options;
using PaymentsService.Abstractions;
using PaymentsService.Infrastructure.Messaging;
using Shared.Contracts.Messaging;

namespace PaymentsService.Infrastructure.Workers;

public sealed class OutboxPublisherHostedService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceProvider _sp;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxPublisherHostedService> _logger;

    public OutboxPublisherHostedService(IServiceProvider sp, IOptions<OutboxOptions> options, ILogger<OutboxPublisherHostedService> logger)
    {
        _sp = sp;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
                var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

                var batch = await store.GetUnpublishedAsync(_options.BatchSize, stoppingToken);
                foreach (var msg in batch)
                {
                    try
                    {
                        var evt = JsonSerializer.Deserialize<PaymentResultEvent>(msg.PayloadJson, JsonOptions);
                        if (evt is null)
                        {
                            await store.IncrementAttemptsAsync(msg.Id, "Failed to deserialize payload.", stoppingToken);
                            continue;
                        }

                        await publisher.PublishPaymentResultAsync(evt, stoppingToken);
                        await store.MarkPublishedAsync(msg.Id, DateTime.UtcNow, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to publish outbox message {MessageId}", msg.Id);
                        await store.IncrementAttemptsAsync(msg.Id, ex.Message, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox publisher loop failed.");
            }

            await Task.Delay(_options.PollDelayMs, stoppingToken);
        }
    }
}
