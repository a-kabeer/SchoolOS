using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SchoolOS.BuildingBlocks.Web;

/// <summary>
/// Adds hardened security headers to every HTTP response (ported from Education-Portal SEC-WEB-04 and tuned for React/Vite).
/// </summary>
public static class SecurityHeaders
{
    private const string ProductionContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "img-src 'self' data:; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "connect-src 'self'; " +
        "worker-src 'self' blob:; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'";

    private const string DevelopmentContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net; " +
        "connect-src 'self' http://localhost:5173 ws://localhost:5173; " +
        "worker-src 'self' blob:; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'";

    private const string PermissionsPolicy =
        "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";

    public static IApplicationBuilder UseSchoolOSSecurityHeaders(this IApplicationBuilder app, bool isDevelopment)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (context, next) =>
        {
            context.Response.OnStarting(static state =>
            {
                var (httpContext, isDev) = ((HttpContext, bool))state;
                var headers = httpContext.Response.Headers;

                headers["Content-Security-Policy"] = isDev
                    ? DevelopmentContentSecurityPolicy
                    : ProductionContentSecurityPolicy;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Permissions-Policy"] = PermissionsPolicy;

                if (httpContext.Request.Path.StartsWithSegments("/api"))
                {
                    headers["Cache-Control"] = "no-store";
                }

                return Task.CompletedTask;
            }, (context, isDevelopment));

            await next();
        });
    }
}
