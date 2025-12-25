using Microsoft.EntityFrameworkCore;
using PaymentsService.Abstractions;
using PaymentsService.Domain;

namespace PaymentsService.Infrastructure.Persistence;

public sealed class AccountsRepository : IAccountsRepository
{
    private readonly PaymentsDbContext _db;

    public AccountsRepository(PaymentsDbContext db)
    {
        _db = db;
    }

    public async Task<bool> CreateAsync(Account account, CancellationToken ct)
    {
        _db.Accounts.Add(account);
        try
        {
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    public async Task<bool> TopUpAsync(string userId, decimal amount, CancellationToken ct)
    {
        var affected = await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE accounts SET balance = balance + {amount}, updated_at_utc = {DateTime.UtcNow} WHERE user_id = {userId};",
            ct);

        return affected == 1;
    }

    public async Task<decimal?> GetBalanceAsync(string userId, CancellationToken ct)
    {
        var account = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, ct);
        return account?.Balance;
    }

    public async Task<bool> TryDebitAsync(string userId, decimal amount, CancellationToken ct)
    {
        var affected = await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE accounts SET balance = balance - {amount}, updated_at_utc = {DateTime.UtcNow} WHERE user_id = {userId} AND balance >= {amount};",
            ct);

        return affected == 1;
    }
}
