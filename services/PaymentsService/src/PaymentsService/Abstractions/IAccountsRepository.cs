using PaymentsService.Domain;

namespace PaymentsService.Abstractions;

public interface IAccountsRepository
{
    Task<bool> CreateAsync(Account account, CancellationToken ct);
    Task<bool> TopUpAsync(string userId, decimal amount, CancellationToken ct);
    Task<decimal?> GetBalanceAsync(string userId, CancellationToken ct);
    Task<bool> TryDebitAsync(string userId, decimal amount, CancellationToken ct);
}
