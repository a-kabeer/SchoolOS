using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SchoolOS.BuildingBlocks.Web;

/// <summary>
/// Modular Minimal API endpoint group contract (ported from Education-Portal).
/// Implementations are discovered via <see cref="EndpointGroupExtensions.MapEndpointGroups"/>.
/// </summary>
public interface IEndpointGroup
{
    static virtual string? RoutePrefix => null;
    static abstract void Map(RouteGroupBuilder groupBuilder);
}

public static class EndpointGroupExtensions
{
    public static WebApplication MapEndpointGroups(this WebApplication app, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(assembly);

        var endpointGroupTypes = assembly.GetExportedTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                     && t.IsAssignableTo(typeof(IEndpointGroup)));

        foreach (var type in endpointGroupTypes)
        {
            var groupName = type.Name;
            var routePrefix = type.GetProperty(nameof(IEndpointGroup.RoutePrefix))
                ?.GetValue(null) as string ?? $"/api/v1/{groupName.ToLowerInvariant()}";
            var group = app.MapGroup(routePrefix).WithTags(groupName);

            type.GetMethod(nameof(IEndpointGroup.Map))!.Invoke(null, [group]);
        }

        return app;
    }

    public static RouteHandlerBuilder MapNamedGet(this IEndpointRouteBuilder builder, Delegate handler, [StringSyntax("Route")] string pattern = "") =>
        builder.MapGet(pattern, EnsureNamed(handler)).WithName(handler.Method.Name);

    public static RouteHandlerBuilder MapNamedPost(this IEndpointRouteBuilder builder, Delegate handler, [StringSyntax("Route")] string pattern = "") =>
        builder.MapPost(pattern, EnsureNamed(handler)).WithName(handler.Method.Name);

    public static RouteHandlerBuilder MapNamedPut(this IEndpointRouteBuilder builder, Delegate handler, [StringSyntax("Route")] string pattern) =>
        builder.MapPut(pattern, EnsureNamed(handler)).WithName(handler.Method.Name);

    public static RouteHandlerBuilder MapNamedPatch(this IEndpointRouteBuilder builder, Delegate handler, [StringSyntax("Route")] string pattern) =>
        builder.MapPatch(pattern, EnsureNamed(handler)).WithName(handler.Method.Name);

    public static RouteHandlerBuilder MapNamedDelete(this IEndpointRouteBuilder builder, Delegate handler, [StringSyntax("Route")] string pattern) =>
        builder.MapDelete(pattern, EnsureNamed(handler)).WithName(handler.Method.Name);

    private static Delegate EnsureNamed(Delegate handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (handler.Method.Name.Contains('<', StringComparison.Ordinal))
        {
            throw new ArgumentException("Endpoint handlers must be named methods so OpenAPI operationId generation is deterministic.", nameof(handler));
        }

        return handler;
    }
}
