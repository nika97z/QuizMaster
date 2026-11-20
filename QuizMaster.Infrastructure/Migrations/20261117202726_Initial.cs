using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizMaster.Infrastructure.Migrations;

/// <inheritdoc />
public partial class _20261117202726_Initial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LogEntries",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                EventType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                UserId = table.Column<int>(type: "int", nullable: true),
                QuizId = table.Column<int>(type: "int", nullable: true),
                AttemptId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LogEntries", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Username = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                PasswordSalt = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Quizzes",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Topic = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                TimeLimitMinutes = table.Column<int>(type: "int", nullable: false),
                PassPercent = table.Column<int>(type: "int", nullable: false),
                IsPublished = table.Column<bool>(type: "bit", nullable: false),
                LecturerId = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Quizzes", x => x.Id);
                table.CheckConstraint("CK_Quizzes_PassPercent", "[PassPercent] BETWEEN 1 AND 100");
                table.CheckConstraint("CK_Quizzes_TimeLimitMinutes", "[TimeLimitMinutes] BETWEEN 1 AND 300");
                table.ForeignKey(
                    name: "FK_Quizzes_Users_LecturerId",
                    column: x => x.LecturerId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Attempts",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                QuizId = table.Column<int>(type: "int", nullable: false),
                StudentId = table.Column<int>(type: "int", nullable: false),
                StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                Deadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Score = table.Column<int>(type: "int", nullable: false),
                MaxScore = table.Column<int>(type: "int", nullable: false),
                Percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                Result = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                CertificateCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                QuestionOrder = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Attempts", x => x.Id);
                table.CheckConstraint("CK_Attempts_Percent", "[Percent] BETWEEN 0 AND 100");
                table.CheckConstraint("CK_Attempts_Score", "[Score] >= 0 AND [Score] <= [MaxScore]");
                table.ForeignKey(
                    name: "FK_Attempts_Quizzes_QuizId",
                    column: x => x.QuizId,
                    principalTable: "Quizzes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Attempts_Users_StudentId",
                    column: x => x.StudentId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Questions",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                QuizId = table.Column<int>(type: "int", nullable: false),
                Text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                Points = table.Column<int>(type: "int", nullable: false),
                OrderIndex = table.Column<int>(type: "int", nullable: false),
                Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                CorrectOptionIndexes = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                CorrectOptionIndex = table.Column<int>(type: "int", nullable: true),
                CorrectValue = table.Column<bool>(type: "bit", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Questions", x => x.Id);
                table.CheckConstraint("CK_Questions_Points", "[Points] > 0");
                table.ForeignKey(
                    name: "FK_Questions_Quizzes_QuizId",
                    column: x => x.QuizId,
                    principalTable: "Quizzes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AnswerRecords",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                AttemptId = table.Column<int>(type: "int", nullable: false),
                QuestionId = table.Column<int>(type: "int", nullable: false),
                SelectedOptionIndexes = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                PointsAwarded = table.Column<int>(type: "int", nullable: false),
                AnsweredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AnswerRecords", x => x.Id);
                table.ForeignKey(
                    name: "FK_AnswerRecords_Attempts_AttemptId",
                    column: x => x.AttemptId,
                    principalTable: "Attempts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_AnswerRecords_Questions_QuestionId",
                    column: x => x.QuestionId,
                    principalTable: "Questions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "QuestionOptions",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                QuestionId = table.Column<int>(type: "int", nullable: false),
                Position = table.Column<int>(type: "int", nullable: false),
                Text = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_QuestionOptions", x => x.Id);
                table.ForeignKey(
                    name: "FK_QuestionOptions_Questions_QuestionId",
                    column: x => x.QuestionId,
                    principalTable: "Questions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AnswerRecords_AttemptId_QuestionId",
            table: "AnswerRecords",
            columns: new[] { "AttemptId", "QuestionId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AnswerRecords_QuestionId",
            table: "AnswerRecords",
            column: "QuestionId");

        migrationBuilder.CreateIndex(
            name: "IX_Attempts_CertificateCode",
            table: "Attempts",
            column: "CertificateCode",
            unique: true,
            filter: "[CertificateCode] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Attempts_QuizId",
            table: "Attempts",
            column: "QuizId");

        migrationBuilder.CreateIndex(
            name: "IX_Attempts_StudentId_QuizId_Status",
            table: "Attempts",
            columns: new[] { "StudentId", "QuizId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_LogEntries_Timestamp",
            table: "LogEntries",
            column: "Timestamp");

        migrationBuilder.CreateIndex(
            name: "IX_QuestionOptions_QuestionId_Position",
            table: "QuestionOptions",
            columns: new[] { "QuestionId", "Position" });

        migrationBuilder.CreateIndex(
            name: "IX_Questions_QuizId",
            table: "Questions",
            column: "QuizId");

        migrationBuilder.CreateIndex(
            name: "IX_Quizzes_LecturerId",
            table: "Quizzes",
            column: "LecturerId");

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_Username",
            table: "Users",
            column: "Username",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AnswerRecords");

        migrationBuilder.DropTable(
            name: "LogEntries");

        migrationBuilder.DropTable(
            name: "QuestionOptions");

        migrationBuilder.DropTable(
            name: "Attempts");

        migrationBuilder.DropTable(
            name: "Questions");

        migrationBuilder.DropTable(
            name: "Quizzes");

        migrationBuilder.DropTable(
            name: "Users");
    }
}
