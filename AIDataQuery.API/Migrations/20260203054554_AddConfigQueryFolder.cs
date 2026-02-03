using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIDataQuery.API.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigQueryFolder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FolderId",
                table: "ConfigQueries",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConfigQueryFolders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigQueryFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfigQueryFolders_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigQueries_FolderId",
                table: "ConfigQueries",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigQueryFolders_CreatedBy",
                table: "ConfigQueryFolders",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_ConfigQueries_ConfigQueryFolders_FolderId",
                table: "ConfigQueries",
                column: "FolderId",
                principalTable: "ConfigQueryFolders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConfigQueries_ConfigQueryFolders_FolderId",
                table: "ConfigQueries");

            migrationBuilder.DropTable(
                name: "ConfigQueryFolders");

            migrationBuilder.DropIndex(
                name: "IX_ConfigQueries_FolderId",
                table: "ConfigQueries");

            migrationBuilder.DropColumn(
                name: "FolderId",
                table: "ConfigQueries");
        }
    }
}
