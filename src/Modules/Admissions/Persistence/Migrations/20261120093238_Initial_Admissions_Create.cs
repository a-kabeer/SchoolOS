using System;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolOS.BuildingBlocks.Persistence;

#nullable disable

namespace SchoolOS.Modules.Admissions.Persistence.Migrations;

/// <inheritdoc />
public partial class Initial_Admissions_Create : Migration
{
    private static readonly string[] TenantScopedTables =
    [
        "admission_enquiries",
        "enquiry_follow_ups",
        "campus_visits",
        "student_profiles"
    ];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "admissions");

        migrationBuilder.CreateTable(
            name: "admission_enquiries",
            schema: "admissions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                applicant_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                guardian_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                guardian_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                guardian_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                target_grade = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                academic_year = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                converted_student_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_admission_enquiries", x => x.id);
                table.UniqueConstraint("ak_admission_enquiries_tenant_id_id", x => new { x.tenant_id, x.id });
            });

        migrationBuilder.CreateTable(
            name: "change_audit_entries",
            schema: "admissions",
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
            name: "security_audit_events",
            schema: "admissions",
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
            name: "student_profiles",
            schema: "admissions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                admission_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                date_of_birth = table.Column<DateOnly>(type: "date", nullable: false),
                guardian_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                guardian_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                guardian_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                target_grade = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                source_enquiry_id = table.Column<Guid>(type: "uuid", nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_student_profiles", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "campus_visits",
            schema: "admissions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                enquiry_id = table.Column<Guid>(type: "uuid", nullable: false),
                scheduled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                visitor_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                completed = table.Column<bool>(type: "boolean", nullable: false),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_campus_visits", x => x.id);
                table.ForeignKey(
                    name: "fk_campus_visits_admission_enquiries_tenant_id_enquiry_id",
                    columns: x => new { x.tenant_id, x.enquiry_id },
                    principalSchema: "admissions",
                    principalTable: "admission_enquiries",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "enquiry_follow_ups",
            schema: "admissions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                enquiry_id = table.Column<Guid>(type: "uuid", nullable: false),
                follow_up_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                channel = table.Column<int>(type: "integer", nullable: false),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_enquiry_follow_ups", x => x.id);
                table.ForeignKey(
                    name: "fk_enquiry_follow_ups_admission_enquiries_tenant_id_enquiry_id",
                    columns: x => new { x.tenant_id, x.enquiry_id },
                    principalSchema: "admissions",
                    principalTable: "admission_enquiries",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_admission_enquiries_campus_id",
            schema: "admissions",
            table: "admission_enquiries",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_admission_enquiries_tenant_id",
            schema: "admissions",
            table: "admission_enquiries",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_admission_enquiries_tenant_id_campus_id_status",
            schema: "admissions",
            table: "admission_enquiries",
            columns: new[] { "tenant_id", "campus_id", "status" });

        migrationBuilder.CreateIndex(
            name: "ix_campus_visits_campus_id",
            schema: "admissions",
            table: "campus_visits",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_campus_visits_tenant_id",
            schema: "admissions",
            table: "campus_visits",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_campus_visits_tenant_id_enquiry_id",
            schema: "admissions",
            table: "campus_visits",
            columns: new[] { "tenant_id", "enquiry_id" });

        migrationBuilder.CreateIndex(
            name: "ix_enquiry_follow_ups_campus_id",
            schema: "admissions",
            table: "enquiry_follow_ups",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_enquiry_follow_ups_tenant_id",
            schema: "admissions",
            table: "enquiry_follow_ups",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_enquiry_follow_ups_tenant_id_enquiry_id",
            schema: "admissions",
            table: "enquiry_follow_ups",
            columns: new[] { "tenant_id", "enquiry_id" });

        migrationBuilder.CreateIndex(
            name: "ix_student_profiles_campus_id",
            schema: "admissions",
            table: "student_profiles",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_student_profiles_tenant_id",
            schema: "admissions",
            table: "student_profiles",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_student_profiles_tenant_id_admission_number",
            schema: "admissions",
            table: "student_profiles",
            columns: new[] { "tenant_id", "admission_number" },
            unique: true);

        foreach (var table in TenantScopedTables)
        {
            migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("admissions", table));
        }
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in TenantScopedTables)
        {
            migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("admissions", table));
        }

        migrationBuilder.DropTable(
            name: "campus_visits",
            schema: "admissions");

        migrationBuilder.DropTable(
            name: "change_audit_entries",
            schema: "admissions");

        migrationBuilder.DropTable(
            name: "enquiry_follow_ups",
            schema: "admissions");

        migrationBuilder.DropTable(
            name: "security_audit_events",
            schema: "admissions");

        migrationBuilder.DropTable(
            name: "student_profiles",
            schema: "admissions");

        migrationBuilder.DropTable(
            name: "admission_enquiries",
            schema: "admissions");
    }
}
