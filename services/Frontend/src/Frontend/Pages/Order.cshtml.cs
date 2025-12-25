using Frontend.Abstractions;
using Frontend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Frontend.Pages;

public sealed class OrderModel : PageModel
{
    private readonly IGozonApiClient _api;

    public OrderModel(IGozonApiClient api)
    {
        _api = api;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public OrderDetails? Order { get; private set; }

    public async Task OnGet(CancellationToken ct)
    {
        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnPost(CancellationToken ct)
    {
        await LoadAsync(ct);

        TempData["Toast.Type"] = "ok";
        TempData["Toast.Title"] = "Order";
        TempData["Toast.Message"] = "Refreshed.";

        return RedirectToPage("/Order", new { id = Id });
    }

    public async Task<IActionResult> OnGetOrderJson(CancellationToken ct)
    {
        var userId = Request.Cookies["userId"] ?? "user-1";
        var res = await _api.GetOrderAsync(userId, Id, ct);
        if (!res.Ok || res.Data is null)
        {
            return new JsonResult(null);
        }

        return new JsonResult(res.Data);
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var userId = Request.Cookies["userId"] ?? "user-1";
        var res = await _api.GetOrderAsync(userId, Id, ct);
        Order = res.Data;

        if (!res.Ok)
        {
            TempData["Toast.Type"] = "err";
            TempData["Toast.Title"] = "Order";
            TempData["Toast.Message"] = res.Error;
        }
    }
}
