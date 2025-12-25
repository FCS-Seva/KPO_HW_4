using Microsoft.AspNetCore.Mvc;
using PaymentsService.Api.Models;
using PaymentsService.Application;

namespace PaymentsService.Api.Controllers;

[ApiController]
[Route("api/payments/accounts")]
public sealed class AccountsController : ControllerBase
{
    private readonly IPaymentsApplicationService _service;

    public AccountsController(IPaymentsApplicationService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var created = await _service.CreateAccountAsync(ct);
        if (!created) return Conflict("Account already exists.");
        return Ok();
    }

    [HttpPost("topup")]
    public async Task<IActionResult> TopUp([FromBody] TopUpRequest request, CancellationToken ct)
    {
        var ok = await _service.TopUpAsync(request, ct);
        if (!ok) return NotFound("Account not found.");
        return Ok();
    }

    [HttpGet("balance")]
    public async Task<ActionResult<BalanceResponse>> Balance(CancellationToken ct)
    {
        var result = await _service.GetBalanceAsync(ct);
        if (result is null) return NotFound("Account not found.");
        return Ok(result);
    }
}
