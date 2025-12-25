using Microsoft.AspNetCore.Mvc;
using OrdersService.Api.Models;
using OrdersService.Application;

namespace OrdersService.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrdersApplicationService _service;

    public OrdersController(IOrdersApplicationService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<CreateOrderResponse>> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var result = await _service.CreateOrderAsync(request, ct);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderListItemResponse>>> List(CancellationToken ct)
    {
        var result = await _service.GetOrdersAsync(ct);
        return Ok(result);
    }

    [HttpGet("{orderId:guid}")]
    public async Task<ActionResult<OrderDetailsResponse>> Get([FromRoute] Guid orderId, CancellationToken ct)
    {
        var result = await _service.GetOrderAsync(orderId, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }
}
