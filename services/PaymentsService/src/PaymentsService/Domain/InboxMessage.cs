namespace PaymentsService.Domain;

public sealed class InboxMessage
{
    public Guid Id { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public InboxMessageStatus Status { get; set; }
    public string? LastError { get; set; }
}
