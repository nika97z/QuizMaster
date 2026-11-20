using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizMaster.Infrastructure.Migrations;

/// <inheritdoc />
public partial class _20261118132939_RemovePasswordSalt : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PasswordSalt",
            table: "Users");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PasswordSalt",
            table: "Users",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: "");
    }
}
