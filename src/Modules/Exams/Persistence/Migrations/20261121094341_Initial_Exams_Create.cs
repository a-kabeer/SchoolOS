using System;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolOS.BuildingBlocks.Persistence;

#nullable disable

namespace SchoolOS.Modules.Exams.Persistence.Migrations;

/// <inheritdoc />
public partial class Initial_Exams_Create : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "exams");

        migrationBuilder.CreateTable(
            name: "change_audit_entries",
            schema: "exams",
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
            name: "exam_terms",
            schema: "exams",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                school_id = table.Column<Guid>(type: "uuid", nullable: false),
                campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                academic_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                start_date = table.Column<DateOnly>(type: "date", nullable: false),
                end_date = table.Column<DateOnly>(type: "date", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_exam_terms", x => x.id);
                table.UniqueConstraint("ak_exam_terms_tenant_id_id", x => new { x.tenant_id, x.id });
            });

        migrationBuilder.CreateTable(
            name: "security_audit_events",
            schema: "exams",
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
            name: "subjects",
            schema: "exams",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                school_id = table.Column<Guid>(type: "uuid", nullable: false),
                grade_level_id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_subjects", x => x.id);
                table.UniqueConstraint("ak_subjects_tenant_id_id", x => new { x.tenant_id, x.id });
            });

        migrationBuilder.CreateTable(
            name: "exam_papers",
            schema: "exams",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                school_id = table.Column<Guid>(type: "uuid", nullable: false),
                campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                exam_term_id = table.Column<Guid>(type: "uuid", nullable: false),
                grade_level_id = table.Column<Guid>(type: "uuid", nullable: false),
                subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                max_marks = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                passing_marks = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                weightage_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                exam_date = table.Column<DateOnly>(type: "date", nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_exam_papers", x => x.id);
                table.UniqueConstraint("ak_exam_papers_tenant_id_id", x => new { x.tenant_id, x.id });
                table.ForeignKey(
                    name: "fk_exam_papers_exam_terms_tenant_id_exam_term_id",
                    columns: x => new { x.tenant_id, x.exam_term_id },
                    principalSchema: "exams",
                    principalTable: "exam_terms",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_exam_papers_subjects_tenant_id_subject_id",
                    columns: x => new { x.tenant_id, x.subject_id },
                    principalSchema: "exams",
                    principalTable: "subjects",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "mark_sheets",
            schema: "exams",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                school_id = table.Column<Guid>(type: "uuid", nullable: false),
                campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                exam_paper_id = table.Column<Guid>(type: "uuid", nullable: false),
                section_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                submitted_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                submitted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                approved_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                approved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                review_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_mark_sheets", x => x.id);
                table.UniqueConstraint("ak_mark_sheets_tenant_id_id", x => new { x.tenant_id, x.id });
                table.ForeignKey(
                    name: "fk_mark_sheets_exam_papers_tenant_id_exam_paper_id",
                    columns: x => new { x.tenant_id, x.exam_paper_id },
                    principalSchema: "exams",
                    principalTable: "exam_papers",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "student_marks",
            schema: "exams",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                mark_sheet_id = table.Column<Guid>(type: "uuid", nullable: false),
                student_id = table.Column<Guid>(type: "uuid", nullable: false),
                marks_obtained = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                is_absent = table.Column<bool>(type: "boolean", nullable: false),
                remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_student_marks", x => x.id);
                table.ForeignKey(
                    name: "fk_student_marks_mark_sheets_tenant_id_mark_sheet_id",
                    columns: x => new { x.tenant_id, x.mark_sheet_id },
                    principalSchema: "exams",
                    principalTable: "mark_sheets",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_exam_papers_campus_id",
            schema: "exams",
            table: "exam_papers",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_exam_papers_tenant_id",
            schema: "exams",
            table: "exam_papers",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_exam_papers_tenant_id_exam_term_id_grade_level_id_subject_id",
            schema: "exams",
            table: "exam_papers",
            columns: new[] { "tenant_id", "exam_term_id", "grade_level_id", "subject_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_exam_papers_tenant_id_subject_id",
            schema: "exams",
            table: "exam_papers",
            columns: new[] { "tenant_id", "subject_id" });

        migrationBuilder.CreateIndex(
            name: "ix_exam_terms_campus_id",
            schema: "exams",
            table: "exam_terms",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_exam_terms_tenant_id",
            schema: "exams",
            table: "exam_terms",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_exam_terms_tenant_id_campus_id_academic_year_id_code",
            schema: "exams",
            table: "exam_terms",
            columns: new[] { "tenant_id", "campus_id", "academic_year_id", "code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_mark_sheets_campus_id",
            schema: "exams",
            table: "mark_sheets",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_mark_sheets_tenant_id",
            schema: "exams",
            table: "mark_sheets",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_mark_sheets_unique_paper_section",
            schema: "exams",
            table: "mark_sheets",
            columns: new[] { "tenant_id", "exam_paper_id", "section_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_student_marks_campus_id",
            schema: "exams",
            table: "student_marks",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_student_marks_tenant_id",
            schema: "exams",
            table: "student_marks",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_student_marks_tenant_id_mark_sheet_id_student_id",
            schema: "exams",
            table: "student_marks",
            columns: new[] { "tenant_id", "mark_sheet_id", "student_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_student_marks_tenant_id_student_id",
            schema: "exams",
            table: "student_marks",
            columns: new[] { "tenant_id", "student_id" });

        migrationBuilder.CreateIndex(
            name: "ix_subjects_tenant_id",
            schema: "exams",
            table: "subjects",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_subjects_tenant_id_school_id_grade_level_id_code",
            schema: "exams",
            table: "subjects",
            columns: new[] { "tenant_id", "school_id", "grade_level_id", "code" },
            unique: true);

        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("exams", "subjects"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("exams", "exam_terms"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("exams", "exam_papers"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("exams", "mark_sheets"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("exams", "student_marks"));
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("exams", "student_marks"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("exams", "mark_sheets"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("exams", "exam_papers"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("exams", "exam_terms"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("exams", "subjects"));

        migrationBuilder.DropTable(
            name: "change_audit_entries",
            schema: "exams");

        migrationBuilder.DropTable(
            name: "security_audit_events",
            schema: "exams");

        migrationBuilder.DropTable(
            name: "student_marks",
            schema: "exams");

        migrationBuilder.DropTable(
            name: "mark_sheets",
            schema: "exams");

        migrationBuilder.DropTable(
            name: "exam_papers",
            schema: "exams");

        migrationBuilder.DropTable(
            name: "exam_terms",
            schema: "exams");

        migrationBuilder.DropTable(
            name: "subjects",
            schema: "exams");
    }
}
