namespace PaymentsService.Domain;

public sealed class PaymentTransaction
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentTransactionStatus Status { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
