using System;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolOS.BuildingBlocks.Persistence;

#nullable disable

namespace SchoolOS.Modules.Organization.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_AcademicYears_GradeLevels_And_Sections : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "academic_years",
            schema: "organization",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                school_id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                start_date = table.Column<DateOnly>(type: "date", nullable: false),
                end_date = table.Column<DateOnly>(type: "date", nullable: false),
                is_current = table.Column<bool>(type: "boolean", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_academic_years", x => x.id);
                table.UniqueConstraint("ak_academic_years_tenant_id_id", x => new { x.tenant_id, x.id });
                table.CheckConstraint("ck_academic_years_date_range", "end_date >= start_date");
                table.ForeignKey(
                    name: "fk_academic_years_schools_tenant_id_school_id",
                    columns: x => new { x.tenant_id, x.school_id },
                    principalSchema: "organization",
                    principalTable: "schools",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "grade_levels",
            schema: "organization",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                school_id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                sequence_order = table.Column<int>(type: "integer", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_grade_levels", x => x.id);
                table.UniqueConstraint("ak_grade_levels_tenant_id_id", x => new { x.tenant_id, x.id });
                table.ForeignKey(
                    name: "fk_grade_levels_schools_tenant_id_school_id",
                    columns: x => new { x.tenant_id, x.school_id },
                    principalSchema: "organization",
                    principalTable: "schools",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "sections",
            schema: "organization",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                school_id = table.Column<Guid>(type: "uuid", nullable: false),
                campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                academic_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                grade_level_id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                capacity = table.Column<int>(type: "integer", nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_sections", x => x.id);
                table.UniqueConstraint("ak_sections_tenant_id_id", x => new { x.tenant_id, x.id });
                table.ForeignKey(
                    name: "fk_sections_academic_years_tenant_id_academic_year_id",
                    columns: x => new { x.tenant_id, x.academic_year_id },
                    principalSchema: "organization",
                    principalTable: "academic_years",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_sections_campuses_tenant_id_campus_id",
                    columns: x => new { x.tenant_id, x.campus_id },
                    principalSchema: "organization",
                    principalTable: "campuses",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_sections_grade_levels_tenant_id_grade_level_id",
                    columns: x => new { x.tenant_id, x.grade_level_id },
                    principalSchema: "organization",
                    principalTable: "grade_levels",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_academic_years_single_current_per_school",
            schema: "organization",
            table: "academic_years",
            columns: new[] { "tenant_id", "school_id" },
            unique: true,
            filter: "is_current = true");

        migrationBuilder.CreateIndex(
            name: "ix_academic_years_tenant_id",
            schema: "organization",
            table: "academic_years",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_academic_years_tenant_id_school_id_code",
            schema: "organization",
            table: "academic_years",
            columns: new[] { "tenant_id", "school_id", "code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_grade_levels_tenant_id",
            schema: "organization",
            table: "grade_levels",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_grade_levels_tenant_id_school_id_code",
            schema: "organization",
            table: "grade_levels",
            columns: new[] { "tenant_id", "school_id", "code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_sections_campus_id",
            schema: "organization",
            table: "sections",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_sections_tenant_id",
            schema: "organization",
            table: "sections",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_sections_tenant_id_academic_year_id",
            schema: "organization",
            table: "sections",
            columns: new[] { "tenant_id", "academic_year_id" });

        migrationBuilder.CreateIndex(
            name: "ix_sections_tenant_id_campus_id_academic_year_id_grade_level_i",
            schema: "organization",
            table: "sections",
            columns: new[] { "tenant_id", "campus_id", "academic_year_id", "grade_level_id", "code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_sections_tenant_id_grade_level_id",
            schema: "organization",
            table: "sections",
            columns: new[] { "tenant_id", "grade_level_id" });

        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("organization", "academic_years"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("organization", "grade_levels"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("organization", "sections"));
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("organization", "sections"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("organization", "grade_levels"));
        migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("organization", "academic_years"));

        migrationBuilder.DropTable(
            name: "sections",
            schema: "organization");

        migrationBuilder.DropTable(
            name: "academic_years",
            schema: "organization");

        migrationBuilder.DropTable(
            name: "grade_levels",
            schema: "organization");
    }
}
