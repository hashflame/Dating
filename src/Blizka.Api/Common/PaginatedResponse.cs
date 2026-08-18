namespace Blizka.Api.Common;

/// <summary>Page of results, meant to be used as the <c>Data</c> payload of an <see cref="ApiResponse{T}"/>.</summary>
public sealed record PaginatedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public bool HasMore => (long)Page * PageSize < TotalCount;
}
