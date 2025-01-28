using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace DependencyInjection.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKeyedSingletonIfNotEmpty<T>(this IServiceCollection services, T service, object? key)
        where T : class
    {
        if (key != null)
            return services.AddKeyedSingleton(key, service);
        else
            return services.AddSingleton(service);
    }

    public static IServiceCollection AddKeyedSingletonIfNotEmpty(this IServiceCollection services, Type @interface, Type serviceType, object? key)
    {
        if (key != null)
            return services.AddKeyedSingleton(@interface, key, serviceType);
        else
            return services.AddSingleton(@interface, serviceType);
    }

    public static IServiceCollection AddKeyedSingletonIfNotEmpty(this IServiceCollection services, Type @interface, object implementation, object? key)
    {
        if (key != null)
            return services.AddKeyedSingleton(@interface, key, implementation);
        else
            return services.AddSingleton(@interface, implementation);
    }

    public static IServiceCollection WrappingIntercept<TService, TInterceptor>(this IServiceCollection services, List<object>? parameters = null)
    {
        var targetServices = services.Where(s => s.ServiceType == typeof(TService)).ToList();

        if (targetServices.Count == 0)        
            return services;
        

        foreach (var service in targetServices)
        {
            var ix = services.IndexOf(service);

            if (service.ImplementationFactory == null)
            {
                services[ix] = new ServiceDescriptor(
                    typeof(TService),
                    provider => ActivatorUtilities.CreateInstance(
                        provider,
                        typeof(TInterceptor),
                        parameters != null 
                            ? new List<object> { ActivatorUtilities.GetServiceOrCreateInstance(provider, service.ImplementationType) }.Union(parameters) 
                            : ActivatorUtilities.GetServiceOrCreateInstance(provider, service.ImplementationType)),
                    service.Lifetime);
            }
            else
            {
                // register descriptor for the service with factory
                services[ix] = new ServiceDescriptor(
                    typeof(TService),
                    provider => ActivatorUtilities.CreateInstance(
                        provider,
                        typeof(TInterceptor),
                         parameters != null
                            ? new List<object> { service.ImplementationFactory.Invoke(provider) }.Union(parameters)
                         : service.ImplementationFactory.Invoke(provider)),
                    service.Lifetime);
            }
        }

        return services;
    }

    public static IServiceCollection WrappingIntercept(this IServiceCollection services, Type serviceType, Type interceptorType, List<object>? parameters = null)
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
                        parameters != null
                            ? new List<object> { ActivatorUtilities.GetServiceOrCreateInstance(provider, service.ImplementationType) }.Union(parameters)
                            : ActivatorUtilities.GetServiceOrCreateInstance(provider, service.ImplementationType)),
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
                         parameters != null
                            ? new List<object> { service.ImplementationFactory.Invoke(provider) }.Union(parameters)
                         : service.ImplementationFactory.Invoke(provider)),
                    service.Lifetime);
            }
        }

        return services;
    }

    public static IServiceCollection AddWrappingIntercept(this IServiceCollection services, Type serviceType, Type interceptorType, object? key, List<object>? parameters = null)
    {
        var targetServices = services.Where(s => s.ServiceType == serviceType).ToList();

        if (targetServices.Count == 0)
            return services;


        foreach (var service in targetServices)
        {
            var wrappingServiceDescriptor = new ServiceDescriptor(
                    serviceType,
                    key,
                    (provider, key) => ActivatorUtilities.CreateInstance(
                        provider,
                        interceptorType,
                        service.InvokeWrappingImplementationFactory(provider, key, parameters)),
                        service.Lifetime);

            services.Add(wrappingServiceDescriptor);


            //if (service.ImplementationFactory == null && service.KeyedImplementationFactory == null)
            //{
            //    services[ix] = new ServiceDescriptor(
            //        serviceType,
            //        key,
            //        (provider, key) => ActivatorUtilities.CreateInstance(
            //            provider,
            //            interceptorType,
            //            parameters != null
            //                ? new List<object> { ActivatorUtilities.GetServiceOrCreateInstance(provider, service.ImplementationType) }.Union(parameters)
            //                : ActivatorUtilities.GetServiceOrCreateInstance(provider, service.ImplementationType)),
            //        service.Lifetime);
            //}
            //else
            //{
            //    // register descriptor for the service with factory
            //    services[ix] = new ServiceDescriptor(
            //        serviceType,
            //        key,
            //        (provider,key) => ActivatorUtilities.CreateInstance(
            //            provider,
            //            interceptorType,
            //             parameters != null
            //                ? new List<object> { service.ImplementationFactory.Invoke(provider) }.Union(parameters)
            //             : service.ImplementationFactory.Invoke(provider)),
            //        service.Lifetime);
            //}
        }

        return services;
    }

    private static object InvokeWrappingImplementationFactory(this ServiceDescriptor service, IServiceProvider provider, object? key = null, List<object>? parameters = null)
    {
        if(service.IsKeyedService)
        {
            if (service.KeyedImplementationFactory != null)
                return parameters != null
                            ? new List<object> { service.KeyedImplementationFactory.Invoke(provider, key) }.Union(parameters)
                         : service.KeyedImplementationFactory.Invoke(provider, key);
            else
                return parameters != null
                            ? new List<object> { ActivatorUtilities.GetServiceOrCreateInstance(provider, service.ImplementationType) }.Union(parameters)
                            : ActivatorUtilities.GetServiceOrCreateInstance(provider, service.ImplementationType);
        }
        else
        {
            if (service.ImplementationFactory != null)
                return parameters != null
                            ? new List<object> { service.ImplementationFactory.Invoke(provider) }.Union(parameters)
                         : service.ImplementationFactory.Invoke(provider);
            else
                return parameters != null
                            ? new List<object> { ActivatorUtilities.GetServiceOrCreateInstance(provider, service.ImplementationType) }.Union(parameters)
                            : ActivatorUtilities.GetServiceOrCreateInstance(provider, service.ImplementationType);
        }
    }
}
