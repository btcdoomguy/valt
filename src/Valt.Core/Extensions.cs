using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("Valt.Tests")]

namespace Valt.Core;

public static class Extensions
{
    public static IServiceCollection AddValtCore(this IServiceCollection services)
    {
        //services implementations

        return services;
    }
}