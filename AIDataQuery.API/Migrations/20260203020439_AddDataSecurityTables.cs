using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIDataQuery.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDataSecurityTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 先尝试删除旧表（如果存在）
            migrationBuilder.Sql("DROP TABLE IF EXISTS `SensitiveAccessLogs`;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS `SensitiveAccessPermissions`;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS `SensitiveAccessRequests`;");

            // 创建 SensitiveMaskingRules 表（如果不存在）
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS `SensitiveMaskingRules` (
                    `Id` int NOT NULL AUTO_INCREMENT,
                    `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
                    `FieldPattern` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
                    `MaskType` int NOT NULL,
                    `MaskConfig` varchar(1000) CHARACTER SET utf8mb4 NULL,
                    `Priority` int NOT NULL DEFAULT 0,
                    `IsActive` tinyint(1) NOT NULL DEFAULT 1,
                    `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
                    `CreatedAt` datetime(6) NOT NULL,
                    `UpdatedAt` datetime(6) NULL,
                    PRIMARY KEY (`Id`),
                    INDEX `IX_SensitiveMaskingRules_IsActive_Priority` (`IsActive`, `Priority`)
                ) CHARACTER SET=utf8mb4;
            ");

            // 创建 SensitiveFieldMarks 表（如果不存在）
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS `SensitiveFieldMarks` (
                    `Id` int NOT NULL AUTO_INCREMENT,
                    `ConnectionId` int NOT NULL,
                    `TableName` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
                    `FieldName` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
                    `MaskType` int NOT NULL,
                    `MaskConfig` varchar(1000) CHARACTER SET utf8mb4 NULL,
                    `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
                    `MarkedBy` int NOT NULL,
                    `CreatedAt` datetime(6) NOT NULL,
                    `UpdatedAt` datetime(6) NULL,
                    PRIMARY KEY (`Id`),
                    UNIQUE INDEX `IX_SensitiveFieldMarks_ConnectionId_TableName_FieldName` (`ConnectionId`, `TableName`, `FieldName`),
                    INDEX `IX_SensitiveFieldMarks_MarkedBy` (`MarkedBy`),
                    CONSTRAINT `FK_SensitiveFieldMarks_DatabaseConnections_ConnectionId` FOREIGN KEY (`ConnectionId`) REFERENCES `DatabaseConnections` (`Id`) ON DELETE CASCADE,
                    CONSTRAINT `FK_SensitiveFieldMarks_Users_MarkedBy` FOREIGN KEY (`MarkedBy`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT
                ) CHARACTER SET=utf8mb4;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SensitiveFieldMarks");
            migrationBuilder.DropTable(name: "SensitiveMaskingRules");
        }
    }
}
