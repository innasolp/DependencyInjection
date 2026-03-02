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
    
    public static IServiceCollection RegisterAllServiceImplementationFromPath(this IServiceCollection services, string path)
    {
        var assemblies = AssemblyHelper.GetAllAssembliesFromPath(path);

        foreach (var assembly in assemblies)
        {
            var registrations = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Length > 0)
            .Select(t => new
            {
                Implementation = t,
                Services = t.GetInterfaces()
            });

            foreach (var reg in registrations)
            {
                foreach (var service in reg.Services)
                {
                    services.AddSingleton(service, reg.Implementation);
                }
            }
        }

        return services;
    }
}