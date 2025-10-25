using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MessagingService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Updating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_messages_channel_created_deleted",
                table: "messages");

            migrationBuilder.CreateIndex(
                name: "ix_messages_channel_created_deleted",
                table: "messages",
                columns: new[] { "channel_id", "created_at", "is_deleted" })
                .Annotation("Npgsql:IndexInclude", new[] { "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_messages_channel_created_deleted",
                table: "messages");

            migrationBuilder.CreateIndex(
                name: "ix_messages_channel_created_deleted",
                table: "messages",
                columns: new[] { "channel_id", "created_at", "is_deleted" });
        }
    }
}
