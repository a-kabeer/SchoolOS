using Microsoft.Extensions.Hosting;

namespace SchoolOS.BuildingBlocks.Observability;

/// <summary>
/// Configuration section <c>Observability</c> for OpenTelemetry OTLP logs, metrics, and traces.
/// </summary>
public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";
    public const double DevelopmentSampleRatio = 1.0;
    public const double DefaultSampleRatio = 0.1;

    public bool Enabled { get; set; } = true;
    public string ServiceName { get; set; } = TelemetryConstants.ServiceName;
    public string? OtlpEndpoint { get; set; }
    public double? TraceSampleRatio { get; set; }
    public bool TenantIdAsMetricLabel { get; set; }

    public double SampleRatioFor(IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return TraceSampleRatio ?? (environment.IsDevelopment() ? DevelopmentSampleRatio : DefaultSampleRatio);
    }

    public IReadOnlyList<string> GetErrors()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ServiceName))
        {
            errors.Add($"{SectionName}:ServiceName is required.");
        }

        if (TraceSampleRatio is < 0 or > 1 || (TraceSampleRatio is { } ratio && double.IsNaN(ratio)))
        {
            errors.Add($"{SectionName}:TraceSampleRatio must be between 0 and 1.");
        }

        if (!string.IsNullOrWhiteSpace(OtlpEndpoint)
            && (!Uri.TryCreate(OtlpEndpoint, UriKind.Absolute, out var endpoint)
                || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps)))
        {
            errors.Add($"{SectionName}:OtlpEndpoint must be an absolute http or https URL.");
        }

        return errors;
    }
}
