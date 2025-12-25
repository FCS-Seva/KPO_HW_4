using Frontend.Abstractions;
using Frontend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Frontend.Pages;

public sealed class OrdersModel : PageModel
{
    private readonly IGozonApiClient _api;

    public OrdersModel(IGozonApiClient api)
    {
        _api = api;
    }

    public string UserId { get; private set; } = "user-1";
    public IReadOnlyList<OrderListItem> Orders { get; private set; } = Array.Empty<OrderListItem>();

    public async Task OnGet(CancellationToken ct)
    {
        UserId = Request.Cookies["userId"] ?? "user-1";
        await LoadOrdersAsync(ct);
    }

    public async Task<IActionResult> OnPostCreateOrder([FromForm] decimal amount, [FromForm] string? description, CancellationToken ct)
    {
        UserId = Request.Cookies["userId"] ?? "user-1";
        var res = await _api.CreateOrderAsync(UserId, amount, description, ct);

        TempData["Toast.Type"] = res.Ok ? "ok" : "err";
        TempData["Toast.Title"] = "Create order";
        TempData["Toast.Message"] = res.Ok ? $"Order created: {res.Data}" : res.Error;

        if (res.Ok && res.Data != Guid.Empty)
        {
            return RedirectToPage("/Order", new { id = res.Data });
        }

        return RedirectToPage("/Orders");
    }

    public async Task<IActionResult> OnGetOrdersJson(CancellationToken ct)
    {
        UserId = Request.Cookies["userId"] ?? "user-1";
        var res = await _api.GetOrdersAsync(UserId, ct);
        if (!res.Ok)
        {
            return new JsonResult(Array.Empty<OrderListItem>());
        }

        return new JsonResult(res.Data ?? Array.Empty<OrderListItem>());
    }

    private async Task LoadOrdersAsync(CancellationToken ct)
    {
        var res = await _api.GetOrdersAsync(UserId, ct);
        Orders = (res.Data ?? Array.Empty<OrderListItem>())
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToList();
    }
}
