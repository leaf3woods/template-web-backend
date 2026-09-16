using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Org.Product.Infrastructure.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "permission",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    route = table.Column<string>(type: "text", nullable: true),
                    permission_code = table.Column<string>(type: "text", nullable: true),
                    visible = table.Column<bool>(type: "boolean", nullable: false),
                    favorite = table.Column<bool>(type: "boolean", nullable: false),
                    visiable = table.Column<bool>(type: "boolean", nullable: false),
                    is_link = table.Column<bool>(type: "boolean", nullable: false),
                    icon_url = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    soft_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    delete_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    creator_level = table.Column<int>(type: "integer", nullable: true),
                    creation_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modifier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modification_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permission", x => x.id);
                    table.ForeignKey(
                        name: "fk_permission_permission_parent_id",
                        column: x => x.parent_id,
                        principalTable: "permission",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    creator_level = table.Column<int>(type: "integer", nullable: true),
                    creation_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modifier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modification_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    soft_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    delete_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "text", nullable: false),
                    code = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: true),
                    nick = table.Column<string>(type: "text", nullable: true),
                    passphrase = table.Column<string>(type: "text", nullable: false),
                    salt = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    gender = table.Column<int>(type: "integer", nullable: false),
                    birth_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    register_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    creator_level = table.Column<int>(type: "integer", nullable: true),
                    creation_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modifier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modification_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    soft_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    delete_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_permission",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permission", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_permission_permission_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permission",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_permission_role_role_id",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_detail",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gender = table.Column<int>(type: "integer", nullable: false),
                    about_me = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_detail", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_user_detail_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_role",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_role", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_role_role_role_id",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_role_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_setting",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_setting", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_user_setting_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "permission",
                columns: new[] { "id", "code", "creation_time", "creator_id", "creator_level", "delete_time", "description", "favorite", "icon_url", "is_link", "last_modification_time", "last_modifier_id", "name", "order", "parent_id", "permission_code", "route", "soft_deleted", "state", "type", "visiable", "visible" },
                values: new object[] { new Guid("e9df3280-8ab1-4b45-8d6a-6c3e669317ac"), "root", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, "root menu", false, null, false, null, null, "Root", 0, null, null, null, false, false, 0, true, true });

            migrationBuilder.InsertData(
                table: "role",
                columns: new[] { "id", "code", "creation_time", "creator_id", "creator_level", "delete_time", "description", "last_modification_time", "last_modifier_id", "level", "name", "order", "soft_deleted", "state" },
                values: new object[,]
                {
                    { new Guid("4a15f57a-0cb7-4cc9-95c0-91ba672a341c"), "member", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, "normal user with some basic resources", null, null, 0, "member", 0, false, false },
                    { new Guid("4fe6ebb8-5001-40b4-a59e-d193ad9186f8"), "super", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, "super user with all catchable resources", null, null, 0, "super", 0, false, false },
                    { new Guid("e1f23f37-919c-453b-aff1-1214415e54b8"), "admin", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, "admin to manage user resourcs", null, null, 0, "admin", 0, false, false },
                    { new Guid("e8df3280-8ab1-4b45-8d6a-6c3e669317ac"), "developer", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, "developer with all cathable resources even it was obselete", null, null, 0, "developer", 0, false, false },
                    { new Guid("ffce17eb-a74c-4b44-aaac-2e2e78e04f9e"), "visitor", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, "a visitor with some read resources", null, null, 0, "visitor", 0, false, false }
                });

            migrationBuilder.InsertData(
                table: "user",
                columns: new[] { "id", "birth_date", "code", "creation_time", "creator_id", "creator_level", "delete_time", "email", "gender", "last_modification_time", "last_modifier_id", "name", "nick", "note", "order", "passphrase", "phone_number", "register_time", "salt", "soft_deleted", "state", "username" },
                values: new object[,]
                {
                    { new Guid("a8ba3a6d-c7b1-4d90-b0c9-66f1cbd66101"), null, null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, "unknow", 2, null, null, null, "initial-developer", null, 0, "Uh+8E9ft9jptdMzAVRKo0UYQtqn5epsbJUZQGbL/Xhk=", "unknow", new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "5+fPPv0FShtKo3ed746TiuNojEZsxuPkhbU+YvF5DuQ=", false, false, "developer" },
                    { new Guid("a8ba3a6d-c7b1-4d90-b0c9-66f1cbd66102"), null, null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, "unknow", 2, null, null, null, "initial-super", null, 0, "WSAcdSAvzQFUq3iXLWXLmcuPmWHIjE8ffSBTVjJVBPQ=", "unknow", new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "aY68cuKZh+LNfYczaGclgtTOYy34yvl1O/H9IX3bBtU=", false, false, "super" },
                    { new Guid("a8ba3a6d-c7b1-4d90-b0c9-66f1cbd66103"), null, null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, "unknow", 2, null, null, null, "initial-admin", null, 0, "Lc8DL5jIpDxDfsDp6gYk2HjVIEzXZ30MJc5eW6OU6ko=", "unknow", new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "JO3wh7gOTUQ5cBydCoQqnazvw5dgRoVQkNpdrIAvVgI=", false, false, "admin" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_permission_code",
                table: "permission",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_permission_order",
                table: "permission",
                column: "order");

            migrationBuilder.CreateIndex(
                name: "ix_permission_parent_id",
                table: "permission",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_name_soft_deleted",
                table: "role",
                columns: new[] { "name", "soft_deleted" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_soft_deleted",
                table: "role",
                column: "soft_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_role_permission_permission_id",
                table: "role_permission",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_permission_role_id",
                table: "role_permission",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_soft_deleted",
                table: "user",
                column: "soft_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_user_username_soft_deleted",
                table: "user",
                columns: new[] { "username", "soft_deleted" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_role_role_id",
                table: "user_role",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_role_user_id",
                table: "user_role",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "role_permission");

            migrationBuilder.DropTable(
                name: "user_detail");

            migrationBuilder.DropTable(
                name: "user_role");

            migrationBuilder.DropTable(
                name: "user_setting");

            migrationBuilder.DropTable(
                name: "permission");

            migrationBuilder.DropTable(
                name: "role");

            migrationBuilder.DropTable(
                name: "user");
        }
    }
}
