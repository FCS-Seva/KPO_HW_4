namespace PaymentsService.Abstractions;

public interface IUserContextAccessor
{
    string UserId { get; }
}
