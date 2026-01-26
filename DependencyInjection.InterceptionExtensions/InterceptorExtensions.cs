using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.InterceptionExtensions;

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


    public static IServiceCollection Intercept(this IServiceCollection services, Type serviceType, object instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if(!serviceType.IsAssignableFrom(instance.GetType()))
            throw new ArgumentException($"{serviceType.Name} is not assignable from {instance.GetType().Name}.");

        var targetServices = services.Where(s => s.ServiceType == serviceType).ToList();

        if (targetServices.Count == 0)
            return services;


        foreach (var service in targetServices)
        {
            var ix = services.IndexOf(service);

            services[ix] = new ServiceDescriptor(
                    serviceType,
                    provider => instance,
                    service.Lifetime);
        }

        return services;
    }

    public static IServiceCollection Intercept(this IServiceCollection services, Type serviceType, Func<object, object> interceptInstance)
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
                    provider =>
                    {
                        var initialInstance = service.ImplementationType != null
                        ? ActivatorUtilities.CreateInstance(
                            provider,
                            service.ImplementationType)
                         : service.ImplementationInstance != null ? ActivatorUtilities.CreateInstance(
                            provider,
                            service.ImplementationInstance.GetType())
                         : throw new ArgumentException($"no any implementations for service {service.GetType().Name}");
                        return interceptInstance(initialInstance);
                    },
                    service.Lifetime);
            }
            else
            {
                services[ix] = new ServiceDescriptor(
                    serviceType,
                    provider =>
                    {
                        var initialInstance = service.ImplementationFactory.Invoke(provider);
                        return interceptInstance(initialInstance);
                    },
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