using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.Extensions;

public static class ServiceProviderExtensions
{
    public static T? GetSingletonKeyedOrDefault<T>(this IServiceProvider serviceProvider, object? key)
    {
        return key != null ? serviceProvider.GetKeyedService<T>(key) : serviceProvider.GetService<T>();
    }

    public static object? GetSingletonKeyedOrDefault(this IServiceProvider serviceProvider, Type serviceType, object? key)
    {
        return key != null ? serviceProvider.GetKeyedServices(serviceType, key) : serviceProvider.GetService(serviceType);
    }
}
