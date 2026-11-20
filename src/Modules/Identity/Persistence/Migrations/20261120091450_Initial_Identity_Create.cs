using System;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolOS.BuildingBlocks.Persistence;

#nullable disable

namespace SchoolOS.Modules.Identity.Persistence.Migrations;

/// <inheritdoc />
public partial class Initial_Identity_Create : Migration
{
    private static readonly string[] TenantScopedTables =
    [
        "users",
        "user_credentials",
        "user_sessions",
        "school_roles",
        "role_permissions",
        "user_campus_assignments",
        "user_role_assignments"
    ];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "identity");

        migrationBuilder.CreateTable(
            name: "change_audit_entries",
            schema: "identity",
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
            name: "school_roles",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                school_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                is_system_role = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_school_roles", x => x.id);
                table.UniqueConstraint("ak_school_roles_tenant_id_id", x => new { x.tenant_id, x.id });
            });

        migrationBuilder.CreateTable(
            name: "security_audit_events",
            schema: "identity",
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
            name: "users",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                school_id = table.Column<Guid>(type: "uuid", nullable: true),
                email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                middle_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                phone_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                user_type = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                access_failed_count = table.Column<int>(type: "integer", nullable: false),
                lockout_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                credential_version = table.Column<int>(type: "integer", nullable: false),
                security_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                preferred_language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_users", x => x.id);
                table.UniqueConstraint("ak_users_tenant_id_id", x => new { x.tenant_id, x.id });
            });

        migrationBuilder.CreateTable(
            name: "role_permissions",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                school_role_id = table.Column<Guid>(type: "uuid", nullable: false),
                module = table.Column<int>(type: "integer", nullable: false),
                can_view = table.Column<bool>(type: "boolean", nullable: false),
                can_create = table.Column<bool>(type: "boolean", nullable: false),
                can_edit = table.Column<bool>(type: "boolean", nullable: false),
                can_delete = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_role_permissions", x => x.id);
                table.ForeignKey(
                    name: "fk_role_permissions_school_roles_tenant_id_school_role_id",
                    columns: x => new { x.tenant_id, x.school_role_id },
                    principalSchema: "identity",
                    principalTable: "school_roles",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "user_campus_assignments",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                school_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                is_primary = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_user_campus_assignments", x => x.id);
                table.ForeignKey(
                    name: "fk_user_campus_assignments_users_tenant_id_user_id",
                    columns: x => new { x.tenant_id, x.user_id },
                    principalSchema: "identity",
                    principalTable: "users",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "user_credentials",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                hash_algorithm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                iterations = table.Column<int>(type: "integer", nullable: false),
                must_change_password_on_next_login = table.Column<bool>(type: "boolean", nullable: false),
                last_changed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_user_credentials", x => x.id);
                table.ForeignKey(
                    name: "fk_user_credentials_users_tenant_id_user_id",
                    columns: x => new { x.tenant_id, x.user_id },
                    principalSchema: "identity",
                    principalTable: "users",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "user_role_assignments",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                school_role_id = table.Column<Guid>(type: "uuid", nullable: false),
                campus_id = table.Column<Guid>(type: "uuid", nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_user_role_assignments", x => x.id);
                table.ForeignKey(
                    name: "fk_user_role_assignments_school_roles_tenant_id_school_role_id",
                    columns: x => new { x.tenant_id, x.school_role_id },
                    principalSchema: "identity",
                    principalTable: "school_roles",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_user_role_assignments_users_tenant_id_user_id",
                    columns: x => new { x.tenant_id, x.user_id },
                    principalSchema: "identity",
                    principalTable: "users",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "user_sessions",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                active_campus_id = table.Column<Guid>(type: "uuid", nullable: true),
                refresh_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                previous_refresh_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                refresh_token_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_from_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                is_revoked = table.Column<bool>(type: "boolean", nullable: false),
                revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                revoked_reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                last_activity_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_user_sessions", x => x.id);
                table.ForeignKey(
                    name: "fk_user_sessions_users_tenant_id_user_id",
                    columns: x => new { x.tenant_id, x.user_id },
                    principalSchema: "identity",
                    principalTable: "users",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_role_permissions_tenant_id",
            schema: "identity",
            table: "role_permissions",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_role_permissions_tenant_id_school_role_id_module",
            schema: "identity",
            table: "role_permissions",
            columns: new[] { "tenant_id", "school_role_id", "module" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_school_roles_tenant_id",
            schema: "identity",
            table: "school_roles",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_school_roles_tenant_id_school_id_name",
            schema: "identity",
            table: "school_roles",
            columns: new[] { "tenant_id", "school_id", "name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_user_campus_assignments_campus_id",
            schema: "identity",
            table: "user_campus_assignments",
            column: "campus_id");

        migrationBuilder.CreateIndex(
            name: "ix_user_campus_assignments_tenant_id",
            schema: "identity",
            table: "user_campus_assignments",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_user_campus_assignments_tenant_id_user_id_campus_id",
            schema: "identity",
            table: "user_campus_assignments",
            columns: new[] { "tenant_id", "user_id", "campus_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_user_credentials_tenant_id",
            schema: "identity",
            table: "user_credentials",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_user_credentials_tenant_id_user_id",
            schema: "identity",
            table: "user_credentials",
            columns: new[] { "tenant_id", "user_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_user_role_assignments_tenant_id",
            schema: "identity",
            table: "user_role_assignments",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_user_role_assignments_tenant_id_school_role_id",
            schema: "identity",
            table: "user_role_assignments",
            columns: new[] { "tenant_id", "school_role_id" });

        migrationBuilder.CreateIndex(
            name: "ix_user_role_assignments_tenant_id_user_id_school_role_id_camp",
            schema: "identity",
            table: "user_role_assignments",
            columns: new[] { "tenant_id", "user_id", "school_role_id", "campus_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_user_sessions_previous_refresh_token_hash",
            schema: "identity",
            table: "user_sessions",
            column: "previous_refresh_token_hash");

        migrationBuilder.CreateIndex(
            name: "ix_user_sessions_refresh_token_hash",
            schema: "identity",
            table: "user_sessions",
            column: "refresh_token_hash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_user_sessions_tenant_id",
            schema: "identity",
            table: "user_sessions",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_user_sessions_tenant_id_user_id",
            schema: "identity",
            table: "user_sessions",
            columns: new[] { "tenant_id", "user_id" });

        migrationBuilder.CreateIndex(
            name: "ix_users_tenant_id",
            schema: "identity",
            table: "users",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_users_tenant_id_email",
            schema: "identity",
            table: "users",
            columns: new[] { "tenant_id", "email" },
            unique: true);

        foreach (var table in TenantScopedTables)
        {
            migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("identity", table));
        }
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in TenantScopedTables.Reverse())
        {
            migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("identity", table));
        }

        migrationBuilder.DropTable(
            name: "change_audit_entries",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "role_permissions",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "security_audit_events",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "user_campus_assignments",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "user_credentials",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "user_role_assignments",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "user_sessions",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "school_roles",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "users",
            schema: "identity");
    }
}
