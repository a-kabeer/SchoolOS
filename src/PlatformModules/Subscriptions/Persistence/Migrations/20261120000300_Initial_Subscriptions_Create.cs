using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolOS.BuildingBlocks.Persistence;

#nullable disable

namespace SchoolOS.PlatformModules.Subscriptions.Persistence.Migrations;

[DbContext(typeof(SubscriptionsDbContext))]
[Migration("20261120000300_Initial_Subscriptions_Create")]
public partial class Initial_Subscriptions_Create : Migration
{
    private static readonly string[] TenantScopedTables =
    [
        "subscriptions",
        "tenant_modules",
        "invoices",
        "invoice_lines",
        "payments",
        "add_on_requests",
        "renewal_reminders"
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "subscriptions");

        migrationBuilder.CreateSequence<long>(
            name: "invoice_number_seq",
            schema: "subscriptions",
            startValue: 1L,
            incrementBy: 1);

        migrationBuilder.CreateTable(
            name: "portal_modules",
            schema: "subscriptions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<int>(type: "integer", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                is_core = table.Column<bool>(type: "boolean", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                sort_order = table.Column<int>(type: "integer", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_portal_modules", x => x.id));

        migrationBuilder.CreateTable(
            name: "module_dependencies",
            schema: "subscriptions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                module_id = table.Column<Guid>(type: "uuid", nullable: false),
                requires_module_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_module_dependencies", x => x.id));

        migrationBuilder.CreateTable(
            name: "plans",
            schema: "subscriptions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_plans", x => x.id));

        migrationBuilder.CreateTable(
            name: "plan_modules",
            schema: "subscriptions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                module_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_plan_modules", x => x.id);
                table.ForeignKey("fk_plan_modules_plans_plan_id", x => x.plan_id, "plans", "id", "subscriptions", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("fk_plan_modules_portal_modules_module_id", x => x.module_id, "portal_modules", "id", "subscriptions", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "price_books",
            schema: "subscriptions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                tax_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_price_books", x => x.id));

        migrationBuilder.CreateTable(
            name: "price_book_items",
            schema: "subscriptions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                price_book_id = table.Column<Guid>(type: "uuid", nullable: false),
                plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                module_id = table.Column<Guid>(type: "uuid", nullable: true),
                billing_cycle = table.Column<int>(type: "integer", nullable: false),
                price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_price_book_items", x => x.id);
                table.ForeignKey("fk_price_book_items_price_books_price_book_id", x => x.price_book_id, "price_books", "id", "subscriptions", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "subscriptions",
            schema: "subscriptions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                price_book_id = table.Column<Guid>(type: "uuid", nullable: false),
                billing_cycle = table.Column<int>(type: "integer", nullable: false),
                currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                tax_rate_override = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                status = table.Column<int>(type: "integer", nullable: false),
                start_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                end_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                trial_ends_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                suspended_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                cancelled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_subscriptions", x => x.id);
                table.UniqueConstraint("ak_subscriptions_tenant_id_id", x => new { x.tenant_id, x.id });
            });

        migrationBuilder.CreateTable(
            name: "tenant_modules",
            schema: "subscriptions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                module_id = table.Column<Guid>(type: "uuid", nullable: false),
                module_code = table.Column<int>(type: "integer", nullable: false),
                is_school_wide = table.Column<bool>(type: "boolean", nullable: false),
                campus_id = table.Column<Guid>(type: "uuid", nullable: true),
                status = table.Column<int>(type: "integer", nullable: false),
                valid_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tenant_modules", x => x.id);
                table.ForeignKey(
                    name: "fk_tenant_modules_subscriptions_tenant_id_subscription_id",
                    columns: x => new { x.tenant_id, x.subscription_id },
                    principalSchema: "subscriptions",
                    principalTable: "subscriptions",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "invoices",
            schema: "subscriptions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                invoice_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                period_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                period_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                tax_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                issued_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                due_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                paid_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                voided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_invoices", x => x.id);
                table.UniqueConstraint("ak_invoices_tenant_id_id", x => new { x.tenant_id, x.id });
                table.ForeignKey(
                    name: "fk_invoices_subscriptions_tenant_id_subscription_id",
                    columns: x => new { x.tenant_id, x.subscription_id },
                    principalSchema: "subscriptions",
                    principalTable: "subscriptions",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "invoice_lines",
            schema: "subscriptions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_invoice_lines", x => x.id);
                table.ForeignKey(
                    name: "fk_invoice_lines_invoices_tenant_id_invoice_id",
                    columns: x => new { x.tenant_id, x.invoice_id },
                    principalSchema: "subscriptions",
                    principalTable: "invoices",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "payments",
            schema: "subscriptions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                method = table.Column<int>(type: "integer", nullable: false),
                reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                provider_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_payments", x => x.id);
                table.ForeignKey(
                    name: "fk_payments_invoices_tenant_id_invoice_id",
                    columns: x => new { x.tenant_id, x.invoice_id },
                    principalSchema: "subscriptions",
                    principalTable: "invoices",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "add_on_requests",
            schema: "subscriptions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                module_id = table.Column<Guid>(type: "uuid", nullable: false),
                module_code = table.Column<int>(type: "integer", nullable: false),
                requested_by_user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                status = table.Column<int>(type: "integer", nullable: false),
                requested_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                decided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                decided_by_user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_add_on_requests", x => x.id));

        migrationBuilder.CreateTable(
            name: "renewal_reminders",
            schema: "subscriptions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                period_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                days_before = table.Column<int>(type: "integer", nullable: false),
                queued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_renewal_reminders", x => x.id));

        migrationBuilder.CreateTable(
            name: "change_audit_entries",
            schema: "subscriptions",
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
            constraints: table => table.PrimaryKey("pk_change_audit_entries", x => x.id));

        migrationBuilder.CreateTable(
            name: "security_audit_events",
            schema: "subscriptions",
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
            constraints: table => table.PrimaryKey("pk_security_audit_events", x => x.id));

        migrationBuilder.CreateIndex("ix_portal_modules_code", "portal_modules", "code", "subscriptions", unique: true);
        migrationBuilder.CreateIndex("ix_module_dependencies_module_id_requires_module_id", "module_dependencies", new[] { "module_id", "requires_module_id" }, "subscriptions", unique: true);
        migrationBuilder.CreateIndex("ix_plans_code", "plans", "code", "subscriptions", unique: true);
        migrationBuilder.CreateIndex("ix_plan_modules_plan_id_module_id", "plan_modules", new[] { "plan_id", "module_id" }, "subscriptions", unique: true);
        migrationBuilder.CreateIndex("ix_price_books_country_code_is_active", "price_books", new[] { "country_code", "is_active" }, "subscriptions");
        migrationBuilder.CreateIndex("ix_price_book_items_price_book_id_plan_id_module_id_billing_cycle", "price_book_items", new[] { "price_book_id", "plan_id", "module_id", "billing_cycle" }, "subscriptions", unique: true);
        migrationBuilder.CreateIndex("ix_subscriptions_tenant_id_status", "subscriptions", new[] { "tenant_id", "status" }, "subscriptions");
        migrationBuilder.CreateIndex("ix_tenant_modules_tenant_id_subscription_id_module_id", "tenant_modules", new[] { "tenant_id", "subscription_id", "module_id" }, "subscriptions", unique: true);
        migrationBuilder.CreateIndex("ix_invoices_invoice_number", "invoices", "invoice_number", "subscriptions", unique: true);
        migrationBuilder.CreateIndex("ix_invoices_tenant_id_subscription_id_status", "invoices", new[] { "tenant_id", "subscription_id", "status" }, "subscriptions");
        migrationBuilder.CreateIndex("ix_add_on_requests_tenant_id_status", "add_on_requests", new[] { "tenant_id", "status" }, "subscriptions");
        migrationBuilder.CreateIndex("ix_renewal_reminders_tenant_id_subscription_id_period_end_days_before", "renewal_reminders", new[] { "tenant_id", "subscription_id", "period_end", "days_before" }, "subscriptions", unique: true);

        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'schoolos_platform_admin') THEN
                    CREATE ROLE schoolos_platform_admin NOLOGIN NOINHERIT NOSUPERUSER NOBYPASSRLS;
                END IF;
            END $$;
            """);

        foreach (var table in TenantScopedTables)
        {
            migrationBuilder.Sql(PostgresRlsHelper.GenerateEnableRlsSql("subscriptions", table));
            migrationBuilder.Sql(PostgresRlsHelper.GeneratePlatformAdminRolePolicySql("subscriptions", table, "schoolos_platform_admin"));
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in TenantScopedTables)
        {
            migrationBuilder.Sql(PostgresRlsHelper.GenerateDropPlatformAdminRolePolicySql("subscriptions", table));
            migrationBuilder.Sql(PostgresRlsHelper.GenerateDisableRlsSql("subscriptions", table));
        }

        migrationBuilder.DropTable("security_audit_events", "subscriptions");
        migrationBuilder.DropTable("change_audit_entries", "subscriptions");
        migrationBuilder.DropTable("renewal_reminders", "subscriptions");
        migrationBuilder.DropTable("add_on_requests", "subscriptions");
        migrationBuilder.DropTable("payments", "subscriptions");
        migrationBuilder.DropTable("invoice_lines", "subscriptions");
        migrationBuilder.DropTable("invoices", "subscriptions");
        migrationBuilder.DropTable("tenant_modules", "subscriptions");
        migrationBuilder.DropTable("subscriptions", "subscriptions");
        migrationBuilder.DropTable("price_book_items", "subscriptions");
        migrationBuilder.DropTable("price_books", "subscriptions");
        migrationBuilder.DropTable("plan_modules", "subscriptions");
        migrationBuilder.DropTable("plans", "subscriptions");
        migrationBuilder.DropTable("module_dependencies", "subscriptions");
        migrationBuilder.DropTable("portal_modules", "subscriptions");

        migrationBuilder.DropSequence(name: "invoice_number_seq", schema: "subscriptions");
    }
}
