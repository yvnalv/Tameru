namespace Tameru.SharedKernel.Results;

/// <summary>A page of results plus the paging metadata returned by list endpoints.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}
