using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadBatchAndSourceFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UploadBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UploadedByUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TotalFiles = table.Column<int>(type: "int", nullable: false),
                    ProcessedFiles = table.Column<int>(type: "int", nullable: false),
                    FailedFiles = table.Column<int>(type: "int", nullable: false),
                    OriginalZipFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UploadBatchFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StoredFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProcessingStartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ProcessingCompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadBatchFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadBatchFiles_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UploadBatchFiles_UploadBatches_UploadBatchId",
                        column: x => x.UploadBatchId,
                        principalTable: "UploadBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceSourceFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadBatchFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StoredFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PreviewFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceSourceFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceSourceFiles_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceSourceFiles_UploadBatchFiles_UploadBatchFileId",
                        column: x => x.UploadBatchFileId,
                        principalTable: "UploadBatchFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceSourceFiles_InvoiceId",
                table: "InvoiceSourceFiles",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceSourceFiles_InvoiceId_PageNumber",
                table: "InvoiceSourceFiles",
                columns: new[] { "InvoiceId", "PageNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceSourceFiles_UploadBatchFileId",
                table: "InvoiceSourceFiles",
                column: "UploadBatchFileId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadBatches_BatchCode",
                table: "UploadBatches",
                column: "BatchCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UploadBatches_CreatedAt",
                table: "UploadBatches",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UploadBatches_Status",
                table: "UploadBatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UploadBatchFiles_InvoiceId",
                table: "UploadBatchFiles",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadBatchFiles_Status",
                table: "UploadBatchFiles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UploadBatchFiles_UploadBatchId",
                table: "UploadBatchFiles",
                column: "UploadBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadBatchFiles_UploadBatchId_OriginalFileName",
                table: "UploadBatchFiles",
                columns: new[] { "UploadBatchId", "OriginalFileName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceSourceFiles");

            migrationBuilder.DropTable(
                name: "UploadBatchFiles");

            migrationBuilder.DropTable(
                name: "UploadBatches");
        }
    }
}
