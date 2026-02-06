using System.Data.Common;
using Microsoft.Data.SqlClient;
using MySqlConnector;

namespace AIDataQuery.API.Infrastructure.Database;

public static class DbConnectionFactory
{
    public static DbConnection CreateConnection(string databaseType, string connectionString)
    {
        var connStr = EnsureSslSettings(databaseType, connectionString);
        return databaseType?.ToLower() switch
        {
            "mysql" => new MySqlConnection(connStr),
            _ => new SqlConnection(connStr), // 默认 SqlServer
        };
    }

    public static DbCommand CreateCommand(string sql, DbConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        return command;
    }

    public static string EnsureSslSettings(string databaseType, string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return connectionString;

        if (databaseType?.ToLower() == "mysql")
        {
            // MySQL 连接字符串自身处理 SSL，不追加额外设置
            return connectionString;
        }

        // SQL Server: 追加 TrustServerCertificate=True
        if (connectionString.Contains("TrustServerCertificate", StringComparison.OrdinalIgnoreCase))
            return connectionString;

        var separator = connectionString.TrimEnd().EndsWith(';') ? "" : ";";
        return connectionString + separator + "TrustServerCertificate=True";
    }

    public static string GetTablesSql(string databaseType)
    {
        return databaseType?.ToLower() switch
        {
            "mysql" => @"
                SELECT TABLE_SCHEMA, TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE'
                ORDER BY TABLE_SCHEMA, TABLE_NAME",
            _ => @"
                SELECT TABLE_SCHEMA, TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'
                ORDER BY TABLE_SCHEMA, TABLE_NAME",
        };
    }

    public static string GetColumnsSql(string databaseType)
    {
        return databaseType?.ToLower() switch
        {
            "mysql" => @"
                SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @TableName
                ORDER BY ORDINAL_POSITION",
            _ => @"
                SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = @TableName
                ORDER BY ORDINAL_POSITION",
        };
    }
}
