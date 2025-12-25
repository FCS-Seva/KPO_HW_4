namespace PaymentsService.Domain;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid AggregateId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
