using DependencyInjection.AssemblyExtensions;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.ImplementationFactory;

public static class AssemblyExtensions
{
    public static IServiceCollection AddServiceImplementationFactoriesFromPath(this IServiceCollection services, Type serviceType, string path)
    {
        var implementationFactories = typeof(IServiceImplementationFactory).GetImplementationsForInterface(path).
            Where(t => t.IsGenericType &&
                (t.GenericTypeArguments.Contains(serviceType) || t.GenericTypeArguments.Any(g => g.GetInterfaces().Contains(serviceType))));

        foreach (var implementationFactory in implementationFactories)
            services.AddServiceImplementationSingleton(serviceType, implementationFactory);

        return services;
    }
}
