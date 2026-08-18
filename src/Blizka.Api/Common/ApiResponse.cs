namespace Blizka.Api.Common;

/// <summary>Envelope for every successful API response.</summary>
public sealed record ApiResponse<T>(T Data)
{
    public static ApiResponse<T> Ok(T data) => new(data);
}
