using Frontend.Abstractions;
using Frontend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Frontend.Pages;

public sealed class IndexModel : PageModel
{
    private readonly IGozonApiClient _api;

    public IndexModel(IGozonApiClient api)
    {
        _api = api;
    }

    public string UserId { get; private set; } = "user-1";

    public bool BalanceOk { get; private set; }
    public decimal Balance { get; private set; }
    public string? BalanceError { get; private set; }

    public IReadOnlyList<OrderListItem> LatestOrders { get; private set; } = Array.Empty<OrderListItem>();

    public async Task OnGet(CancellationToken ct)
    {
        UserId = Request.Cookies["userId"] ?? "user-1";
        await LoadSummaryAsync(ct);
    }

    public IActionResult OnPostSetUserId([FromForm] string? userId)
    {
        var value = string.IsNullOrWhiteSpace(userId) ? "user-1" : userId.Trim();
        Response.Cookies.Append("userId", value, new CookieOptions { HttpOnly = false, IsEssential = true });

        TempData["Toast.Type"] = "ok";
        TempData["Toast.Title"] = "UserId set";
        TempData["Toast.Message"] = $"Using {value}";

        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostCreateAccount(CancellationToken ct)
    {
        UserId = Request.Cookies["userId"] ?? "user-1";
        var res = await _api.CreateAccountAsync(UserId, ct);

        TempData["Toast.Type"] = res.Ok ? "ok" : "err";
        TempData["Toast.Title"] = "Create account";
        TempData["Toast.Message"] = res.Ok ? "Account created." : res.Error;

        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostTopUp([FromForm] decimal amount, CancellationToken ct)
    {
        UserId = Request.Cookies["userId"] ?? "user-1";
        var res = await _api.TopUpAsync(UserId, amount, ct);

        TempData["Toast.Type"] = res.Ok ? "ok" : "err";
        TempData["Toast.Title"] = "Top up";
        TempData["Toast.Message"] = res.Ok ? "Top up done." : res.Error;

        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostCreateOrder([FromForm] decimal amount, [FromForm] string? description, CancellationToken ct)
    {
        UserId = Request.Cookies["userId"] ?? "user-1";
        var res = await _api.CreateOrderAsync(UserId, amount, description, ct);

        TempData["Toast.Type"] = res.Ok ? "ok" : "err";
        TempData["Toast.Title"] = "Create order";
        TempData["Toast.Message"] = res.Ok
            ? $"Order created: {res.Data}"
            : res.Error;

        if (res.Ok && res.Data != Guid.Empty)
        {
            return RedirectToPage("/Order", new { id = res.Data });
        }

        return RedirectToPage("/Index");
    }

    private async Task LoadSummaryAsync(CancellationToken ct)
    {
        var bal = await _api.GetBalanceAsync(UserId, ct);
        BalanceOk = bal.Ok;
        Balance = bal.Data;
        BalanceError = bal.Error;

        var orders = await _api.GetOrdersAsync(UserId, ct);
        LatestOrders = (orders.Data ?? Array.Empty<OrderListItem>())
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(5)
            .ToList();
    }
}
