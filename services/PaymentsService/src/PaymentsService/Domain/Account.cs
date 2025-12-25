namespace PaymentsService.Domain;

public sealed class Account
{
    public string UserId { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
