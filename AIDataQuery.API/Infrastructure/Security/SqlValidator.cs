using System.Text.RegularExpressions;

namespace AIDataQuery.API.Infrastructure.Security;

public class SqlValidator : ISqlValidator
{
    private static readonly string[] ForbiddenKeywords =
    {
        "INSERT", "UPDATE", "DELETE", "DROP", "TRUNCATE",
        "CREATE", "ALTER", "EXEC", "EXECUTE", "GRANT", "REVOKE",
        "DENY", "BACKUP", "RESTORE", "SHUTDOWN", "KILL",
        "SP_", "XP_", "DBCC", "BULK", "OPENROWSET", "OPENDATASOURCE"
    };

    private static readonly Regex CommentPattern = new(@"(--.*$)|(/\*[\s\S]*?\*/)", RegexOptions.Multiline | RegexOptions.Compiled);

    public SqlValidationResult Validate(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return SqlValidationResult.Fail("SQL语句不能为空");
        }

        // Remove comments
        var cleanedSql = CommentPattern.Replace(sql, " ");

        // Normalize whitespace
        cleanedSql = Regex.Replace(cleanedSql, @"\s+", " ").Trim();

        // Check if it starts with SELECT
        if (!cleanedSql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            return SqlValidationResult.Fail("仅支持SELECT查询语句");
        }

        // Check for forbidden keywords
        var upperSql = cleanedSql.ToUpperInvariant();

        foreach (var keyword in ForbiddenKeywords)
        {
            // Use word boundary check to avoid false positives
            var pattern = $@"\b{keyword}\b";
            if (Regex.IsMatch(upperSql, pattern, RegexOptions.IgnoreCase))
            {
                return SqlValidationResult.Fail($"SQL语句中包含禁止的关键字: {keyword}");
            }
        }

        // Check for multiple statements (semicolon followed by another statement)
        var statements = cleanedSql.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var nonEmptyStatements = statements
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        if (nonEmptyStatements.Count > 1)
        {
            // Check if all statements are SELECT
            foreach (var stmt in nonEmptyStatements)
            {
                if (!stmt.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    return SqlValidationResult.Fail("不支持执行多条非SELECT语句");
                }
            }
        }

        // Check for potential SQL injection patterns
        if (ContainsSqlInjectionPattern(cleanedSql))
        {
            return SqlValidationResult.Fail("检测到潜在的SQL注入模式");
        }

        return SqlValidationResult.Success();
    }

    private static bool ContainsSqlInjectionPattern(string sql)
    {
        // Check for common SQL injection patterns
        var injectionPatterns = new[]
        {
            @";\s*--",                    // Statement termination followed by comment
            @"'\s*OR\s+'1'\s*=\s*'1",     // Classic OR injection
            @"'\s*OR\s+1\s*=\s*1",        // Numeric OR injection
            @"UNION\s+ALL\s+SELECT",      // UNION injection (only if suspicious context)
            @"WAITFOR\s+DELAY",           // Time-based injection
            @"BENCHMARK\s*\(",            // MySQL time-based
        };

        foreach (var pattern in injectionPatterns)
        {
            if (Regex.IsMatch(sql, pattern, RegexOptions.IgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
