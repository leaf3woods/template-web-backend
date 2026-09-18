using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Org.Product.Infrastructure.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class RenameSortableEnableableProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "state",
                table: "user",
                newName: "is_enabled");

            migrationBuilder.RenameColumn(
                name: "order",
                table: "user",
                newName: "sort_order");

            migrationBuilder.RenameColumn(
                name: "state",
                table: "role",
                newName: "is_enabled");

            migrationBuilder.RenameColumn(
                name: "order",
                table: "role",
                newName: "sort_order");

            migrationBuilder.RenameColumn(
                name: "state",
                table: "permission",
                newName: "is_enabled");

            migrationBuilder.RenameColumn(
                name: "order",
                table: "permission",
                newName: "sort_order");

            migrationBuilder.RenameIndex(
                name: "ix_permission_order",
                table: "permission",
                newName: "ix_permission_sort_order");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "sort_order",
                table: "user",
                newName: "order");

            migrationBuilder.RenameColumn(
                name: "is_enabled",
                table: "user",
                newName: "state");

            migrationBuilder.RenameColumn(
                name: "sort_order",
                table: "role",
                newName: "order");

            migrationBuilder.RenameColumn(
                name: "is_enabled",
                table: "role",
                newName: "state");

            migrationBuilder.RenameColumn(
                name: "sort_order",
                table: "permission",
                newName: "order");

            migrationBuilder.RenameColumn(
                name: "is_enabled",
                table: "permission",
                newName: "state");

            migrationBuilder.RenameIndex(
                name: "ix_permission_sort_order",
                table: "permission",
                newName: "ix_permission_order");
        }
    }
}
