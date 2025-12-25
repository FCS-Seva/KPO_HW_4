using System.Text.Json;
using OrdersService.Abstractions;
using OrdersService.Api.Models;
using OrdersService.Domain;
using Shared.Contracts.Messaging;

namespace OrdersService.Application;

public sealed class OrdersApplicationService : IOrdersApplicationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IOrdersRepository _ordersRepository;
    private readonly IOrderCreationStore _creationStore;
        private readonly IUserContextAccessor _userContext;

    public OrdersApplicationService(IOrdersRepository ordersRepository, IOrderCreationStore creationStore, IUserContextAccessor userContext)
    {
        _ordersRepository = ordersRepository;
        _creationStore = creationStore;
        _userContext = userContext;
    }

    public async Task<CreateOrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct)
    {
        if (request.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Amount), "Amount must be positive.");
        }

        var now = DateTime.UtcNow;
        var orderId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            UserId = _userContext.UserId,
            Amount = request.Amount,
            Description = request.Description ?? string.Empty,
            Status = OrderStatus.New,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var command = new PayOrderCommand(
            MessageId: Guid.NewGuid(),
            OrderId: orderId,
            UserId: _userContext.UserId,
            Amount: request.Amount,
            CreatedAtUtc: now
        );

        var outbox = new OutboxMessage
        {
            Id = command.MessageId,
            AggregateId = orderId,
            Type = nameof(PayOrderCommand),
            PayloadJson = JsonSerializer.Serialize(command, JsonOptions),
            CreatedAtUtc = now,
            Attempts = 0
        };

        await _creationStore.CreateOrderWithOutboxAsync(order, outbox, ct);
return new CreateOrderResponse(orderId, order.Status.ToString());
    }

    public async Task<IReadOnlyList<OrderListItemResponse>> GetOrdersAsync(CancellationToken ct)
    {
        var orders = await _ordersRepository.GetByUserAsync(_userContext.UserId, ct);
        return orders
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new OrderListItemResponse(x.Id, x.Amount, x.Status.ToString(), x.CreatedAtUtc))
            .ToList();
    }

    public async Task<OrderDetailsResponse?> GetOrderAsync(Guid orderId, CancellationToken ct)
    {
        var order = await _ordersRepository.GetByIdAsync(orderId, _userContext.UserId, ct);
        if (order is null) return null;

        return new OrderDetailsResponse(order.Id, order.Amount, order.Description, order.Status.ToString(), order.CreatedAtUtc, order.UpdatedAtUtc);
    }
}
