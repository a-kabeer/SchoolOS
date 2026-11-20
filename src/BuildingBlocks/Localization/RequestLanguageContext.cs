using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace SchoolOS.BuildingBlocks.Localization;

public interface IRequestLanguageContext
{
    string Language { get; }
    bool IsRtl { get; }
    CultureInfo Culture { get; }
}

public sealed class RequestLanguageContext : IRequestLanguageContext
{
    public string Language { get; private set; } = SupportedLanguage.DefaultCode;
    public bool IsRtl => SupportedLanguage.IsRtl(Language);
    public CultureInfo Culture => SupportedLanguage.ToCultureInfo(Language);

    public void SetLanguage(string? languageCode)
    {
        Language = SupportedLanguage.Normalize(languageCode);
    }
}

public sealed class RequestLanguageMiddleware
{
    public const string LanguageHeaderName = "X-SchoolOS-Lang";
    private readonly RequestDelegate _next;

    public RequestLanguageMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRequestLanguageContext languageContext)
    {
        var chosen = ResolveLanguage(context);
        if (languageContext is RequestLanguageContext mutable)
        {
            mutable.SetLanguage(chosen);
        }

        var culture = languageContext.Culture;
        var prevCulture = CultureInfo.CurrentCulture;
        var prevUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            context.Response.Headers["Content-Language"] = languageContext.Language;
            await _next(context);
        }
        finally
        {
            CultureInfo.CurrentCulture = prevCulture;
            CultureInfo.CurrentUICulture = prevUiCulture;
        }
    }

    public static string ResolveLanguage(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(LanguageHeaderName, out var customHeader))
        {
            var normalized = SupportedLanguage.Normalize(customHeader.FirstOrDefault(), fallbackToDefault: false);
            if (!string.IsNullOrEmpty(normalized))
            {
                return normalized;
            }
        }

        if (context.Request.Headers.TryGetValue("Accept-Language", out var acceptHeader))
        {
            var raw = acceptHeader.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(raw))
            {
                foreach (var segment in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var langPart = segment.Split(';', 2)[0].Trim();
                    var normalized = SupportedLanguage.Normalize(langPart, fallbackToDefault: false);
                    if (!string.IsNullOrEmpty(normalized))
                    {
                        return normalized;
                    }
                }
            }
        }

        return SupportedLanguage.DefaultCode;
    }
}

public static class LocalizationDependencyInjection
{
    public static IServiceCollection AddLocalizationBuildingBlocks(this IServiceCollection services)
    {
        services.AddScoped<IRequestLanguageContext, RequestLanguageContext>();
        return services;
    }

    public static IApplicationBuilder UseRequestLocalizationContext(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestLanguageMiddleware>();
}
