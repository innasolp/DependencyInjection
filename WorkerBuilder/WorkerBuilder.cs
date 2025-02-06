using DependencyInjection.ImplementationFactory;
using DependencyInjection.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Text.Json;

namespace DependencyInjection.WorkerBuilder;

public abstract class WorkerBuilder(IHostApplicationBuilder builder)
{
    protected IHostApplicationBuilder Builder { get; } = builder;

    protected Dictionary<string, Assembly> ServiceAssemblies { get; } = [];

    protected Dictionary<KeyValuePair<Assembly, Type>, Type> AssemblyInterfaceTypes { get; } = [];

    protected Dictionary<KeyValuePair<Assembly, string>, Type> AssemblyNameTypes { get; } = [];

    protected Dictionary<KeyValuePair<string, Type>, Type[]> PathInterfaceTypes { get; } = [];

    protected Dictionary<KeyValuePair<string, string>, Type> PathServiceTypeNameTypes { get; } = [];

    protected Dictionary<string, IHttpClientBuilder> HttpClientBuilders { get; } = [];

    protected Assembly LoadAssembly(string assemblyPath)
    {
        Assembly assembly;
        if (!ServiceAssemblies.TryGetValue(assemblyPath, out Assembly? value))
        {
            assembly = Assembly.LoadFrom(assemblyPath);
            ServiceAssemblies.Add(assemblyPath, assembly);
        }
        else
            assembly = value;

        return assembly;
    }
    protected Type? LoadServiceFromAssembly(Assembly assembly, Type serviceType)
    {
        Type? implementationType;

        var assemlyInterfaceType = new KeyValuePair<Assembly, Type>(assembly, serviceType);

        if (!AssemblyInterfaceTypes.TryGetValue(assemlyInterfaceType, out Type? value))
        {
            var types = assembly.GetTypes();
            implementationType = types.FirstOrDefault(t => t.GetInterfaces().Contains(serviceType));
            if (implementationType != null)
                AssemblyInterfaceTypes.Add(assemlyInterfaceType, implementationType);
        }
        else
            implementationType = value;

        return implementationType;
    }

    protected Type? LoadServiceFromAssembly(Assembly assembly, string typeName)
    {
        Type? serviceType;

        var key = new KeyValuePair<Assembly, string>(assembly, typeName);

        if (!AssemblyNameTypes.TryGetValue(key, out Type? type))
        {
            var types = assembly.GetTypes();
            serviceType = types.FirstOrDefault(t => t.IsClass && !t.IsAbstract && t.Name.Contains(typeName));
            if (serviceType != null)
                AssemblyNameTypes.Add(key, serviceType);
        }
        else
            serviceType = type;

        return serviceType;
    }

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
            implementationType = serviceTypeName.GetServiceTypeFromAssembly(path);

            if (implementationType != null)
                PathServiceTypeNameTypes.Add(key, implementationType);
        }

        return implementationType;
    }

    protected IServiceCollection AddService(Type interfaceType, string assemblyPath, out Type? serviceType)
    {
        Assembly assembly = LoadAssembly(assemblyPath);

        var serviceKey = new KeyValuePair<Assembly, Type>(assembly, interfaceType);

        if (!AssemblyInterfaceTypes.TryGetValue(serviceKey, out serviceType))
            serviceType = LoadServiceFromAssembly(assembly, interfaceType);

        return serviceType != null ?
             Builder.Services.AddSingleton(interfaceType, serviceType)
            : Builder.Services;
    }

    protected IServiceCollection AddService(string serviceTypeName, string assemblyPath, out Type? implementationType)
    {
        Assembly assembly = LoadAssembly(assemblyPath);

        var interfaceType = assembly.GetInterfaceType(serviceTypeName);

        var serviceKey = new KeyValuePair<Assembly, Type>(assembly, interfaceType);

        if (!AssemblyInterfaceTypes.TryGetValue(serviceKey, out implementationType))
            implementationType = LoadServiceFromAssembly(assembly, serviceTypeName);

        return implementationType != null ?
             Builder.Services.AddSingleton(interfaceType, implementationType)
            : Builder.Services;
    }

    protected IServiceCollection AddKeyedService(Type interfaceType, string assemblyPath, object? key, out Type? serviceType)
    {
        Assembly assembly = LoadAssembly(assemblyPath);

        var serviceKey = new KeyValuePair<Assembly, Type>(assembly, interfaceType);

        if (!AssemblyInterfaceTypes.TryGetValue(serviceKey, out serviceType))
            serviceType = LoadServiceFromAssembly(assembly, interfaceType);


        return serviceType != null ?
            Builder.Services.AddKeyedSingleton(interfaceType, key, serviceType)
            : Builder.Services;
    }

    protected IServiceCollection AddKeyedService(string serviceTypeName, string assemblyPath, object? key, out Type? implementationType)
    {
        Assembly assembly = LoadAssembly(assemblyPath);

        var interfaceType = assembly.GetInterfaceType(serviceTypeName);

        var serviceKey = new KeyValuePair<Assembly, Type>(assembly, interfaceType);

        if (!AssemblyInterfaceTypes.TryGetValue(serviceKey, out implementationType))
            implementationType = LoadServiceFromAssembly(assembly, serviceTypeName);

        return implementationType != null ?
             Builder.Services.AddKeyedSingleton(interfaceType, implementationType, key)
            : Builder.Services;
    }

    protected IServiceCollection AddServiceByImplementationFactory(Type serviceInterfaceType, string serviceProviderPath)
    {
        AddService(typeof(IServiceImplementationFactory), serviceProviderPath, out Type? serviceProviderType);

        if (serviceProviderType == null)
            return Builder.Services;

        return Builder.Services.AddServiceImplementationSingleton(serviceInterfaceType, serviceProviderType);
    }

    protected IServiceCollection AddServiceByImplementationFactory(string serviceTypeName, string serviceProviderPath, string assemblyPath)
    {
        return AddServiceByImplementationFactory(serviceTypeName.GetInterfaceTypeFromAssembly(assemblyPath), serviceProviderPath);
    }

    protected IServiceCollection AddServiceByKeyedImplementationFactory(Type serviceInterfaceType, string serviceProviderPath, object? key)
    {
        AddKeyedService(typeof(IServiceImplementationFactory), serviceProviderPath, key, out Type? serviceProviderType);

        if (serviceProviderType == null)
            return Builder.Services;

        return Builder.Services.AddServiceImplementationSingletonByKey(serviceInterfaceType, serviceProviderType, key);
    }

    protected IServiceCollection AddServiceByKeyedImplementationFactory(string serviceTypeName, string serviceProviderPath, string assemblyPath, object? key)
    {
        return AddServiceByKeyedImplementationFactory(serviceTypeName.GetInterfaceTypeFromAssembly(assemblyPath), serviceProviderPath, key);
    }

    protected IServiceCollection AddServiceValueFromJson(string assemblyPath, string json, Func<Assembly, Type?> getType)
    {
        var assembly = LoadAssembly(assemblyPath);

        var implementationType = getType(assembly);

        if (implementationType == null) return Builder.Services;

        var value = JsonSerializer.Deserialize(json, implementationType);

        return value != null ? Builder.Services.AddSingleton(implementationType, value) : Builder.Services;
    }

    protected IServiceCollection AddServiceValueFromJson(Type serviceType, string assemblyPath, string json)
    {
        return AddServiceValueFromJson(assemblyPath, json, (assembly) => LoadServiceFromAssembly(assembly, serviceType));
    }

    protected IServiceCollection AddServiceValueFromJson(string serviceTypeName, string assemblyPath, string json)
    {
        var implementationType = LoadServiceFromPath(assemblyPath, serviceTypeName);

        if (implementationType == null) return Builder.Services;

        var value = JsonSerializer.Deserialize(json, implementationType);

        return value != null ? Builder.Services.AddSingleton(implementationType, value) : Builder.Services;
    }

    protected IServiceCollection AddKeyedServiceValueFromJson(Func<Assembly, Type?> getType, string assemblyPath, string json, object? key)
    {
        var assembly = LoadAssembly(assemblyPath);

        var implementationType = getType(assembly);

        if (implementationType == null) return Builder.Services;

        var value = JsonSerializer.Deserialize(json, implementationType);

        return value != null ? Builder.Services.AddKeyedSingleton(implementationType, key, value) : Builder.Services;
    }
    protected IServiceCollection AddKeyedServiceValueFromJson(string serviceTypeName, string assemblyPath, string json, object? key)
    {
        var implementationType = LoadServiceFromPath(assemblyPath, serviceTypeName);

        if (implementationType == null) return Builder.Services;

        var value = JsonSerializer.Deserialize(json, implementationType);

        return value != null ? Builder.Services.AddKeyedSingleton(implementationType, key, value) : Builder.Services;
    }

    protected IServiceCollection AddKeyedServiceValueFromJson(Type serviceType, string assemblyPath, string json, object? key)
    {
        return AddKeyedServiceValueFromJson((assembly) => LoadServiceFromAssembly(assembly, serviceType), assemblyPath, json, key);
    }

    protected virtual IServiceCollection AddServiceBySettings(Type serviceType, IServiceSettings serviceSettings, object? implementationKey)
    {
        if (!string.IsNullOrEmpty(serviceSettings.ServiceProviderPath))
        {
            return AddServiceByKeyedImplementationFactory(serviceType, serviceSettings.ServiceProviderPath, implementationKey);
        }
        else if (serviceSettings.Value != null && !string.IsNullOrEmpty(serviceSettings.AssemblyPath))
        {
            return AddServiceValueFromJson(serviceType, serviceSettings.AssemblyPath, serviceSettings.Value.ToString());
        }

        return AddService(serviceType, serviceSettings.AssemblyPath, out Type? implementationType);
    }

    protected virtual IServiceCollection AddKeyedServiceBySettings(Type serviceType, IServiceSettings serviceSettings, object? key)
    {
        if (!string.IsNullOrEmpty(serviceSettings.ServiceProviderPath))
        {
            return AddServiceByKeyedImplementationFactory(serviceType, serviceSettings.ServiceProviderPath, key);
        }
        else if (serviceSettings.Value != null && !string.IsNullOrEmpty(serviceSettings.AssemblyPath))
        {
            return AddKeyedServiceValueFromJson(serviceType, serviceSettings.AssemblyPath, serviceSettings.Value.ToString(),key);
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
            return AddServiceValueFromJson(serviceSettings.ServiceTypeName, assemblyPath, serviceSettings.Value.ToString());
        }

        return AddService(serviceSettings.ServiceTypeName, serviceSettings.AssemblyPath, out Type? implementationType);
    }

    protected virtual IServiceCollection AddKeyedServiceBySettings(IServiceSettings serviceSettings, object? key)
    {
        var assemblyPath = serviceSettings.AssemblyPath;

        if (!string.IsNullOrEmpty(serviceSettings.ServiceProviderPath))
        {
            return AddServiceByKeyedImplementationFactory(serviceSettings.ServiceTypeName, serviceSettings.ServiceProviderPath, assemblyPath, key);
        }
        else if (serviceSettings.Value != null && !string.IsNullOrEmpty(serviceSettings.AssemblyPath))
        {
            return AddKeyedServiceValueFromJson(serviceSettings.ServiceTypeName, assemblyPath, serviceSettings.Value.ToString(),key);
        }

        return AddKeyedService(serviceSettings.ServiceTypeName, serviceSettings.AssemblyPath,key, out Type? implementationType);
    }
}
