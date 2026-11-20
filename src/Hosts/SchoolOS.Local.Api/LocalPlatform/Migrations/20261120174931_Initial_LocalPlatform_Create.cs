using System;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolOS.BuildingBlocks.Persistence;

#nullable disable

namespace SchoolOS.Local.Api.LocalPlatform.Migrations;

/// <inheritdoc />
public partial class Initial_LocalPlatform_Create : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "platform_local");

        migrationBuilder.CreateTable(
            name: "license_leases",
            schema: "platform_local",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                node_id = table.Column<Guid>(type: "uuid", nullable: true),
                sequence_number = table.Column<long>(type: "bigint", nullable: false),
                key_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                algorithm = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                payload_json = table.Column<string>(type: "text", nullable: false),
                signature_base64url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                issued_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                paid_through_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                grace_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_trusted_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_license_leases", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_license_leases_tenant_id",
            schema: "platform_local",
            table: "license_leases",
            column: "tenant_id",
            unique: true);

        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("platform_local", "license_leases"));
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("platform_local", "license_leases"));

        migrationBuilder.DropTable(
            name: "license_leases",
            schema: "platform_local");
    }
}
