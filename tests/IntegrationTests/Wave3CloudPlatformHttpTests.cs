using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.PlatformModules.Subscriptions.Domain;
using SchoolOS.PlatformModules.Tenants.Domain;
using Shouldly;
using Xunit;

namespace SchoolOS.IntegrationTests;

public sealed class Wave3CloudPlatformHttpTests : IClassFixture<CloudApiFactory>
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    private readonly CloudApiFactory _factory;

    public Wave3CloudPlatformHttpTests(CloudApiFactory factory)
    {
        _factory = factory;
    }

    private string CreateToken(string userId, Guid? tenantId, string[] roles, string[] scopes)
    {
        var opts = _factory.Services.GetRequiredService<JwtOptions>();
        return JwtTokenHelper.GenerateToken(opts, userId, tenantId, null, roles, scopes);
    }

    [Fact]
    public async Task CloudControlPlane_OnboardingToSubscriptionPaymentAddOnAndLicenseLease_ShouldExecuteEndToEnd()
    {
        var client = _factory.CreateClient();

        // 1. Anonymous visitor submits public school onboarding request
        var submitResp = await client.PostAsJsonAsync(
            "/api/v1/public/school-requests",
            new { schoolName = "Al-Noor Grammar School", contactName = "Amina Tariq", contactEmail = "amina@alnoor.edu.pk" },
            WebJson);
        submitResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var submitted = await submitResp.Content.ReadFromJsonAsync<SchoolRequestResult>(WebJson);
        submitted.ShouldNotBeNull();
        submitted.Status.ShouldBe(SchoolRequestStatus.Pending);

        // 2. PlatformAdmin configures Country PK + Active PriceBook
        var platformToken = CreateToken("platform-admin-1", null, [SecurityPolicies.PlatformAdminRole], [SecurityPolicies.PlatformAdminScope]);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);

        var seedResp = await client.PostAsJsonAsync("/api/v1/platform/modules/seed", new { }, WebJson);
        seedResp.EnsureSuccessStatusCode();

        var countryResp = await client.PostAsJsonAsync(
            "/api/v1/platform/countries",
            new
            {
                code = "PK",
                name = "Pakistan",
                languagesCsv = "en,ur",
                defaultLanguage = "en",
                timeZoneId = "Asia/Karachi",
                currencyCode = "PKR",
                isActive = true
            },
            WebJson);
        countryResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        var bookResp = await client.PostAsJsonAsync(
            "/api/v1/platform/price-books",
            new
            {
                countryCode = "PK",
                currencyCode = "PKR",
                taxRate = 16m,
                planPrices = new[]
                {
                    new { planCode = "STARTER", billingCycle = BillingCycle.Monthly, price = 15000m }
                },
                modulePrices = new[]
                {
                    new { moduleCode = ModuleCode.Library, billingCycle = BillingCycle.Monthly, price = 2500m }
                }
            },
            WebJson);
        bookResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // 3. Approve SchoolRequest -> Tenant + 30-day STARTER Trial provisioned together
        var approveResp = await client.PostAsJsonAsync(
            $"/api/v1/platform/school-requests/{submitted.Id}/approve",
            new { slug = $"al-noor-{Guid.NewGuid():N}"[..16], countryCode = "PK", dataRegion = DataRegion.SouthAsia },
            WebJson);
        approveResp.EnsureSuccessStatusCode();
        var approved = await approveResp.Content.ReadFromJsonAsync<ApproveSchoolResponse>(WebJson);
        approved.ShouldNotBeNull();
        approved.Status.ShouldBe(SchoolRequestStatus.Approved);
        approved.SubscriptionId.ShouldNotBeNull();

        // 4. Create next-period renewal Invoice & record full BankTransfer Payment
        var invResp = await client.PostAsJsonAsync(
            "/api/v1/platform/invoices",
            new { tenantId = approved.TenantId, nextPeriod = true },
            WebJson);
        invResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var invoice = await invResp.Content.ReadFromJsonAsync<InvoiceResponse>(WebJson);
        invoice.ShouldNotBeNull();
        invoice.Subtotal.ShouldBe(15000m);
        invoice.TaxAmount.ShouldBe(2400m);
        invoice.Total.ShouldBe(17400m);

        var payResp = await client.PostAsJsonAsync(
            $"/api/v1/platform/invoices/{invoice.Id}/payments",
            new { amount = 17400m, method = PaymentMethod.BankTransfer, reference = "HBL-2026-991", receivedAtUtc = DateTimeOffset.UtcNow },
            WebJson);
        var payBody = await payResp.Content.ReadAsStringAsync();
        payResp.IsSuccessStatusCode.ShouldBeTrue(payBody);

        // 5. TenantAdmin requests Library add-on -> PlatformAdmin approves -> Subscription repriced & Library enabled
        var tenantToken = CreateToken(
            "tenant-admin-1",
            approved.TenantId,
            [SecurityPolicies.TenantAdminRole],
            [SecurityPolicies.OrganizationManageScope]);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tenantToken);

        var addOnReqResp = await client.PostAsJsonAsync(
            "/api/v1/subscriptions/addon-requests",
            new { moduleCode = ModuleCode.Library, notes = "Need library circulation for main campus" },
            WebJson);
        addOnReqResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var addOnReq = await addOnReqResp.Content.ReadFromJsonAsync<AddOnReqResult>(WebJson);
        addOnReq.ShouldNotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        var approveAddOnResp = await client.PostAsJsonAsync(
            $"/api/v1/platform/addon-requests/{addOnReq.Id}/approve",
            new { },
            WebJson);
        approveAddOnResp.EnsureSuccessStatusCode();

        // 6. Verify Tenant sees active STARTER + Library entitlements
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tenantToken);
        var currentResp = await client.GetAsync("/api/v1/subscriptions/current");
        currentResp.EnsureSuccessStatusCode();
        var current = await currentResp.Content.ReadFromJsonAsync<CurrentSubscriptionResponse>(WebJson);
        current.ShouldNotBeNull();
        current.Status.ShouldBe(SubscriptionStatus.Active);
        current.Amount.ShouldBe(17500m);
        current.EnabledModules.ShouldContain(ModuleCode.Admissions);
        current.EnabledModules.ShouldContain(ModuleCode.Library);

        // 7. Tenant refreshes signed ECDSA P-256 lease and verifies offline using Cloud public key
        var leaseResp = await client.PostAsJsonAsync("/api/v1/licensing/lease/refresh", new { }, WebJson);
        leaseResp.EnsureSuccessStatusCode();
        var lease = await leaseResp.Content.ReadFromJsonAsync<SignedLicenseLease>(WebJson);
        lease.ShouldNotBeNull();

        var pubKeyResp = await client.GetFromJsonAsync<PublicKeyResponse>("/api/v1/platform/licensing/public-key", WebJson);
        pubKeyResp.ShouldNotBeNull();

        var verified = OfflineLicenseLeaseVerifier.Verify(lease, approved.TenantId, pubKeyResp.PublicKeyPem, DateTimeOffset.UtcNow);
        verified.IsValid.ShouldBeTrue();
        verified.EntitledModules.ShouldContain(ModuleCode.Library);
    }

    private sealed record SchoolRequestResult(Guid Id, SchoolRequestStatus Status);
    private sealed record ApproveSchoolResponse(Guid Id, Guid TenantId, string Slug, Guid? SubscriptionId, SchoolRequestStatus Status);
    private sealed record InvoiceResponse(Guid Id, Guid TenantId, string InvoiceNumber, decimal Subtotal, decimal TaxRate, decimal TaxAmount, decimal Total, InvoiceStatus Status);
    private sealed record AddOnReqResult(Guid Id, ModuleCode ModuleCode, AddOnRequestStatus Status);
    private sealed record CurrentSubscriptionResponse(Guid Id, SubscriptionStatus Status, decimal Amount, List<ModuleCode> EnabledModules);
    private sealed record PublicKeyResponse(string KeyId, string Algorithm, string PublicKeyPem);
}
