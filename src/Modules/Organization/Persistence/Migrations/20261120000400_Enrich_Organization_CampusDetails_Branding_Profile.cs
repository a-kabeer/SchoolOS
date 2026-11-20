using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolOS.BuildingBlocks.Persistence;

#nullable disable

namespace SchoolOS.Modules.Organization.Persistence.Migrations;

[DbContext(typeof(OrganizationDbContext))]
[Migration("20261120000400_Enrich_Organization_CampusDetails_Branding_Profile")]
public partial class Enrich_Organization_CampusDetails_Branding_Profile : Migration
{
    private static readonly string[] NewTenantScopedTables =
    [
        "campus_details",
        "campus_phones",
        "branding_images",
        "school_brandings",
        "school_profiles"
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddUniqueConstraint(
            name: "ak_campuses_tenant_id_id",
            schema: "organization",
            table: "campuses",
            columns: new[] { "tenant_id", "id" });

        migrationBuilder.CreateTable(
            name: "campus_details",
            schema: "organization",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                address_line1 = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                postal_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                website_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                timezone_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_campus_details", x => x.id);
                table.ForeignKey(
                    name: "fk_campus_details_campuses_tenant_id_campus_id",
                    columns: x => new { x.tenant_id, x.campus_id },
                    principalSchema: "organization",
                    principalTable: "campuses",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "campus_phones",
            schema: "organization",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                label = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                phone_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                is_primary = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_campus_phones", x => x.id);
                table.ForeignKey(
                    name: "fk_campus_phones_campuses_tenant_id_campus_id",
                    columns: x => new { x.tenant_id, x.campus_id },
                    principalSchema: "organization",
                    principalTable: "campuses",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "branding_images",
            schema: "organization",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                school_id = table.Column<Guid>(type: "uuid", nullable: false),
                slot = table.Column<int>(type: "integer", nullable: false),
                content_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                data = table.Column<byte[]>(type: "bytea", nullable: false),
                sha256_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_branding_images", x => x.id);
                table.ForeignKey(
                    name: "fk_branding_images_schools_tenant_id_school_id",
                    columns: x => new { x.tenant_id, x.school_id },
                    principalSchema: "organization",
                    principalTable: "schools",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "school_brandings",
            schema: "organization",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                school_id = table.Column<Guid>(type: "uuid", nullable: false),
                primary_color_hex = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                secondary_color_hex = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                accent_color_hex = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_school_brandings", x => x.id);
                table.ForeignKey(
                    name: "fk_school_brandings_schools_tenant_id_school_id",
                    columns: x => new { x.tenant_id, x.school_id },
                    principalSchema: "organization",
                    principalTable: "schools",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "school_profiles",
            schema: "organization",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                school_id = table.Column<Guid>(type: "uuid", nullable: false),
                tagline = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                principal_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                established_year = table.Column<int>(type: "integer", nullable: true),
                contact_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                website_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                default_language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_school_profiles", x => x.id);
                table.ForeignKey(
                    name: "fk_school_profiles_schools_tenant_id_school_id",
                    columns: x => new { x.tenant_id, x.school_id },
                    principalSchema: "organization",
                    principalTable: "schools",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("ix_campus_details_tenant_id_campus_id", "campus_details", new[] { "tenant_id", "campus_id" }, "organization", unique: true);
        migrationBuilder.CreateIndex("ix_campus_phones_tenant_id_campus_id", "campus_phones", new[] { "tenant_id", "campus_id" }, "organization");
        migrationBuilder.CreateIndex("ix_branding_images_tenant_id_school_id_slot", "branding_images", new[] { "tenant_id", "school_id", "slot" }, "organization", unique: true);
        migrationBuilder.CreateIndex("ix_school_brandings_tenant_id_school_id", "school_brandings", new[] { "tenant_id", "school_id" }, "organization", unique: true);
        migrationBuilder.CreateIndex("ix_school_profiles_tenant_id_school_id", "school_profiles", new[] { "tenant_id", "school_id" }, "organization", unique: true);

        foreach (var table in NewTenantScopedTables)
        {
            migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("organization", table));
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in NewTenantScopedTables)
        {
            migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("organization", table));
        }

        migrationBuilder.DropTable("school_profiles", "organization");
        migrationBuilder.DropTable("school_brandings", "organization");
        migrationBuilder.DropTable("branding_images", "organization");
        migrationBuilder.DropTable("campus_phones", "organization");
        migrationBuilder.DropTable("campus_details", "organization");

        migrationBuilder.DropUniqueConstraint(
            name: "ak_campuses_tenant_id_id",
            schema: "organization",
            table: "campuses");
    }
}
