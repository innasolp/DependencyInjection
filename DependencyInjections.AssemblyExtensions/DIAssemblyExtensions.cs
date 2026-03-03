using DependencyInjection.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.AssemblyExtensions;

public static class DIAssemblyExtensions
{
    public static IServiceCollection AddServiceImplementationsFromPath(this IServiceCollection services, Type serviceType, string path)
    {
        var implementationTypes = serviceType.GetImplementationsForInterface(path);
        foreach (var implementationType in implementationTypes)
            services.AddSingleton(serviceType, implementationType);

        return services;
    }

    private static IServiceCollection RegisterServiceImplementationsFromPath(this IServiceCollection services, string path, Func<Type, bool> typeFilter)
    {
        var assemblies = AssemblyHelper.GetAllAssembliesFromPath(path);

        foreach (var assembly in assemblies)
        {
            var registrations = assembly.GetLoadableTypes().Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Length > 0 && typeFilter(t))
            .Select(t => new
            {
                Implementation = t,
                Services = t.GetInterfaces()
            });

            foreach (var reg in registrations)
            {
                foreach (var service in reg.Services)
                {
                    services.AddScoped(service, reg.Implementation);
                }
            }
        }

        return services;
    }

    public static IServiceCollection RegisterAllServiceImplementationsFromPath(this IServiceCollection services, string path)
    {
        return services.RegisterServiceImplementationsFromPath(path, (t) => true);
    }

    public static IServiceCollection RegisterServiceImplementationsFromPathInsteadDIIgnore(this IServiceCollection services, string path)
    {
        return services.RegisterServiceImplementationsFromPath(path, (t) => t.GetCustomAttributes(typeof(DIIgnoreAttribute), false).Length == 0);
    }

    public static IServiceCollection RegisterServiceImplementationsFromPathDILoad(this IServiceCollection services, string path)
    {
        return services.RegisterServiceImplementationsFromPath(path, (t) => t.GetCustomAttributes(typeof(DILoadAttribute), false).Length > 0);
    }
}