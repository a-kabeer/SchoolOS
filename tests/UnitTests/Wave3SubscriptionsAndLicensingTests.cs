using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.PlatformModules.Licensing.Application;
using SchoolOS.PlatformModules.Subscriptions.Application;
using SchoolOS.PlatformModules.Subscriptions.Domain;
using SchoolOS.PlatformModules.Subscriptions.Persistence;
using SchoolOS.PlatformModules.Tenants.Domain;
using Shouldly;
using Xunit;

namespace SchoolOS.UnitTests;

public class Wave3SubscriptionsAndLicensingTests
{
    private sealed class MutableTimeProvider(DateTimeOffset startUtc) : TimeProvider
    {
        private DateTimeOffset _utcNow = startUtc;
        public override DateTimeOffset GetUtcNow() => _utcNow;
        public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);
    }

    private static SubscriptionsDbContext CreateDbContext(string dbName, ITenantContext tenantContext)
    {
        var options = new DbContextOptionsBuilder<SubscriptionsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new SubscriptionsDbContext(options, tenantContext);
    }

    [Fact]
    public void ModuleDependencyGraph_ShouldResolveTransitiveClosure_AndDetectCycles()
    {
        var admissionsId = Guid.NewGuid();
        var onlineAdmissionsId = Guid.NewGuid();
        var portalPaymentId = Guid.NewGuid();

        var edges = new[]
        {
            (onlineAdmissionsId, admissionsId),
            (portalPaymentId, onlineAdmissionsId)
        };

        var closure = ModuleDependencyGraph.RequiredBy(edges, [portalPaymentId]);
        closure.ShouldContain(portalPaymentId);
        closure.ShouldContain(onlineAdmissionsId);
        closure.ShouldContain(admissionsId);

        ModuleDependencyGraph.WouldCreateCycle(edges, admissionsId, portalPaymentId).ShouldBeTrue();
        ModuleDependencyGraph.WouldCreateCycle(edges, Guid.NewGuid(), portalPaymentId).ShouldBeFalse();
    }

    [Fact]
    public void SubscriptionQuoteRules_ShouldAutoIncludeCore_ResolveDependencies_AndAvoidDoubleChargingPlanModules()
    {
        var academic = PortalModule.Create(ModuleCode.AcademicSetup, "Academic", "Academic", isCore: true, 3);
        var exams = PortalModule.Create(ModuleCode.Exams, "Exams", "Exams", isCore: false, 11);
        var admissions = PortalModule.Create(ModuleCode.Admissions, "Admissions", "Adm", isCore: false, 7);
        var timetable = PortalModule.Create(ModuleCode.Timetable, "Timetable", "TT", isCore: false, 12);
        var lms = PortalModule.Create(ModuleCode.Lms, "LMS", "LMS", isCore: false, 16);
        var catalog = new[] { academic, admissions, exams, timetable, lms };

        var plan = Plan.Create("STARTER", "Starter", "Starter plan");
        plan.AddModule(admissions);
        var planModuleIds = new HashSet<Guid> { admissions.Id };

        var deps = new[] { (lms.Id, academic.Id), (lms.Id, timetable.Id) };
        var resolved = SubscriptionQuoteRules.ResolveModules(catalog, deps, planModuleIds, [ModuleCode.Lms]);
        resolved.Select(m => m.Code).ShouldBe([ModuleCode.AcademicSetup, ModuleCode.Admissions, ModuleCode.Timetable, ModuleCode.Lms]);

        var priceBook = PriceBook.Create("PK", "PKR", 16m);
        var prices = new[]
        {
            PriceBookItem.ForPlan(priceBook.Id, plan.Id, BillingCycle.Monthly, 15000m),
            PriceBookItem.ForModule(priceBook.Id, admissions.Id, BillingCycle.Monthly, 5000m),
            PriceBookItem.ForModule(priceBook.Id, timetable.Id, BillingCycle.Monthly, 4000m),
            PriceBookItem.ForModule(priceBook.Id, lms.Id, BillingCycle.Monthly, 3000m)
        };

        var total = SubscriptionQuoteRules.Price(plan, planModuleIds, resolved, prices);
        total.ShouldBe(22000m);

        Should.Throw<ArgumentException>(() =>
            SubscriptionQuoteRules.ResolveModules(catalog, deps, planModuleIds, [ModuleCode.UsersRoles]));
    }

    [Fact]
    public void InvoiceAndPayment_ShouldComputeTax_EnforceFullPayment_AndRejectPartialPayment()
    {
        var school = PortalModule.Create(ModuleCode.School, "School", "School", isCore: true, 1);
        var adm = PortalModule.Create(ModuleCode.Admissions, "Adm", "Adm", isCore: false, 7);
        var plan = Plan.Create("STARTER", "Starter", "Starter");
        var book = PriceBook.Create("PK", "PKR", 16m);

        var now = new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
        var tenantId = Guid.NewGuid();
        var sub = Subscription.StartTrial(tenantId, plan, book, BillingCycle.Monthly, 10000m, [school, adm], now);

        Should.Throw<InvalidOperationException>(() =>
            Invoice.Create(sub, "INV-000001", 16m, now, nextPeriod: false, "Current period invalid on trial"));

        var invoice = Invoice.Create(sub, "INV-000001", 16m, now, nextPeriod: true, "STARTER Monthly");
        invoice.Subtotal.ShouldBe(10000m);
        invoice.TaxAmount.ShouldBe(1600m);
        invoice.Total.ShouldBe(11600m);
        invoice.PeriodStart.ShouldBe(sub.EndDate);

        Should.Throw<InvalidOperationException>(() =>
            invoice.RecordPayment(5000m, PaymentMethod.BankTransfer, "TXN-PARTIAL", now, "manual"));

        var payment = invoice.RecordPayment(11600m, PaymentMethod.BankTransfer, "TXN-FULL-01", now, "manual");
        payment.Amount.ShouldBe(11600m);
        invoice.Status.ShouldBe(InvoiceStatus.Paid);

        sub.ApplyPaidPeriod(invoice.PeriodEnd);
        sub.Status.ShouldBe(SubscriptionStatus.Active);
        sub.EndDate.ShouldBe(invoice.PeriodEnd);
    }

    [Fact]
    public async Task SubscriptionLifecycleService_ShouldQueueDeduplicatedReminders_TransitionPastDue_AndSuspendPaidModulesOnly()
    {
        var dbName = Guid.NewGuid().ToString();
        var fakeTime = new MutableTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var adminContext = new TenantContext(null, null, "platform-admin", isPlatformAdmin: true);
        var tenantId = Guid.NewGuid();

        await using var db = CreateDbContext(dbName, adminContext);
        await SubscriptionCatalogSeeder.SeedAsync(db);

        var book = PriceBook.Create("PK", "PKR", 16m);
        var starter = await db.Plans.Include(p => p.Modules).FirstAsync(p => p.Code == "STARTER");
        var lib = await db.PortalModules.FirstAsync(m => m.Code == ModuleCode.Library);
        book.SetPlanPrice(starter.Id, BillingCycle.Monthly, 10000m);
        book.SetModulePrice(lib.Id, BillingCycle.Monthly, 2500m);
        db.PriceBooks.Add(book);
        await db.SaveChangesAsync();

        var auditLog = new StructuredSecurityAuditLog(adminContext, fakeTime);
        var manager = new SubscriptionManager(db, adminContext, new ManualPaymentProvider(), new SequenceInvoiceNumberGenerator(), auditLog, fakeTime);
        await manager.AssignSubscriptionAsync(tenantId, "PK", "STARTER", [ModuleCode.Library], BillingCycle.Monthly, isTrial: false);

        var lifecycle = new SubscriptionLifecycleService(db, adminContext, fakeTime);

        fakeTime.Advance(TimeSpan.FromDays(25)); // ~6 days remaining -> qualifies for thresholds 30 and 7
        var sweep1 = await lifecycle.RunSweepAsync();
        sweep1.QueuedReminders.ShouldBe(2);
        sweep1.MarkedPastDue.ShouldBe(0);

        // Re-running on same day must be idempotent
        var sweep2 = await lifecycle.RunSweepAsync();
        sweep2.QueuedReminders.ShouldBe(0);

        fakeTime.Advance(TimeSpan.FromDays(7)); // past 1-month end date -> PastDue
        var sweep3 = await lifecycle.RunSweepAsync();
        sweep3.MarkedPastDue.ShouldBe(1);

        fakeTime.Advance(TimeSpan.FromDays(Subscription.PastDueGraceDays)); // past grace -> Suspended
        var sweep4 = await lifecycle.RunSweepAsync();
        sweep4.Suspended.ShouldBe(1);

        var reloaded = await db.Subscriptions.IgnoreQueryFilters().Include(s => s.Modules).FirstAsync(s => s.TenantId == tenantId);
        reloaded.Status.ShouldBe(SubscriptionStatus.Suspended);
        reloaded.Modules.Single(m => m.ModuleCode == ModuleCode.School).Status.ShouldBe(TenantModuleStatus.Active);
        reloaded.Modules.Single(m => m.ModuleCode == ModuleCode.Admissions).Status.ShouldBe(TenantModuleStatus.Suspended);
        reloaded.Modules.Single(m => m.ModuleCode == ModuleCode.Library).Status.ShouldBe(TenantModuleStatus.Suspended);
    }

    [Fact]
    public async Task OfflineLicenseLeaseVerifier_ShouldVerifyValidEs256_RejectTamperedExpiredOrWrongTenant_AndKeepCoreActive()
    {
        var keys = LicensingSigningOptions.CreateEphemeral();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var fakeTime = new MutableTimeProvider(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));
        var now = fakeTime.GetUtcNow();

        var payload = new LicenseLeasePayload(
            tenantA,
            now,
            now.AddDays(14),
            keys.KeyId,
            "STARTER",
            [ModuleCode.School, ModuleCode.UsersRoles, ModuleCode.Admissions]);

        var lease = OfflineLicenseLeaseVerifier.SignWithPrivateKey(payload, keys.PrivateKeyPem);

        // Valid verification
        var ok = OfflineLicenseLeaseVerifier.Verify(lease, tenantA, keys.PublicKeyPem, now.AddDays(5));
        ok.IsValid.ShouldBeTrue();
        ok.EntitledModules.ShouldContain(ModuleCode.Admissions);

        // Wrong tenant fails closed
        var wrongTenant = OfflineLicenseLeaseVerifier.Verify(lease, tenantB, keys.PublicKeyPem, now);
        wrongTenant.IsValid.ShouldBeFalse();
        wrongTenant.FailureReason.ShouldBe("TENANT_MISMATCH");

        // Tampered payload fails closed to Core only
        var tampered = lease with { PayloadJson = lease.PayloadJson.Replace("Admissions", "HrPayroll", StringComparison.Ordinal) };
        var tamperedResult = OfflineLicenseLeaseVerifier.Verify(tampered, tenantA, keys.PublicKeyPem, now);
        tamperedResult.IsValid.ShouldBeFalse();
        tamperedResult.FailureReason.ShouldBe("INVALID_SIGNATURE");
        tamperedResult.EntitledModules.ShouldNotContain(ModuleCode.Admissions);
        tamperedResult.EntitledModules.ShouldContain(ModuleCode.School);

        // Store + OfflineLicenseLeaseModuleAccess expiration test
        var store = new InMemoryLicenseLeaseStore();
        await store.SetLeaseAsync(tenantA, lease);
        var moduleAccess = new OfflineLicenseLeaseModuleAccess(
            store,
            new OfflineLicenseVerificationOptions { PublicKeyPem = keys.PublicKeyPem },
            fakeTime);

        (await moduleAccess.HasModuleAsync(tenantA, ModuleCode.Admissions)).ShouldBeTrue();
        (await moduleAccess.HasModuleAsync(tenantA, ModuleCode.Library)).ShouldBeFalse();

        fakeTime.Advance(TimeSpan.FromDays(15));
        (await moduleAccess.HasModuleAsync(tenantA, ModuleCode.Admissions)).ShouldBeFalse();
        (await moduleAccess.HasModuleAsync(tenantA, ModuleCode.School)).ShouldBeTrue();
    }

    [Fact]
    public void SchoolRequest_ShouldValidateAndApproveAtomically()
    {
        var now = DateTimeOffset.UtcNow;
        Should.Throw<ArgumentException>(() => SchoolRequest.Submit("Test School", "Ali", "not-an-email", now));

        var req = SchoolRequest.Submit("Crescent Academy", "Tariq Mahmood", "tariq@crescent.edu.pk", now);
        req.Status.ShouldBe(SchoolRequestStatus.Pending);

        var tenantId = Guid.NewGuid();
        req.Approve(tenantId, "platform-admin", now.AddHours(1), "Verified documents");
        req.Status.ShouldBe(SchoolRequestStatus.Approved);
        req.ApprovedTenantId.ShouldBe(tenantId);

        Should.Throw<InvalidOperationException>(() => req.Reject("platform-admin", now.AddHours(2)));
    }
}
