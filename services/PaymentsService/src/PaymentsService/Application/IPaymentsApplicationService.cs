using PaymentsService.Api.Models;

namespace PaymentsService.Application;

public interface IPaymentsApplicationService
{
    Task<bool> CreateAccountAsync(CancellationToken ct);
    Task<bool> TopUpAsync(TopUpRequest request, CancellationToken ct);
    Task<BalanceResponse?> GetBalanceAsync(CancellationToken ct);
}
