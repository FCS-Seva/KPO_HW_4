namespace OrdersService.Abstractions;

public interface IUserContextAccessor
{
    string UserId { get; }
}
