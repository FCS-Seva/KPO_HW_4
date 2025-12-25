using PaymentsService.Abstractions;
using PaymentsService.Api.Models;
using PaymentsService.Domain;

namespace PaymentsService.Application;

public sealed class PaymentsApplicationService : IPaymentsApplicationService
{
    private readonly IAccountsRepository _accountsRepository;
    private readonly IUserContextAccessor _userContext;

    public PaymentsApplicationService(IAccountsRepository accountsRepository, IUserContextAccessor userContext)
    {
        _accountsRepository = accountsRepository;
        _userContext = userContext;
    }

    public async Task<bool> CreateAccountAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var account = new Account
        {
            UserId = _userContext.UserId,
            Balance = 0m,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        return await _accountsRepository.CreateAsync(account, ct);
    }

    public async Task<bool> TopUpAsync(TopUpRequest request, CancellationToken ct)
    {
        if (request.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Amount), "Amount must be positive.");
        }

        return await _accountsRepository.TopUpAsync(_userContext.UserId, request.Amount, ct);
    }

    public async Task<BalanceResponse?> GetBalanceAsync(CancellationToken ct)
    {
        var bal = await _accountsRepository.GetBalanceAsync(_userContext.UserId, ct);
        if (bal is null) return null;
        return new BalanceResponse(bal.Value);
    }
}
