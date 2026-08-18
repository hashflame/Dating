namespace Blizka.Api.Common;

/// <summary>Страница результатов; предназначена для использования как payload <c>Data</c> в <see cref="ApiResponse{T}"/>.</summary>
public sealed record PaginatedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public bool HasMore => (long)Page * PageSize < TotalCount;
}
