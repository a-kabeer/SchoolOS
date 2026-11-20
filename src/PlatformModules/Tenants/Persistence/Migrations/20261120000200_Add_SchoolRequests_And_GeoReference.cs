using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolOS.PlatformModules.Tenants.Persistence.Migrations;

[DbContext(typeof(PlatformDbContext))]
[Migration("20261120000200_Add_SchoolRequests_And_GeoReference")]
public partial class Add_SchoolRequests_And_GeoReference : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "platform");

        migrationBuilder.CreateTable(
            name: "school_requests",
            schema: "platform",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                school_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                contact_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                contact_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                submitted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                reviewed_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                decision_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                approved_tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_school_requests", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "countries",
            schema: "platform",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                languages_csv = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                default_language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                time_zone_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_countries", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "states",
            schema: "platform",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                country_id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_states", x => x.id);
                table.ForeignKey(
                    name: "fk_states_countries_country_id",
                    column: x => x.country_id,
                    principalSchema: "platform",
                    principalTable: "countries",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "cities",
            schema: "platform",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                state_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_cities", x => x.id);
                table.ForeignKey(
                    name: "fk_cities_states_state_id",
                    column: x => x.state_id,
                    principalSchema: "platform",
                    principalTable: "states",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_school_requests_status",
            schema: "platform",
            table: "school_requests",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "ix_countries_code",
            schema: "platform",
            table: "countries",
            column: "code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_states_country_id_code",
            schema: "platform",
            table: "states",
            columns: new[] { "country_id", "code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_cities_state_id_name",
            schema: "platform",
            table: "cities",
            columns: new[] { "state_id", "name" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "cities", schema: "platform");
        migrationBuilder.DropTable(name: "states", schema: "platform");
        migrationBuilder.DropTable(name: "countries", schema: "platform");
        migrationBuilder.DropTable(name: "school_requests", schema: "platform");
    }
}
