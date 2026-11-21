using System;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolOS.BuildingBlocks.Persistence;

#nullable disable

namespace SchoolOS.Modules.Attendance.Persistence.Migrations;

/// <inheritdoc />
public partial class Initial_Attendance_Create : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "attendance");

        migrationBuilder.CreateTable(
            name: "change_audit_entries",
            schema: "attendance",
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
            name: "daily_attendance_registers",
            schema: "attendance",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                school_id = table.Column<Guid>(type: "uuid", nullable: false),
                campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                academic_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                grade_level_id = table.Column<Guid>(type: "uuid", nullable: false),
                section_id = table.Column<Guid>(type: "uuid", nullable: false),
                attendance_date = table.Column<DateOnly>(type: "date", nullable: false),
                marked_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                marked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                is_locked = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_daily_attendance_registers", x => x.id);
                table.UniqueConstraint("ak_daily_attendance_registers_tenant_id_id", x => new { x.tenant_id, x.id });
            });

        migrationBuilder.CreateTable(
            name: "security_audit_events",
            schema: "attendance",
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
            name: "staff_members",
            schema: "attendance",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                school_id = table.Column<Guid>(type: "uuid", nullable: false),
                campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: true),
                employee_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                role_type = table.Column<int>(type: "integer", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_staff_members", x => x.id);
                table.UniqueConstraint("ak_staff_members_tenant_id_id", x => new { x.tenant_id, x.id });
            });

        migrationBuilder.CreateTable(
            name: "attendance_entries",
            schema: "attendance",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                register_id = table.Column<Guid>(type: "uuid", nullable: false),
                student_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                last_correction_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                last_updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                last_updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_attendance_entries", x => x.id);
                table.UniqueConstraint("ak_attendance_entries_tenant_id_id", x => new { x.tenant_id, x.id });
                table.ForeignKey(
                    name: "fk_attendance_entries_daily_attendance_registers_tenant_id_reg",
                    columns: x => new { x.tenant_id, x.register_id },
                    principalSchema: "attendance",
                    principalTable: "daily_attendance_registers",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "teacher_section_assignments",
            schema: "attendance",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                school_id = table.Column<Guid>(type: "uuid", nullable: false),
                campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                staff_member_id = table.Column<Guid>(type: "uuid", nullable: false),
                academic_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                grade_level_id = table.Column<Guid>(type: "uuid", nullable: false),
                section_id = table.Column<Guid>(type: "uuid", nullable: false),
                is_homeroom_teacher = table.Column<bool>(type: "boolean", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_teacher_section_assignments", x => x.id);
                table.ForeignKey(
                    name: "fk_teacher_section_assignments_staff_members_tenant_id_staff_m",
                    columns: x => new { x.tenant_id, x.staff_member_id },
                    principalSchema: "attendance",
                    principalTable: "staff_members",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "attendance_correction_audits",
            schema: "attendance",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                register_id = table.Column<Guid>(type: "uuid", nullable: false),
                student_id = table.Column<Guid>(type: "uuid", nullable: false),
                attendance_date = table.Column<DateOnly>(type: "date", nullable: false),
                previous_status = table.Column<int>(type: "integer", nullable: false),
                new_status = table.Column<int>(type: "integer", nullable: false),
                reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                corrected_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                corrected_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_attendance_correction_audits", x => x.id);
                table.ForeignKey(
                    name: "fk_attendance_correction_audits_attendance_entries_tenant_id_e",
                    columns: x => new { x.tenant_id, x.entry_id },
                    principalSchema: "attendance",
                    principalTable: "attendance_entries",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_attendance_correction_audits_campus_id",
            schema: "attendance",
            table: "attendance_correction_audits",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_attendance_correction_audits_tenant_id",
            schema: "attendance",
            table: "attendance_correction_audits",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_attendance_correction_audits_tenant_id_entry_id",
            schema: "attendance",
            table: "attendance_correction_audits",
            columns: new[] { "tenant_id", "entry_id" });

        migrationBuilder.CreateIndex(
            name: "ix_attendance_correction_audits_tenant_id_student_id_attendanc",
            schema: "attendance",
            table: "attendance_correction_audits",
            columns: new[] { "tenant_id", "student_id", "attendance_date" });

        migrationBuilder.CreateIndex(
            name: "ix_attendance_entries_campus_id",
            schema: "attendance",
            table: "attendance_entries",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_attendance_entries_tenant_id",
            schema: "attendance",
            table: "attendance_entries",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_attendance_entries_tenant_id_register_id_student_id",
            schema: "attendance",
            table: "attendance_entries",
            columns: new[] { "tenant_id", "register_id", "student_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_attendance_entries_tenant_id_student_id",
            schema: "attendance",
            table: "attendance_entries",
            columns: new[] { "tenant_id", "student_id" });

        migrationBuilder.CreateIndex(
            name: "ix_daily_attendance_registers_campus_id",
            schema: "attendance",
            table: "daily_attendance_registers",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_daily_attendance_registers_tenant_id",
            schema: "attendance",
            table: "daily_attendance_registers",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_daily_attendance_registers_unique_section_date",
            schema: "attendance",
            table: "daily_attendance_registers",
            columns: new[] { "tenant_id", "campus_id", "academic_year_id", "section_id", "attendance_date" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_staff_members_campus_id",
            schema: "attendance",
            table: "staff_members",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_staff_members_tenant_id",
            schema: "attendance",
            table: "staff_members",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_staff_members_tenant_id_school_id_employee_code",
            schema: "attendance",
            table: "staff_members",
            columns: new[] { "tenant_id", "school_id", "employee_code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_staff_members_unique_user",
            schema: "attendance",
            table: "staff_members",
            columns: new[] { "tenant_id", "user_id" },
            unique: true,
            filter: "user_id IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_teacher_section_assignments_campus_id",
            schema: "attendance",
            table: "teacher_section_assignments",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_teacher_section_assignments_tenant_id",
            schema: "attendance",
            table: "teacher_section_assignments",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_teacher_section_assignments_tenant_id_section_id_academic_y",
            schema: "attendance",
            table: "teacher_section_assignments",
            columns: new[] { "tenant_id", "section_id", "academic_year_id" });

        migrationBuilder.CreateIndex(
            name: "ix_teacher_section_assignments_tenant_id_staff_member_id_acade",
            schema: "attendance",
            table: "teacher_section_assignments",
            columns: new[] { "tenant_id", "staff_member_id", "academic_year_id", "section_id" },
            unique: true);

        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("attendance", "staff_members"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("attendance", "teacher_section_assignments"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("attendance", "daily_attendance_registers"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("attendance", "attendance_entries"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("attendance", "attendance_correction_audits"));
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("attendance", "attendance_correction_audits"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("attendance", "attendance_entries"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("attendance", "daily_attendance_registers"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("attendance", "teacher_section_assignments"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("attendance", "staff_members"));

        migrationBuilder.DropTable(
            name: "attendance_correction_audits",
            schema: "attendance");

        migrationBuilder.DropTable(
            name: "change_audit_entries",
            schema: "attendance");

        migrationBuilder.DropTable(
            name: "security_audit_events",
            schema: "attendance");

        migrationBuilder.DropTable(
            name: "teacher_section_assignments",
            schema: "attendance");

        migrationBuilder.DropTable(
            name: "attendance_entries",
            schema: "attendance");

        migrationBuilder.DropTable(
            name: "staff_members",
            schema: "attendance");

        migrationBuilder.DropTable(
            name: "daily_attendance_registers",
            schema: "attendance");
    }
}
