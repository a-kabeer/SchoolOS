using System;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolOS.BuildingBlocks.Persistence;

#nullable disable

namespace SchoolOS.Modules.Students.Persistence.Migrations;

/// <inheritdoc />
public partial class Initial_Students_Create : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "students");

        migrationBuilder.CreateTable(
            name: "change_audit_entries",
            schema: "students",
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
            name: "guardians",
            schema: "students",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                primary_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                secondary_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                occupation = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                preferred_channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_guardians", x => x.id);
                table.UniqueConstraint("ak_guardians_tenant_id_id", x => new { x.tenant_id, x.id });
            });

        migrationBuilder.CreateTable(
            name: "households",
            schema: "students",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                household_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                display_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                primary_guardian_id = table.Column<Guid>(type: "uuid", nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_households", x => x.id);
                table.UniqueConstraint("ak_households_tenant_id_id", x => new { x.tenant_id, x.id });
            });

        migrationBuilder.CreateTable(
            name: "security_audit_events",
            schema: "students",
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
            name: "students",
            schema: "students",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                school_id = table.Column<Guid>(type: "uuid", nullable: false),
                campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                admission_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                middle_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                date_of_birth = table.Column<DateOnly>(type: "date", nullable: false),
                gender = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                admission_date = table.Column<DateOnly>(type: "date", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                source_enquiry_id = table.Column<Guid>(type: "uuid", nullable: true),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_students", x => x.id);
                table.UniqueConstraint("ak_students_tenant_id_id", x => new { x.tenant_id, x.id });
            });

        migrationBuilder.CreateTable(
            name: "household_members",
            schema: "students",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                household_id = table.Column<Guid>(type: "uuid", nullable: false),
                student_id = table.Column<Guid>(type: "uuid", nullable: true),
                guardian_id = table.Column<Guid>(type: "uuid", nullable: true),
                role_label = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                is_billing_member = table.Column<bool>(type: "boolean", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_household_members", x => x.id);
                table.CheckConstraint("ck_household_members_single_ref", "(student_id IS NOT NULL AND guardian_id IS NULL) OR (student_id IS NULL AND guardian_id IS NOT NULL)");
                table.ForeignKey(
                    name: "fk_household_members_households_tenant_id_household_id",
                    columns: x => new { x.tenant_id, x.household_id },
                    principalSchema: "students",
                    principalTable: "households",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "enrollments",
            schema: "students",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                school_id = table.Column<Guid>(type: "uuid", nullable: false),
                campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                student_id = table.Column<Guid>(type: "uuid", nullable: false),
                academic_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                grade_level_id = table.Column<Guid>(type: "uuid", nullable: false),
                section_id = table.Column<Guid>(type: "uuid", nullable: true),
                roll_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                start_date = table.Column<DateOnly>(type: "date", nullable: false),
                end_date = table.Column<DateOnly>(type: "date", nullable: true),
                status = table.Column<int>(type: "integer", nullable: false),
                previous_enrollment_id = table.Column<Guid>(type: "uuid", nullable: true),
                reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_enrollments", x => x.id);
                table.ForeignKey(
                    name: "fk_enrollments_students_tenant_id_student_id",
                    columns: x => new { x.tenant_id, x.student_id },
                    principalSchema: "students",
                    principalTable: "students",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "student_guardians",
            schema: "students",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                student_id = table.Column<Guid>(type: "uuid", nullable: false),
                guardian_id = table.Column<Guid>(type: "uuid", nullable: false),
                relationship_type = table.Column<int>(type: "integer", nullable: false),
                is_primary_contact = table.Column<bool>(type: "boolean", nullable: false),
                is_emergency_contact = table.Column<bool>(type: "boolean", nullable: false),
                has_legal_custody = table.Column<bool>(type: "boolean", nullable: false),
                can_collect_student = table.Column<bool>(type: "boolean", nullable: false),
                receives_academic_notices = table.Column<bool>(type: "boolean", nullable: false),
                receives_financial_notices = table.Column<bool>(type: "boolean", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_student_guardians", x => x.id);
                table.ForeignKey(
                    name: "fk_student_guardians_guardians_tenant_id_guardian_id",
                    columns: x => new { x.tenant_id, x.guardian_id },
                    principalSchema: "students",
                    principalTable: "guardians",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_student_guardians_students_tenant_id_student_id",
                    columns: x => new { x.tenant_id, x.student_id },
                    principalSchema: "students",
                    principalTable: "students",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "student_status_history",
            schema: "students",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                student_id = table.Column<Guid>(type: "uuid", nullable: false),
                from_status = table.Column<int>(type: "integer", nullable: false),
                to_status = table.Column<int>(type: "integer", nullable: false),
                effective_date = table.Column<DateOnly>(type: "date", nullable: false),
                reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                changed_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                changed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_student_status_history", x => x.id);
                table.ForeignKey(
                    name: "fk_student_status_history_students_tenant_id_student_id",
                    columns: x => new { x.tenant_id, x.student_id },
                    principalSchema: "students",
                    principalTable: "students",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_enrollments_campus_id",
            schema: "students",
            table: "enrollments",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_enrollments_single_active_per_student_year",
            schema: "students",
            table: "enrollments",
            columns: new[] { "tenant_id", "student_id", "academic_year_id" },
            unique: true,
            filter: "status = 1");

        migrationBuilder.CreateIndex(
            name: "ix_enrollments_tenant_id",
            schema: "students",
            table: "enrollments",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_enrollments_tenant_id_campus_id_academic_year_id_grade_leve",
            schema: "students",
            table: "enrollments",
            columns: new[] { "tenant_id", "campus_id", "academic_year_id", "grade_level_id", "section_id" });

        migrationBuilder.CreateIndex(
            name: "ix_guardians_tenant_id",
            schema: "students",
            table: "guardians",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_guardians_tenant_id_primary_phone",
            schema: "students",
            table: "guardians",
            columns: new[] { "tenant_id", "primary_phone" });

        migrationBuilder.CreateIndex(
            name: "ix_household_members_tenant_id",
            schema: "students",
            table: "household_members",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_household_members_unique_guardian",
            schema: "students",
            table: "household_members",
            columns: new[] { "tenant_id", "household_id", "guardian_id" },
            unique: true,
            filter: "guardian_id IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_household_members_unique_student",
            schema: "students",
            table: "household_members",
            columns: new[] { "tenant_id", "household_id", "student_id" },
            unique: true,
            filter: "student_id IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_households_tenant_id",
            schema: "students",
            table: "households",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_households_tenant_id_household_code",
            schema: "students",
            table: "households",
            columns: new[] { "tenant_id", "household_code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_student_guardians_tenant_id",
            schema: "students",
            table: "student_guardians",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_student_guardians_tenant_id_guardian_id",
            schema: "students",
            table: "student_guardians",
            columns: new[] { "tenant_id", "guardian_id" });

        migrationBuilder.CreateIndex(
            name: "ix_student_guardians_tenant_id_student_id_guardian_id",
            schema: "students",
            table: "student_guardians",
            columns: new[] { "tenant_id", "student_id", "guardian_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_student_status_history_tenant_id",
            schema: "students",
            table: "student_status_history",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_student_status_history_tenant_id_student_id_changed_at_utc",
            schema: "students",
            table: "student_status_history",
            columns: new[] { "tenant_id", "student_id", "changed_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_students_campus_id",
            schema: "students",
            table: "students",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_students_tenant_id",
            schema: "students",
            table: "students",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_students_tenant_id_campus_id_status",
            schema: "students",
            table: "students",
            columns: new[] { "tenant_id", "campus_id", "status" });

        migrationBuilder.CreateIndex(
            name: "ix_students_tenant_id_school_id_admission_number",
            schema: "students",
            table: "students",
            columns: new[] { "tenant_id", "school_id", "admission_number" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_students_unique_source_enquiry",
            schema: "students",
            table: "students",
            columns: new[] { "tenant_id", "source_enquiry_id" },
            unique: true,
            filter: "source_enquiry_id IS NOT NULL");

        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("students", "students"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("students", "student_status_history"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("students", "guardians"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("students", "student_guardians"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("students", "households"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("students", "household_members"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("students", "enrollments"));
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("students", "enrollments"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("students", "household_members"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("students", "households"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("students", "student_guardians"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("students", "guardians"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("students", "student_status_history"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("students", "students"));

        migrationBuilder.DropTable(
            name: "change_audit_entries",
            schema: "students");

        migrationBuilder.DropTable(
            name: "enrollments",
            schema: "students");

        migrationBuilder.DropTable(
            name: "household_members",
            schema: "students");

        migrationBuilder.DropTable(
            name: "security_audit_events",
            schema: "students");

        migrationBuilder.DropTable(
            name: "student_guardians",
            schema: "students");

        migrationBuilder.DropTable(
            name: "student_status_history",
            schema: "students");

        migrationBuilder.DropTable(
            name: "households",
            schema: "students");

        migrationBuilder.DropTable(
            name: "guardians",
            schema: "students");

        migrationBuilder.DropTable(
            name: "students",
            schema: "students");
    }
}
