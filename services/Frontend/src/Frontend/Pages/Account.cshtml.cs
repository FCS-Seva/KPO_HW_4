using Frontend.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Frontend.Pages;

public sealed class AccountModel : PageModel
{
    private readonly IGozonApiClient _api;

    public AccountModel(IGozonApiClient api)
    {
        _api = api;
    }

    public string UserId { get; private set; } = "user-1";
    public bool BalanceOk { get; private set; }
    public decimal Balance { get; private set; }
    public string? BalanceError { get; private set; }

    public async Task OnGet(CancellationToken ct)
    {
        UserId = Request.Cookies["userId"] ?? "user-1";
        await LoadBalanceAsync(ct);
    }

    public async Task<IActionResult> OnPostCreateAccount(CancellationToken ct)
    {
        UserId = Request.Cookies["userId"] ?? "user-1";
        var res = await _api.CreateAccountAsync(UserId, ct);

        TempData["Toast.Type"] = res.Ok ? "ok" : "err";
        TempData["Toast.Title"] = "Create account";
        TempData["Toast.Message"] = res.Ok ? "Account created." : res.Error;

        return RedirectToPage("/Account");
    }

    public async Task<IActionResult> OnPostTopUp([FromForm] decimal amount, CancellationToken ct)
    {
        UserId = Request.Cookies["userId"] ?? "user-1";
        var res = await _api.TopUpAsync(UserId, amount, ct);

        TempData["Toast.Type"] = res.Ok ? "ok" : "err";
        TempData["Toast.Title"] = "Top up";
        TempData["Toast.Message"] = res.Ok ? "Top up done." : res.Error;

        return RedirectToPage("/Account");
    }

    public async Task<IActionResult> OnPostRefreshBalance(CancellationToken ct)
    {
        UserId = Request.Cookies["userId"] ?? "user-1";
        var bal = await _api.GetBalanceAsync(UserId, ct);

        TempData["Toast.Type"] = bal.Ok ? "ok" : "err";
        TempData["Toast.Title"] = "Balance";
        TempData["Toast.Message"] = bal.Ok ? $"Balance: {bal.Data}" : bal.Error;

        return RedirectToPage("/Account");
    }

    private async Task LoadBalanceAsync(CancellationToken ct)
    {
        var bal = await _api.GetBalanceAsync(UserId, ct);
        BalanceOk = bal.Ok;
        Balance = bal.Data;
        BalanceError = bal.Error;
    }
}
