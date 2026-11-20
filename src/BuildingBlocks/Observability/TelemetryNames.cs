namespace SchoolOS.BuildingBlocks.Observability;

/// <summary>
/// Central registry of OpenTelemetry <see cref="System.Diagnostics.ActivitySource"/> and
/// <see cref="System.Diagnostics.Metrics.Meter"/> names and tag keys.
/// Never put passwords, tokens, secret values, request bodies, or personal data in tags, metric labels, or logs.
/// </summary>
public static class TelemetryNames
{
    public const string Application = "SchoolOS.Application";
    public const string Secrets = "SchoolOS.Secrets";
    public const string Npgsql = "Npgsql";

    public static readonly IReadOnlyList<string> ActivitySources = [Application, Secrets];
    public static readonly IReadOnlyList<string> Meters = [Application, Secrets];

    public static class Tags
    {
        public const string TenantId = "tenant.id";
        public const string CampusId = "campus.id";
    }
}
