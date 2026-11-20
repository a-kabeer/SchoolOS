using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SchoolOS.BuildingBlocks.Observability;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Secrets;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.SharedKernel;
using Shouldly;
using Xunit;

namespace SchoolOS.UnitTests;

public class Wave1BuildingBlocksTests
{
    private enum SampleKind { Alpha = 1, Beta = 2 }

    [Fact]
    public void EnumNames_Accepts_MemberNames_And_Rejects_NumericStrings()
    {
        EnumNames.TryParse<SampleKind>("alpha", out var parsed).ShouldBeTrue();
        parsed.ShouldBe(SampleKind.Alpha);

        EnumNames.TryParse<SampleKind>("1", out _).ShouldBeFalse();
        EnumNames.TryParse<SampleKind>("99", out _).ShouldBeFalse();
        EnumNames.TryParse<SampleKind>("", out _).ShouldBeFalse();
    }

    [Fact]
    public void SecretKey_Validates_Platform_And_TenantGuid_And_Blocks_Traversal()
    {
        var tenantId = Guid.CreateVersion7();
        var platformKey = SecretKey.Platform("db-connection");
        platformKey.Path.ShouldBe("platform/db-connection");
        platformKey.IsPlatform.ShouldBeTrue();

        var tenantKey = SecretKey.ForTenant(tenantId, "sms-api-key");
        tenantKey.Path.ShouldBe($"tenants/{tenantId:D}/sms-api-key");
        tenantKey.IsPlatform.ShouldBeFalse();

        SecretKey.Parse(tenantKey.Path).ShouldBe(tenantKey);
        AzureKeyVaultSecretProvider.ToSecretName(tenantKey).ShouldBe($"tenant-{tenantId:N}-sms-api-key");

        Should.Throw<ArgumentException>(() => SecretKey.Platform("../etc/passwd"));
        Should.Throw<ArgumentException>(() => SecretKey.Platform("UPPERCASE"));
        Should.Throw<ArgumentException>(() => SecretKey.ForTenant(Guid.Empty, "valid-name"));
        Should.Throw<ArgumentException>(() => SecretKey.Parse("tenants/not-a-guid/key"));
    }

    [Fact]
    public async Task TenantSecretProvider_FailsClosed_Without_Tenant_And_Scopes_By_Context()
    {
        var fakeStore = new InMemorySecretProvider();
        var noTenant = new TenantSecretProvider(new TenantContext(null, null, "user-1"), fakeStore);

        await Should.ThrowAsync<InvalidOperationException>(() => noTenant.GetAsync("sms-key"));

        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var providerA = new TenantSecretProvider(new TenantContext(tenantA, null, "user-a"), fakeStore);
        var providerB = new TenantSecretProvider(new TenantContext(tenantB, null, "user-b"), fakeStore);

        await providerA.SetAsync("sms-key", "secret-a");
        (await providerA.GetAsync("sms-key")).ShouldBe("secret-a");
        (await providerB.GetAsync("sms-key")).ShouldBeNull();
    }

    [Fact]
    public async Task CachedSecretProvider_Caches_Hits_And_Evicts_On_Set_And_Delete()
    {
        var inner = new InMemorySecretProvider();
        var key = SecretKey.Platform("jwt-signing-key");
        await inner.SetSecretAsync(key, "v1");

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var cached = new CachedSecretProvider(inner, cache, Options.Create(new SecretsOptions { CacheTtlMinutes = 5 }));

        (await cached.GetSecretAsync(key)).ShouldBe("v1");
        (await cached.GetSecretAsync(key)).ShouldBe("v1");
        inner.ReadCount.ShouldBe(1);

        await cached.SetSecretAsync(key, "v2");
        (await cached.GetSecretAsync(key)).ShouldBe("v2");
        inner.ReadCount.ShouldBe(2);

        await cached.DeleteSecretAsync(key);
        (await cached.GetSecretAsync(key)).ShouldBeNull();
    }

    [Fact]
    public async Task ResilientSecretProvider_Retries_Transient_Failures_And_DoesNotRetry_Permanent_Errors()
    {
        var key = SecretKey.Platform("db-connection");
        var flaky = new FlakySecretProvider(transientFailuresBeforeSuccess: 2);
        var pipeline = ResilientSecretProvider.CreatePipeline(NullLogger.Instance, baseDelay: TimeSpan.FromMilliseconds(1));
        var resilient = new ResilientSecretProvider(flaky, pipeline);

        var result = await resilient.GetSecretAsync(key);
        result.ShouldBe("recovered-value");
        flaky.Attempts.ShouldBe(3);

        var permanent = new FlakySecretProvider(transientFailuresBeforeSuccess: 0, throwPermanent: true);
        var resilientPerm = new ResilientSecretProvider(permanent, pipeline);
        await Should.ThrowAsync<SecretProviderException>(() => resilientPerm.GetSecretAsync(key));
        permanent.Attempts.ShouldBe(1);
    }

    [Fact]
    public void SecretsOptionsValidator_And_ObservabilityOptions_Catch_Invalid_Configuration()
    {
        SecretsOptionsValidator.GetErrors(new SecretsOptions { Provider = "InvalidStore" }).ShouldNotBeEmpty();
        SecretsOptionsValidator.GetErrors(new SecretsOptions { Provider = "AzureKeyVault", AzureKeyVault = new() { VaultUri = "http://insecure" } }).ShouldNotBeEmpty();
        SecretsOptionsValidator.GetErrors(new SecretsOptions { Provider = null }).ShouldBeEmpty();

        new ObservabilityOptions { TraceSampleRatio = 2.0 }.GetErrors().ShouldNotBeEmpty();
        new ObservabilityOptions { OtlpEndpoint = "ftp://bad" }.GetErrors().ShouldNotBeEmpty();
        new ObservabilityOptions { ServiceName = "SchoolOS.Local.Api", OtlpEndpoint = "http://localhost:4317", TraceSampleRatio = 0.5 }.GetErrors().ShouldBeEmpty();
    }

    [Fact]
    public async Task ChangeAuditInterceptor_Captures_Added_And_Modified_Diffs_Atomically()
    {
        var tenantId = Guid.CreateVersion7();
        var ctx = new TenantContext(tenantId, null, "auditor-1");
        var interceptor = new ChangeAuditInterceptor(ctx);

        var options = new DbContextOptionsBuilder<AuditTestDbContext>()
            .UseInMemoryDatabase($"audit-test-{Guid.NewGuid()}")
            .AddInterceptors(interceptor)
            .Options;

        await using var db = new AuditTestDbContext(options);
        var entity = new AuditedSampleEntity(Guid.CreateVersion7(), tenantId, "Starter", 2500m);
        db.Samples.Add(entity);
        await db.SaveChangesAsync();

        entity.Rename("Growth", 4500m);
        await db.SaveChangesAsync();

        var entries = await db.ChangeAuditEntries.OrderBy(e => e.OccurredAtUtc).ToListAsync();
        entries.Count.ShouldBe(2);
        entries[0].Action.ShouldBe(ChangeAuditAction.Added);
        entries[0].TenantId.ShouldBe(tenantId);
        entries[0].ActorUserId.ShouldBe("auditor-1");
        entries[0].ChangesJson.ShouldContain("Starter");

        entries[1].Action.ShouldBe(ChangeAuditAction.Modified);
        entries[1].ChangesJson.ShouldContain("\"old\":\"Starter\"");
        entries[1].ChangesJson.ShouldContain("\"new\":\"Growth\"");
        entries[1].ChangesJson.ShouldContain("\"old\":\"2500\"");
        entries[1].ChangesJson.ShouldContain("\"new\":\"4500\"");
    }

    private sealed class InMemorySecretProvider : ISecretProvider
    {
        private readonly Dictionary<string, string> _data = new(StringComparer.Ordinal);
        public int ReadCount { get; private set; }

        public Task<string?> GetSecretAsync(SecretKey key, CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return Task.FromResult(_data.TryGetValue(key.Path, out var value) ? value : null);
        }

        public Task SetSecretAsync(SecretKey key, string value, CancellationToken cancellationToken = default)
        {
            _data[key.Path] = value;
            return Task.CompletedTask;
        }

        public Task DeleteSecretAsync(SecretKey key, CancellationToken cancellationToken = default)
        {
            _data.Remove(key.Path);
            return Task.CompletedTask;
        }
    }

    private sealed class FlakySecretProvider(int transientFailuresBeforeSuccess, bool throwPermanent = false) : ISecretProvider
    {
        public int Attempts { get; private set; }

        public Task<string?> GetSecretAsync(SecretKey key, CancellationToken cancellationToken = default)
        {
            Attempts++;
            if (throwPermanent)
            {
                throw new SecretProviderException("get", key, isTransient: false);
            }

            if (Attempts <= transientFailuresBeforeSuccess)
            {
                throw new SecretProviderException("get", key, isTransient: true);
            }

            return Task.FromResult<string?>("recovered-value");
        }

        public Task SetSecretAsync(SecretKey key, string value, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteSecretAsync(SecretKey key, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class AuditedSampleEntity : Entity<Guid>, ITenantScopedEntity, IChangeAuditedEntity
    {
        public Guid TenantId { get; private set; }
        public string Name { get; private set; }
        public decimal Price { get; private set; }

        public AuditedSampleEntity(Guid id, Guid tenantId, string name, decimal price) : base(id)
        {
            TenantId = tenantId;
            Name = name;
            Price = price;
        }

        public void Rename(string name, decimal price)
        {
            Name = name;
            Price = price;
        }
    }

    private sealed class AuditTestDbContext(DbContextOptions<AuditTestDbContext> options) : DbContext(options)
    {
        public DbSet<AuditedSampleEntity> Samples => Set<AuditedSampleEntity>();
        public DbSet<ChangeAuditEntry> ChangeAuditEntries => Set<ChangeAuditEntry>();
    }
}
