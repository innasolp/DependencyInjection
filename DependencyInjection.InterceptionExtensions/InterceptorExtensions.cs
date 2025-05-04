using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.Interception;

public static class InterceptorExtensions
{
    public static IServiceCollection Intercept(this IServiceCollection services, Type serviceType, Type interceptorType)
    {
        var targetServices = services.Where(s => s.ServiceType == serviceType).ToList();

        if (targetServices.Count == 0)
            return services;


        foreach (var service in targetServices)
        {
            var ix = services.IndexOf(service);

            if (service.ImplementationFactory == null)
            {
                services[ix] = new ServiceDescriptor(
                    serviceType,
                    provider => ActivatorUtilities.CreateInstance(
                        provider,
                        interceptorType,
                        ActivatorUtilities.GetServiceOrCreateInstance(provider, service.ImplementationType)),
                    service.Lifetime);
            }
            else
            {
                // register descriptor for the service with factory
                services[ix] = new ServiceDescriptor(
                    serviceType,
                    provider => ActivatorUtilities.CreateInstance(
                        provider,
                        interceptorType,
                        service.ImplementationFactory.Invoke(provider)),
                    service.Lifetime);
            }
        }

        return services;
    }

    public static IServiceCollection Intercept<TService, TInterceptor>(this IServiceCollection services)
    {
        return services.Intercept(typeof(TService), typeof(TInterceptor));
    }
}
