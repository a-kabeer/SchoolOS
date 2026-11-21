using System;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolOS.BuildingBlocks.Persistence;

#nullable disable

namespace SchoolOS.Modules.Fees.Persistence.Migrations;

/// <inheritdoc />
public partial class Initial_Fees_Create : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "fees");

        migrationBuilder.CreateTable(
            name: "change_audit_entries",
            schema: "fees",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                actor_user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                entity_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                action = table.Column<int>(type: "integer", nullable: false),
                changes_json = table.Column<string>(type: "text", nullable: false),
                trace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_change_audit_entries", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "fee_challans",
            schema: "fees",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                school_id = table.Column<Guid>(type: "uuid", nullable: false),
                campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                student_id = table.Column<Guid>(type: "uuid", nullable: false),
                household_id = table.Column<Guid>(type: "uuid", nullable: true),
                academic_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                grade_level_id = table.Column<Guid>(type: "uuid", nullable: false),
                billing_period = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                challan_number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                issue_date = table.Column<DateOnly>(type: "date", nullable: false),
                due_date = table.Column<DateOnly>(type: "date", nullable: false),
                gross_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                late_fee_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                net_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                paid_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                balance_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                void_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_fee_challans", x => x.id);
                table.UniqueConstraint("ak_fee_challans_tenant_id_id", x => new { x.tenant_id, x.id });
            });

        migrationBuilder.CreateTable(
            name: "fee_heads",
            schema: "fees",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                school_id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                frequency = table.Column<int>(type: "integer", nullable: false),
                is_optional = table.Column<bool>(type: "boolean", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_fee_heads", x => x.id);
                table.UniqueConstraint("ak_fee_heads_tenant_id_id", x => new { x.tenant_id, x.id });
            });

        migrationBuilder.CreateTable(
            name: "fee_posting_outbox",
            schema: "fees",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                event_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                reference_id = table.Column<Guid>(type: "uuid", nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                payload_json = table.Column<string>(type: "text", nullable: false),
                occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_fee_posting_outbox", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "security_audit_events",
            schema: "fees",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                event_type = table.Column<int>(type: "integer", nullable: false),
                outcome = table.Column<int>(type: "integer", nullable: false),
                actor_user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                actor_tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                actor_campus_id = table.Column<Guid>(type: "uuid", nullable: true),
                target_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                target_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                source_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                trace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_security_audit_events", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "fee_challan_lines",
            schema: "fees",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                challan_id = table.Column<Guid>(type: "uuid", nullable: false),
                fee_head_id = table.Column<Guid>(type: "uuid", nullable: false),
                description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                gross_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                net_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_fee_challan_lines", x => x.id);
                table.ForeignKey(
                    name: "fk_fee_challan_lines_fee_challans_tenant_id_challan_id",
                    columns: x => new { x.tenant_id, x.challan_id },
                    principalSchema: "fees",
                    principalTable: "fee_challans",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "fee_receipts",
            schema: "fees",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                school_id = table.Column<Guid>(type: "uuid", nullable: false),
                campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                challan_id = table.Column<Guid>(type: "uuid", nullable: false),
                student_id = table.Column<Guid>(type: "uuid", nullable: false),
                receipt_number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                method = table.Column<int>(type: "integer", nullable: false),
                external_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                collected_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                collected_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                reversal_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                reversed_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                reversed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_fee_receipts", x => x.id);
                table.ForeignKey(
                    name: "fk_fee_receipts_fee_challans_tenant_id_challan_id",
                    columns: x => new { x.tenant_id, x.challan_id },
                    principalSchema: "fees",
                    principalTable: "fee_challans",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "fee_schedules",
            schema: "fees",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                school_id = table.Column<Guid>(type: "uuid", nullable: false),
                campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                academic_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                grade_level_id = table.Column<Guid>(type: "uuid", nullable: false),
                fee_head_id = table.Column<Guid>(type: "uuid", nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                sibling_discount_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                late_fee_per_day = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_fee_schedules", x => x.id);
                table.ForeignKey(
                    name: "fk_fee_schedules_fee_heads_tenant_id_fee_head_id",
                    columns: x => new { x.tenant_id, x.fee_head_id },
                    principalSchema: "fees",
                    principalTable: "fee_heads",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_fee_challan_lines_tenant_id",
            schema: "fees",
            table: "fee_challan_lines",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_fee_challan_lines_tenant_id_challan_id",
            schema: "fees",
            table: "fee_challan_lines",
            columns: new[] { "tenant_id", "challan_id" });

        migrationBuilder.CreateIndex(
            name: "ix_fee_challans_campus_id",
            schema: "fees",
            table: "fee_challans",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_fee_challans_tenant_id",
            schema: "fees",
            table: "fee_challans",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_fee_challans_tenant_id_campus_id_status_due_date",
            schema: "fees",
            table: "fee_challans",
            columns: new[] { "tenant_id", "campus_id", "status", "due_date" });

        migrationBuilder.CreateIndex(
            name: "ix_fee_challans_tenant_id_school_id_challan_number",
            schema: "fees",
            table: "fee_challans",
            columns: new[] { "tenant_id", "school_id", "challan_number" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_fee_challans_unique_active_student_period",
            schema: "fees",
            table: "fee_challans",
            columns: new[] { "tenant_id", "student_id", "academic_year_id", "billing_period" },
            unique: true,
            filter: "status <> 5");

        migrationBuilder.CreateIndex(
            name: "ix_fee_heads_tenant_id",
            schema: "fees",
            table: "fee_heads",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_fee_heads_tenant_id_school_id_code",
            schema: "fees",
            table: "fee_heads",
            columns: new[] { "tenant_id", "school_id", "code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_fee_posting_outbox_campus_id",
            schema: "fees",
            table: "fee_posting_outbox",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_fee_posting_outbox_tenant_id",
            schema: "fees",
            table: "fee_posting_outbox",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_fee_posting_outbox_tenant_id_occurred_at_utc",
            schema: "fees",
            table: "fee_posting_outbox",
            columns: new[] { "tenant_id", "occurred_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_fee_receipts_campus_id",
            schema: "fees",
            table: "fee_receipts",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_fee_receipts_tenant_id",
            schema: "fees",
            table: "fee_receipts",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_fee_receipts_tenant_id_challan_id",
            schema: "fees",
            table: "fee_receipts",
            columns: new[] { "tenant_id", "challan_id" });

        migrationBuilder.CreateIndex(
            name: "ix_fee_receipts_tenant_id_school_id_receipt_number",
            schema: "fees",
            table: "fee_receipts",
            columns: new[] { "tenant_id", "school_id", "receipt_number" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_fee_schedules_campus_id",
            schema: "fees",
            table: "fee_schedules",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_fee_schedules_tenant_id",
            schema: "fees",
            table: "fee_schedules",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_fee_schedules_tenant_id_fee_head_id",
            schema: "fees",
            table: "fee_schedules",
            columns: new[] { "tenant_id", "fee_head_id" });

        migrationBuilder.CreateIndex(
            name: "ix_fee_schedules_unique_head_per_grade",
            schema: "fees",
            table: "fee_schedules",
            columns: new[] { "tenant_id", "campus_id", "academic_year_id", "grade_level_id", "fee_head_id" },
            unique: true);

        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("fees", "fee_heads"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("fees", "fee_schedules"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("fees", "fee_challans"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("fees", "fee_challan_lines"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("fees", "fee_receipts"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("fees", "fee_posting_outbox"));
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("fees", "fee_posting_outbox"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("fees", "fee_receipts"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("fees", "fee_challan_lines"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("fees", "fee_challans"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("fees", "fee_schedules"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("fees", "fee_heads"));

        migrationBuilder.DropTable(
            name: "change_audit_entries",
            schema: "fees");

        migrationBuilder.DropTable(
            name: "fee_challan_lines",
            schema: "fees");

        migrationBuilder.DropTable(
            name: "fee_posting_outbox",
            schema: "fees");

        migrationBuilder.DropTable(
            name: "fee_receipts",
            schema: "fees");

        migrationBuilder.DropTable(
            name: "fee_schedules",
            schema: "fees");

        migrationBuilder.DropTable(
            name: "security_audit_events",
            schema: "fees");

        migrationBuilder.DropTable(
            name: "fee_challans",
            schema: "fees");

        migrationBuilder.DropTable(
            name: "fee_heads",
            schema: "fees");
    }
}
