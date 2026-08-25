namespace ProcureLive.Contracts.Common;

public interface IPagedRequest
{
    int Page { get; }

    int PageSize { get; }
}

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
