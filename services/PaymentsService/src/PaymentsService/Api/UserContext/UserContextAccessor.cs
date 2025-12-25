using PaymentsService.Abstractions;

namespace PaymentsService.Api.UserContext;

public sealed class UserContextAccessor : IUserContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserId
    {
        get
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx is null) return string.Empty;

            if (ctx.Request.Headers.TryGetValue(PaymentsService.Api.Http.HttpHeaderNames.UserId, out var userId))
            {
                return userId.ToString();
            }

            return string.Empty;
        }
    }
}
