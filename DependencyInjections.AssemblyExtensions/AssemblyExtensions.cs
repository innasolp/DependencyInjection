using DependencyInjection.ImplementationFactory;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.AssemblyExtensions;

public static class AssemblyExtensions
{
    public static IServiceCollection AddServiceImplementationsFromPath(this IServiceCollection services, Type serviceType, string path)
    {
        var implementationTypes = serviceType.GetAssemblyImplmentationsForInterface(path);
        foreach (var implementationType in implementationTypes.SelectMany(i => i.Value))
            services.AddSingleton(serviceType, implementationType);

        return services;
    }

    public static IServiceCollection AddServiceImplementationFactoriesFromPath(this IServiceCollection services, Type serviceType, string path)
    {
        var implementationFactories = typeof(IServiceImplementationFactory).GetAssemblyImplmentationsForInterface(path).SelectMany(i => i.Value).
            Where(t => t.IsGenericType &&
                (t.GenericTypeArguments.Contains(serviceType) || t.GenericTypeArguments.Any(g => g.GetInterfaces().Contains(serviceType))));
        foreach (var implementationFactory in implementationFactories)
            services.AddServiceImplementationSingleton(serviceType, implementationFactory);

        return services;
    }
}
