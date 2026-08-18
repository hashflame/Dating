namespace Blizka.Api.Common;

/// <summary>Обёртка для каждого успешного ответа API.</summary>
/// <param name="Data">Полезная нагрузка ответа.</param>
public sealed record ApiResponse<T>(T Data)
{
    public static ApiResponse<T> Ok(T data) => new(data);
}
