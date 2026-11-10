using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolOS.Modules.Organization.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial_Organization_Create : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "organization");

            migrationBuilder.CreateTable(
                name: "schools",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_schools", x => x.id);
                    table.UniqueConstraint("ak_schools_tenant_id_id", x => new { x.tenant_id, x.id });
                });

            migrationBuilder.CreateTable(
                name: "campuses",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campuses", x => x.id);
                    table.ForeignKey(
                        name: "fk_campuses_schools_tenant_id_school_id",
                        columns: x => new { x.tenant_id, x.school_id },
                        principalSchema: "organization",
                        principalTable: "schools",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_campuses_school_id_code",
                schema: "organization",
                table: "campuses",
                columns: new[] { "school_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_campuses_tenant_id",
                schema: "organization",
                table: "campuses",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_campuses_tenant_id_school_id",
                schema: "organization",
                table: "campuses",
                columns: new[] { "tenant_id", "school_id" });

            migrationBuilder.CreateIndex(
                name: "ix_schools_tenant_id",
                schema: "organization",
                table: "schools",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_schools_tenant_id_code",
                schema: "organization",
                table: "schools",
                columns: new[] { "tenant_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "campuses",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "schools",
                schema: "organization");
        }
    }
}
