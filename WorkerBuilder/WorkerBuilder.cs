using DependencyInjection.ImplementationFactory;
using DependencyInjection.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace DependencyInjection.WorkerBuilder;

public abstract class WorkerBuilder(IServiceCollection services)
{
    protected IServiceCollection Services { get; } = services;

    protected Dictionary<KeyValuePair<string, Type>, Type[]> PathInterfaceTypes { get; } = [];

    protected Dictionary<KeyValuePair<string, string>, Type> PathServiceTypeNameTypes { get; } = [];

    protected Type? LoadServiceFromPath(string path, Type serviceType)
    {
        Type? implementationType = null;

        var key = new KeyValuePair<string, Type>(path, serviceType);

        if (PathInterfaceTypes.TryGetValue(key, out Type[]? types))
            implementationType = types?.FirstOrDefault();
        else
        {
            var implementations = serviceType.GetAssemblyImplmentationsForInterface(path).SelectMany(at => at.Value).ToArray();
            if (implementations.Length != 0)
                PathInterfaceTypes.Add(key, implementations);
            implementationType = implementations.FirstOrDefault();
        }

        return implementationType;
    }

    protected Type? LoadServiceFromPath(string path, string serviceTypeName)
    {
        var key = new KeyValuePair<string, string>(path, serviceTypeName);

        if (!PathServiceTypeNameTypes.TryGetValue(key, out Type? implementationType))
        {
            implementationType = path.GetServiceTypeFromAssembly(serviceTypeName);

            if (implementationType != null)
                PathServiceTypeNameTypes.Add(key, implementationType);
        }

        return implementationType;
    }

    protected IServiceCollection AddService(Type serviceType, string assemblyPath, out Type? implementationType)
    {
        implementationType = LoadServiceFromPath(assemblyPath, serviceType);

        return implementationType != null ?
             Services.AddSingleton(serviceType, implementationType) : Services;
    }

    protected IServiceCollection AddService(string serviceTypeName, string assemblyPath, out Type? implementationType)
    {
        implementationType = LoadServiceFromPath(assemblyPath, serviceTypeName);

        var serviceType = implementationType?.GetInterfaces().FirstOrDefault(i => i.Name == serviceTypeName)
            ?? implementationType?.GetInterfaces().FirstOrDefault(i => i.Name.Contains(serviceTypeName));

        return implementationType != null && serviceType != null ?
             Services.AddSingleton(serviceType, implementationType) : Services;
    }

    protected IServiceCollection AddKeyedService(Type serviceType, string assemblyPath, object? key, out Type? implementationType)
    {
        implementationType = LoadServiceFromPath(assemblyPath, serviceType);

        return implementationType != null ? Services.AddKeyedSingleton(serviceType, key, implementationType) : Services;
    }

    protected IServiceCollection AddKeyedService(string serviceTypeName, string assemblyPath, object? key, out Type? implementationType)
    {
        implementationType = LoadServiceFromPath(assemblyPath, serviceTypeName);

        var serviceType = implementationType?.GetInterfaces().FirstOrDefault(i => i.Name == serviceTypeName)
            ?? implementationType?.GetInterfaces().FirstOrDefault(i => i.Name.Contains(serviceTypeName));

        return implementationType != null && serviceType != null ?
             Services.AddKeyedSingleton(serviceType, implementationType, key)
            : Services;
    }

    protected IServiceCollection AddServiceByImplementationFactory(Type serviceType, string serviceProviderPath)
    {
        var serviceImplementationFactoryType = LoadServiceFromPath(serviceProviderPath, typeof(IServiceImplementationFactory));

        if (serviceImplementationFactoryType == null)
            return Services;

        return Services.AddServiceImplementationSingleton(serviceType, serviceImplementationFactoryType);
    }

    protected IServiceCollection AddServiceByImplementationFactory(string serviceTypeName, string serviceProviderPath, string assemblyPath)
    {
        return AddServiceByImplementationFactory(assemblyPath.GetInterfaceTypeFromAssembly(serviceTypeName), serviceProviderPath);
    }

    protected IServiceCollection AddServiceByKeyedImplementationFactory(Type serviceType, string serviceProviderPath, object? key)
    {
        var serviceImplementationFactoryType = LoadServiceFromPath(serviceProviderPath, typeof(IServiceImplementationFactory));

        if (serviceImplementationFactoryType == null)
            return Services;

        return Services.AddServiceImplementationSingletonByKey(serviceType, serviceImplementationFactoryType, key);
    }

    protected IServiceCollection AddKeyedServiceByImplementationFactory(string serviceTypeName, string serviceProviderPath, string assemblyPath, object? key)
    {
        return AddKeyedServiceByImplementationFactory(assemblyPath.GetInterfaceTypeFromAssembly(serviceTypeName), serviceProviderPath, key);
    }

    protected IServiceCollection AddKeyedServiceByImplementationFactory(Type serviceType, string serviceProviderPath, object? key)
    {
        var serviceImplementationFactoryType = LoadServiceFromPath(serviceProviderPath, typeof(IServiceImplementationFactory));

        if (serviceImplementationFactoryType == null)
            return Services;

        return Services.AddKeyedServiceImplementationSingleton(serviceType, serviceImplementationFactoryType, key);
    }

    protected IServiceCollection AddServiceByKeyedImplementationFactory(string serviceTypeName, string serviceProviderPath, string assemblyPath, object? key)
    {
        return AddServiceByKeyedImplementationFactory(assemblyPath.GetInterfaceTypeFromAssembly(serviceTypeName), serviceProviderPath, key);
    }

    protected IServiceCollection AddServiceValueFromJson(Type serviceType, string assemblyPath, string json)
    {
        var implementationType = LoadServiceFromPath(assemblyPath, serviceType);

        var value = JsonSerializer.Deserialize(json, implementationType ?? serviceType);

        return value != null ? Services.AddSingleton(serviceType, value) : Services;
    }

    protected IServiceCollection AddServiceValueFromJson(string serviceTypeName, string assemblyPath, string json)
    {
        var implementationType = LoadServiceFromPath(assemblyPath, serviceTypeName);

        var serviceType = implementationType?.GetInterfaces().FirstOrDefault(i => i.Name == serviceTypeName)
            ?? implementationType?.GetInterfaces().FirstOrDefault(i => i.Name.Contains(serviceTypeName));

        var value = implementationType != null ? JsonSerializer.Deserialize(json, implementationType) : null;

        return value != null ? Services.AddSingleton(serviceType ?? implementationType, value) : Services;
    }


    protected IServiceCollection AddKeyedServiceValueFromJson(string serviceTypeName, string assemblyPath, string json, object? key)
    {
        var implementationType = LoadServiceFromPath(assemblyPath, serviceTypeName);

        if (implementationType == null) return Services;

        var serviceType = implementationType?.GetInterfaces().FirstOrDefault(i => i.Name == serviceTypeName)
            ?? implementationType?.GetInterfaces().FirstOrDefault(i => i.Name.Contains(serviceTypeName));

        var value = JsonSerializer.Deserialize(json, implementationType);

        return value != null ? Services.AddKeyedSingleton(serviceType ?? implementationType, key, value) : Services;
    }

    protected IServiceCollection AddKeyedServiceValueFromJson(Type serviceType, string assemblyPath, string json, object? key)
    {
        var implementationType = LoadServiceFromPath(assemblyPath, serviceType);

        var value = JsonSerializer.Deserialize(json, implementationType ?? serviceType);

        return value != null ? Services.AddKeyedSingleton(serviceType, key, value) : Services;
    }

    protected IServiceCollection AddKeyedServiceValueFromJson(Type serviceType, string json, object? key)
    {
        var value = JsonSerializer.Deserialize(json, serviceType);

        return value != null ? Services.AddKeyedSingleton(serviceType, key, value) : Services;
    }

    protected virtual IServiceCollection AddServiceBySettings(Type serviceType, IServiceSettings serviceSettings, object? implementationKey)
    {
        if (!string.IsNullOrEmpty(serviceSettings.ServiceProviderPath))
        {
            return AddServiceByKeyedImplementationFactory(serviceType, serviceSettings.ServiceProviderPath, implementationKey);
        }
        else if (serviceSettings.Value != null && !string.IsNullOrEmpty(serviceSettings.AssemblyPath))
        {
            return AddServiceValueFromJson(serviceType, serviceSettings.AssemblyPath, serviceSettings.Value);
        }

        return AddService(serviceType, serviceSettings.AssemblyPath, out Type? implementationType);
    }

    protected virtual IServiceCollection AddKeyedServiceBySettings(Type serviceType, IServiceSettings serviceSettings, object? key)
    {
        if (!string.IsNullOrEmpty(serviceSettings.ServiceProviderPath))
        {
            return AddKeyedServiceByImplementationFactory(serviceType, serviceSettings.ServiceProviderPath, key);
        }
        else if (serviceSettings.Value != null)
        {
            return !string.IsNullOrEmpty(serviceSettings.AssemblyPath) 
                ? AddKeyedServiceValueFromJson(serviceType, serviceSettings.AssemblyPath, serviceSettings.Value, key)
                : AddKeyedServiceValueFromJson(serviceType, serviceSettings.Value, key);
        }

        return AddKeyedService(serviceType, serviceSettings.AssemblyPath, key, out Type? implementationType);
    }

    protected virtual IServiceCollection AddServiceBySettings(IServiceSettings serviceSettings, object? implementationKey)
    {
        var assemblyPath = serviceSettings.AssemblyPath;

        if (!string.IsNullOrEmpty(serviceSettings.ServiceProviderPath))
        {
            return AddServiceByKeyedImplementationFactory(serviceSettings.ServiceTypeName,
                serviceSettings.ServiceProviderPath,
                assemblyPath,
                implementationKey);
        }
        else if (serviceSettings.Value != null && !string.IsNullOrEmpty(serviceSettings.AssemblyPath))
        {
            return AddServiceValueFromJson(serviceSettings.ServiceTypeName, assemblyPath, serviceSettings.Value);
        }

        return AddService(serviceSettings.ServiceTypeName, serviceSettings.AssemblyPath, out Type? implementationType);
    }

    protected virtual IServiceCollection AddKeyedServiceBySettings(IServiceSettings serviceSettings, object? key)
    {
        var assemblyPath = serviceSettings.AssemblyPath;

        if (!string.IsNullOrEmpty(serviceSettings.ServiceProviderPath))
        {
            return AddKeyedServiceByImplementationFactory(serviceSettings.ServiceTypeName, serviceSettings.ServiceProviderPath, assemblyPath, key);
        }
        else if (serviceSettings.Value != null && !string.IsNullOrEmpty(serviceSettings.AssemblyPath))
        {
            return AddKeyedServiceValueFromJson(serviceSettings.ServiceTypeName, assemblyPath, serviceSettings.Value, key);
        }

        return AddKeyedService(serviceSettings.ServiceTypeName, serviceSettings.AssemblyPath, key, out Type? implementationType);
    }
}
