using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Frontend.Abstractions;
using Frontend.Models;

namespace Frontend.Infrastructure;

public sealed class GozonApiClient : IGozonApiClient
{
    private static async Task<string> ReadFailureAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        var body = await resp.Content.ReadAsStringAsync(ct);
        return $"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}";
    }
    
    private readonly HttpClient _http;
    private readonly GatewayOptions _options;

    public GozonApiClient(HttpClient http, IOptions<GatewayOptions> options)
    {
        _http = http;
        _options = options.Value;
        _http.BaseAddress = new Uri(_options.BaseUrl);
    }

    private HttpRequestMessage WithUserId(HttpRequestMessage req, string userId)
    {
        req.Headers.TryAddWithoutValidation(HttpHeaderNames.UserId, userId);
        return req;
    }

    public async Task<ApiResult> CreateAccountAsync(string userId, CancellationToken ct)
    {
        var req = WithUserId(new HttpRequestMessage(HttpMethod.Post, "/api/payments/accounts"), userId);
        var resp = await _http.SendAsync(req, ct);
        return resp.IsSuccessStatusCode ? new ApiResult(true, null) : new ApiResult(false, await ReadFailureAsync(resp, ct));
    }

    public async Task<ApiResult> TopUpAsync(string userId, decimal amount, CancellationToken ct)
    {
        var req = WithUserId(new HttpRequestMessage(HttpMethod.Post, "/api/payments/accounts/topup"), userId);
        req.Content = JsonContent.Create(new { amount });
        var resp = await _http.SendAsync(req, ct);
        return resp.IsSuccessStatusCode ? new ApiResult(true, null) : new ApiResult(false, await ReadFailureAsync(resp, ct));
    }

    public async Task<ApiResult<decimal>> GetBalanceAsync(string userId, CancellationToken ct)
    {
        var req = WithUserId(new HttpRequestMessage(HttpMethod.Get, "/api/payments/accounts/balance"), userId);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            return new ApiResult<decimal>(false, default, await ReadFailureAsync(resp, ct));
        }

        var body = await resp.Content.ReadFromJsonAsync<BalanceResponse>(cancellationToken: ct);
        if (body is null)
        {
            return new ApiResult<decimal>(false, default, "Empty response body.");
        }

        return new ApiResult<decimal>(true, body.Balance, null);
    }

    public async Task<ApiResult<Guid>> CreateOrderAsync(string userId, decimal amount, string? description, CancellationToken ct)
    {
        var req = WithUserId(new HttpRequestMessage(HttpMethod.Post, "/api/orders"), userId);
        req.Content = JsonContent.Create(new { amount, description });
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            return new ApiResult<Guid>(false, default, await ReadFailureAsync(resp, ct));
        }

        var body = await resp.Content.ReadFromJsonAsync<CreateOrderResponse>(cancellationToken: ct);
        if (body is null)
        {
            return new ApiResult<Guid>(false, default, "Empty response body.");
        }

        return new ApiResult<Guid>(true, body.OrderId, null);
    }

    public async Task<ApiResult<IReadOnlyList<OrderListItem>>> GetOrdersAsync(string userId, CancellationToken ct)
    {
        var req = WithUserId(new HttpRequestMessage(HttpMethod.Get, "/api/orders"), userId);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            return new ApiResult<IReadOnlyList<OrderListItem>>(false, null, await ReadFailureAsync(resp, ct));
        }

        var body = await resp.Content.ReadFromJsonAsync<List<OrderListItem>>(cancellationToken: ct);
        return new ApiResult<IReadOnlyList<OrderListItem>>(true, body ?? new List<OrderListItem>(), null);
    }

    public async Task<ApiResult<OrderDetails>> GetOrderAsync(string userId, Guid orderId, CancellationToken ct)
    {
        var req = WithUserId(new HttpRequestMessage(HttpMethod.Get, $"/api/orders/{orderId}"), userId);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            return new ApiResult<OrderDetails>(false, null, await ReadFailureAsync(resp, ct));
        }

        var body = await resp.Content.ReadFromJsonAsync<OrderDetails>(cancellationToken: ct);
        return new ApiResult<OrderDetails>(true, body, null);
    }

    private sealed record BalanceResponse(decimal Balance);
    private sealed record CreateOrderResponse(Guid OrderId, string Status);
}
