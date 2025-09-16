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
}
