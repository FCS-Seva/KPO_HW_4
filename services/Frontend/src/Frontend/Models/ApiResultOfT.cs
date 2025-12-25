namespace Frontend.Models;

public sealed record ApiResult<T>(bool Ok, T? Data, string? Error);
