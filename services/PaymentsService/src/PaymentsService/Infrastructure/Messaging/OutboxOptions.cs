namespace PaymentsService.Infrastructure.Messaging;

public sealed class OutboxOptions
{
    public int BatchSize { get; set; } = 20;
    public int PollDelayMs { get; set; } = 750;
}
