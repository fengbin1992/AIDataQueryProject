namespace AIDataQuery.API.Models.DTOs.Common;

public class QueryParams
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;

    public int PageIndex { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    public string? Keyword { get; set; }
}
