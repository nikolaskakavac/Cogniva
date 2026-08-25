using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cogniva.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageSourceCascadeAndUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MessageSources_DocumentChunks_DocumentChunkId",
                table: "MessageSources");

            migrationBuilder.DropForeignKey(
                name: "FK_MessageSources_Documents_DocumentId",
                table: "MessageSources");

            migrationBuilder.CreateIndex(
                name: "IX_MessageSources_MessageId_DocumentChunkId",
                table: "MessageSources",
                columns: new[] { "MessageId", "DocumentChunkId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MessageSources_DocumentChunks_DocumentChunkId",
                table: "MessageSources",
                column: "DocumentChunkId",
                principalTable: "DocumentChunks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MessageSources_Documents_DocumentId",
                table: "MessageSources",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MessageSources_DocumentChunks_DocumentChunkId",
                table: "MessageSources");

            migrationBuilder.DropForeignKey(
                name: "FK_MessageSources_Documents_DocumentId",
                table: "MessageSources");

            migrationBuilder.DropIndex(
                name: "IX_MessageSources_MessageId_DocumentChunkId",
                table: "MessageSources");

            migrationBuilder.AddForeignKey(
                name: "FK_MessageSources_DocumentChunks_DocumentChunkId",
                table: "MessageSources",
                column: "DocumentChunkId",
                principalTable: "DocumentChunks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MessageSources_Documents_DocumentId",
                table: "MessageSources",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
