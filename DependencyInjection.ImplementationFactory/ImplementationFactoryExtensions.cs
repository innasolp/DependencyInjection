using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.ImplementationFactory;

public static class ImplementationFactoryExtensions
{
    private static IServiceImplementationFactory GetIfAvailable(this IServiceProvider serviceProvider, Type serviceImplementationFactoryType)
    {
        var serviceImplementationObj = serviceProvider.GetService(serviceImplementationFactoryType)
            ?? throw new InvalidOperationException($"Service implementation factory {serviceImplementationFactoryType.Name} not registered");
        
        if (serviceImplementationObj is IServiceImplementationFactory serviceImplementationFactory)
            return serviceImplementationFactory;

        throw new InvalidOperationException($"Type {serviceImplementationFactoryType.Name} is not IServiceImplementationFactory");
    }

    public static IServiceCollection AddServiceImplementationTransient(this IServiceCollection services, Type serviceType, Type serviceImplementationFactoryType)
    {
        services.AddTransient(serviceImplementationFactoryType);
        return services.AddTransient(serviceType, (serviceProvider) =>
        {
            var serviceImplementation = serviceProvider.GetIfAvailable(serviceImplementationFactoryType);
            
            return serviceImplementation.GetService(serviceProvider, serviceType);
        });
    }

    public static IServiceCollection AddServiceImplementationTransient(this IServiceCollection services, Type serviceType, Type serviceImplementationFactoryType, object? key)
    {
        services.AddTransient(serviceImplementationFactoryType);

        return services.AddTransient(serviceType, (serviceProvider) =>
        {
            var serviceImplementation = serviceProvider.GetIfAvailable(serviceImplementationFactoryType);

            return serviceImplementation.GetService(serviceProvider, serviceType, key);
        });
    }

    public static IServiceCollection AddKeyedServiceImplementationTransient(this IServiceCollection services, Type serviceType, Type serviceImplementationFactoryType, object? serviceKey)
    {
        services.AddTransient(serviceImplementationFactoryType);

        return services.AddKeyedTransient(serviceType, serviceKey, (serviceProvider, key) =>
        {
            var serviceImplementation = serviceProvider.GetIfAvailable(serviceImplementationFactoryType);

            return serviceImplementation.GetService(serviceProvider, serviceType, key);
        });
    }

    public static IServiceCollection AddServiceImplementationSingleton(this IServiceCollection services, Type serviceType, Type serviceImplementationFactoryType)
    {
        services.AddTransient(serviceImplementationFactoryType);

        return services.AddSingleton(serviceType, (serviceProvider) =>
        {
            var serviceImplementation = serviceProvider.GetIfAvailable(serviceImplementationFactoryType);

            return serviceImplementation.GetService(serviceProvider, serviceType);
        });
    }

    public static IServiceCollection AddServiceImplementationSingleton(this IServiceCollection services, Type serviceType, Type serviceImplementationFactoryType, object? key)
    {
        services.AddTransient(serviceImplementationFactoryType);

        return services.AddSingleton(serviceType, (serviceProvider) =>
        {
            var serviceImplementation = serviceProvider.GetIfAvailable(serviceImplementationFactoryType);

            return serviceImplementation.GetService(serviceProvider, serviceType, key);
        });
    }

    public static IServiceCollection AddKeyedServiceImplementationSingleton(this IServiceCollection services, Type serviceType, Type serviceImplementationFactoryType, object? serviceKey)
    {
        services.AddTransient(serviceImplementationFactoryType);

        return services.AddKeyedSingleton(serviceType, serviceKey, (serviceProvider, key) =>
        {
            var serviceImplementation = serviceProvider.GetIfAvailable(serviceImplementationFactoryType);

            return serviceImplementation.GetService(serviceProvider, serviceType, key);
        });
    }
}
