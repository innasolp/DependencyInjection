using DependencyInjection.ImplementationFactory;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.AssemblyExtensions;

public static class AssemblyExtensions
{
    public static IServiceCollection AddServiceImplementationsFromPath(this IServiceCollection services, Type serviceType, string path)
    {
        var implementationTypes = serviceType.GetImplementationsForInterface(path);
        foreach (var implementationType in implementationTypes)
            services.AddSingleton(serviceType, implementationType);

        return services;
    }

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
