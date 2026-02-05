using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIDataQuery.API.Migrations
{
    /// <inheritdoc />
    public partial class AddConditionGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConditionGroup",
                table: "ConfigQueryParameters",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConditionGroup",
                table: "ConfigQueryParameters");
        }
    }
}
