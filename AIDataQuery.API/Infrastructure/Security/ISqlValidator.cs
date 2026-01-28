namespace AIDataQuery.API.Infrastructure.Security;

public interface ISqlValidator
{
    SqlValidationResult Validate(string sql);
}

public class SqlValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }

    public static SqlValidationResult Success() => new() { IsValid = true };
    public static SqlValidationResult Fail(string message) => new() { IsValid = false, ErrorMessage = message };
}
