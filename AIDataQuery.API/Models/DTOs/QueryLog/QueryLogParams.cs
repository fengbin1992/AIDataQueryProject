using AIDataQuery.API.Models.DTOs.Common;
using AIDataQuery.API.Models.Enums;

namespace AIDataQuery.API.Models.DTOs.QueryLog;

public class QueryLogParams : QueryParams
{
    public string? PlatformCode { get; set; }
    public QueryStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
