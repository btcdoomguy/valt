using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Kernel.Validation;

[assembly: InternalsVisibleTo("Valt.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Valt.App;

public static class Extensions
{
    public static IServiceCollection AddValtApp(this IServiceCollection services)
    {
        // Register dispatchers
        services.AddSingleton<ICommandDispatcher, CommandDispatcher>();
        services.AddSingleton<IQueryDispatcher, QueryDispatcher>();

        // Auto-register command handlers (transient - new instance per operation)
        services.Scan(scan => scan
            .FromAssemblyOf<AssemblyMarker>()
            .AddClasses(classes => classes.Where(type =>
                type.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))), publicOnly: false)
            .AsImplementedInterfaces()
            .WithTransientLifetime());

        // Auto-register query handlers (singleton - stateless reads)
        services.Scan(scan => scan
            .FromAssemblyOf<AssemblyMarker>()
            .AddClasses(classes => classes.Where(type =>
                type.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))), publicOnly: false)
            .AsImplementedInterfaces()
            .WithSingletonLifetime());

        // Auto-register validators (transient)
        services.Scan(scan => scan
            .FromAssemblyOf<AssemblyMarker>()
            .AddClasses(classes => classes.Where(type =>
                type.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IValidator<>))), publicOnly: false)
            .AsImplementedInterfaces()
            .WithTransientLifetime());

        return services;
    }

    /// <summary>
    /// Marker class for assembly scanning.
    /// </summary>
    public sealed class AssemblyMarker;
}
