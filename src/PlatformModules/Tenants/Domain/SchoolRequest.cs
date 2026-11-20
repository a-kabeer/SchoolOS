using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.SharedKernel;

namespace SchoolOS.PlatformModules.Tenants.Domain;

/// <summary>
/// Data residency region for a tenant's primary storage and backups.
/// </summary>
public enum DataRegion
{
    SouthAsia = 1,
    MiddleEast = 2,
    NorthAmerica = 3
}

/// <summary>
/// Lifecycle status of a public school onboarding request.
/// </summary>
public enum SchoolRequestStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

/// <summary>
/// Publicly submitted school onboarding inquiry managed in the Cloud Control Plane (<c>platform</c> schema).
/// Approving creates an operational <see cref="Tenant"/> and triggers trial provisioning.
/// </summary>
public sealed class SchoolRequest : Entity<Guid>, IChangeAuditedEntity
{
    public const int SchoolNameMaxLength = 200;
    public const int ContactNameMaxLength = 150;
    public const int ContactEmailMaxLength = 254;
    public const int DecisionNotesMaxLength = 500;

    private SchoolRequest()
    {
        SchoolName = string.Empty;
        ContactName = string.Empty;
        ContactEmail = string.Empty;
    }

    private SchoolRequest(
        string schoolName,
        string contactName,
        string contactEmail,
        DateTimeOffset submittedAtUtc) : this()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schoolName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contactName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contactEmail);

        var trimmedSchool = schoolName.Trim();
        var trimmedContact = contactName.Trim();
        var normalizedEmail = contactEmail.Trim().ToLowerInvariant();

        if (trimmedSchool.Length > SchoolNameMaxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(schoolName));
        }

        if (trimmedContact.Length > ContactNameMaxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(contactName));
        }

        if (!ContactRules.IsValidEmail(normalizedEmail) || normalizedEmail.Length > ContactEmailMaxLength)
        {
            throw new ArgumentException("A valid contact email address is required.", nameof(contactEmail));
        }

        Id = Guid.CreateVersion7();
        SchoolName = trimmedSchool;
        ContactName = trimmedContact;
        ContactEmail = normalizedEmail;
        Status = SchoolRequestStatus.Pending;
        SubmittedAtUtc = submittedAtUtc;
    }

    public string SchoolName { get; private set; }
    public string ContactName { get; private set; }
    public string ContactEmail { get; private set; }
    public SchoolRequestStatus Status { get; private set; }
    public DateTimeOffset SubmittedAtUtc { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public string? ReviewedBy { get; private set; }
    public string? DecisionNotes { get; private set; }
    public Guid? ApprovedTenantId { get; private set; }

    public static SchoolRequest Submit(
        string schoolName,
        string contactName,
        string contactEmail,
        DateTimeOffset submittedAtUtc)
        => new(schoolName, contactName, contactEmail, submittedAtUtc);

    public void Approve(Guid tenantId, string reviewedBy, DateTimeOffset reviewedAtUtc, string? notes = null)
    {
        if (Status != SchoolRequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending school requests can be approved.");
        }

        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Approved tenant ID cannot be empty.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reviewedBy);
        var trimmedNotes = notes?.Trim();
        if (trimmedNotes is { Length: > DecisionNotesMaxLength })
        {
            throw new ArgumentOutOfRangeException(nameof(notes));
        }

        Status = SchoolRequestStatus.Approved;
        ApprovedTenantId = tenantId;
        ReviewedBy = reviewedBy.Trim();
        ReviewedAtUtc = reviewedAtUtc;
        DecisionNotes = trimmedNotes;
    }

    public void Reject(string reviewedBy, DateTimeOffset reviewedAtUtc, string? notes = null)
    {
        if (Status != SchoolRequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending school requests can be rejected.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reviewedBy);
        var trimmedNotes = notes?.Trim();
        if (trimmedNotes is { Length: > DecisionNotesMaxLength })
        {
            throw new ArgumentOutOfRangeException(nameof(notes));
        }

        Status = SchoolRequestStatus.Rejected;
        ReviewedBy = reviewedBy.Trim();
        ReviewedAtUtc = reviewedAtUtc;
        DecisionNotes = trimmedNotes;
    }
}

/// <summary>
/// Cross-module contract implemented by Subscriptions to start a 30-day free trial upon tenant approval.
/// </summary>
public interface ITenantTrialProvisioner
{
    Task<Guid?> StartTrialAsync(
        Guid tenantId,
        string countryCode,
        CancellationToken cancellationToken = default);
}
