using DependencyInjection.ImplementationFactory;
using Json.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace DependencyInjection.WorkerBuilder;

public abstract class WorkerBuilder(IHostApplicationBuilder builder)
{
    protected IHostApplicationBuilder Builder { get; } = builder;

    protected Dictionary<string, Assembly> ServiceAssemblies { get; } = [];

    protected Dictionary<KeyValuePair<Assembly, Type>, Type> AssemblyInterfaceTypes { get; } = [];

    protected Dictionary<KeyValuePair<Assembly, string>, Type> AssemblyNameTypes { get; } = [];

    protected Dictionary<KeyValuePair<string, Type>, Type[]> PathInterfaceTypes { get; } = [];

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
    protected Type? LoadServiceTypeByInterface(Assembly assembly, Type serviceInterfaceType)
    {
        Type? serviceType;

        var assemlyInterfaceType = new KeyValuePair<Assembly, Type>(assembly, serviceInterfaceType);

        if (!AssemblyInterfaceTypes.TryGetValue(assemlyInterfaceType, out Type? value))
        {
            var types = assembly.GetTypes();
            serviceType = types.FirstOrDefault(t => t.GetInterfaces().Contains(serviceInterfaceType));
            if (serviceType != null)
                AssemblyInterfaceTypes.Add(assemlyInterfaceType, serviceType);
        }
        else
            serviceType = value;

        return serviceType;
    }

    protected Type? LoadServiceTypeByName(Assembly assembly, string typeName)
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

    protected Type? LoadImplementationType(string path, Type interfaceType)
    {
        Type? implementationType = null;

        var key = new KeyValuePair<string, Type>(path, interfaceType);

        if (PathInterfaceTypes.TryGetValue(key, out Type[]? types))
            implementationType = types?.FirstOrDefault();
        else
        {
            var implementations = interfaceType.GetAssemblyImplmentationsForInterface(path).SelectMany(at => at.Value).ToArray();
            if (implementations.Length != 0)
                PathInterfaceTypes.Add(key, implementations);
            implementationType = implementations.FirstOrDefault();
        }

        return implementationType;
    }

    protected IServiceCollection AddService(string assemblyPath, Type interfaceType, object? key, out Type? serviceType)
    {
        Assembly assembly = LoadAssembly(assemblyPath);

        var serviceKey = new KeyValuePair<Assembly, Type>(assembly, interfaceType);
        
        if (!AssemblyInterfaceTypes.TryGetValue(serviceKey, out serviceType))        
            serviceType = LoadServiceTypeByInterface(assembly, interfaceType);
        

        return serviceType != null ?
            (key != null ? Builder.Services.AddKeyedSingleton(interfaceType, key, serviceType)
                          : Builder.Services.AddSingleton(interfaceType, serviceType))
            : Builder.Services;
    }

    protected IServiceCollection AddServiceByImplementationFactory(string serviceProviderPath,Type serviceInterfaceType, object? key)
    {
        AddService(serviceProviderPath, typeof(IServiceImplementationFactory), key, out Type? serviceProviderType);

        if (serviceProviderType == null)
            return Builder.Services;

        return key != null ? Builder.Services.AddServiceImplementationSingleton(serviceInterfaceType, serviceProviderType, key)
                            : Builder.Services.AddServiceImplementationSingleton(serviceInterfaceType, serviceProviderType);
    }

    protected IServiceCollection AddServiceValue(string assemblyPath, string valuePath, Func<Assembly, Type?> getType)
    {
        var assembly = LoadAssembly(assemblyPath);

        var type = getType(assembly);

        if (type == null) return Builder.Services;

        var value = valuePath.ReadFromJsonFile(type);

        return value != null ? Builder.Services.AddSingleton(type, value) : Builder.Services;
    }

    protected IServiceCollection AddKeyedServiceValue(string assemblyPath, string valuePath, Func<Assembly, Type?> getType, object? key)
    {
        var assembly = LoadAssembly(assemblyPath);

        var type = getType(assembly);

        if (type == null) return Builder.Services;

        var value = valuePath.ReadFromJsonFile(type);

        return value != null ? Builder.Services.AddKeyedSingleton(type, value, key) : Builder.Services;
    }

    protected IServiceCollection AddServiceValueByInterface(string assemblyPath, string valuePath, Type interfaceType)
    {
        return AddServiceValue(assemblyPath, valuePath, (assembly) => LoadServiceTypeByInterface(assembly, interfaceType));
    }
    
    protected IServiceCollection AddKeyedServiceValueByInterface(string assemblyPath, string valuePath, Type interfaceType, object? key)
    {
        return AddKeyedServiceValue(assemblyPath, valuePath, (assembly) => LoadServiceTypeByInterface(assembly, interfaceType), key);
    }

    protected IServiceCollection AddServiceValueByTypeName(string assemblyPath, string valuePath, string typeName)
    {
        return AddServiceValue(assemblyPath, valuePath, (assembly) => LoadServiceTypeByName(assembly, typeName));
    }
    
    protected IServiceCollection AddKeyedServiceValueByTypeName(string assemblyPath, string valuePath, string typeName, object? key)
    {
        return AddKeyedServiceValue(assemblyPath, valuePath, (assembly) => LoadServiceTypeByName(assembly, typeName), key);
    }

    public IServiceCollection AddSingleton<TService>()
        where TService:class
    {
        return Builder.Services.AddSingleton<TService, TService>();
    }
    
    public IServiceCollection AddSingleton<TService, TImplementation>()
        where TService:class
        where TImplementation : class, TService
    {
        return Builder.Services.AddSingleton<TService, TImplementation>();
    }
    
    public IServiceCollection AddScoped<TService>()
        where TService:class
    {
        return Builder.Services.AddScoped<TService>();
    }

    public IServiceCollection ConfigureDefaultHttps()
    {
        return Builder.Services.ConfigureHttpClientDefaults(builder =>
        {
            builder.ConfigurePrimaryHttpMessageHandler(
                () => new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (req, cert, chain, errors) =>
                    {
                        return true;
                    }
                });
        });
    }

    public IHttpClientBuilder AddHttpClient(string name)
    {
        var httpClientBuilder = Builder.Services.AddHttpClient(name);
        HttpClientBuilders.Add(name, httpClientBuilder);
        return httpClientBuilder;
    }

    public IServiceCollection AddHttpMessageDelegatingHandler<TMessageHandler>(string apiHost)
        where TMessageHandler : DelegatingHandler
    {
        Builder.Services.AddSingleton<TMessageHandler>();

        if (HttpClientBuilders.TryGetValue(apiHost, out var httpClientBuilder))
            httpClientBuilder.AddHttpMessageHandler(serviceProvider => serviceProvider.GetRequiredService<TMessageHandler>());

        return Builder.Services;
    }
}
