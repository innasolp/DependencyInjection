using System.Reflection;

namespace DependencyInjection.AssemblyExtensions;

public static class TypeExtensions
{
    private static IEnumerable<Assembly> GetAllAssembliesFromPath(string path)
    {
        var allFiles = Directory.EnumerateFiles(path, "*.dll", SearchOption.AllDirectories).ToArray();
        
        var assemblies = new List<Assembly>();
        foreach (var file in allFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(file);
                if (!assemblies.Contains(assembly))
                    assemblies.Add(assembly);
            }
            catch(BadImageFormatException)
            {
                continue;
            }
        }  

        return assemblies;
    }

    public static Dictionary<Assembly, Type[]> GetAssemblyImplementationsForInterface(this Type interfaceType, string path)
    {
        var assemblies = GetAllAssembliesFromPath(path);

        return assemblies.Select(a => new { assembly = a, types = a.GetTypes().Where(t => t.IsImplementation(interfaceType)) })
             .ToDictionary(at => at.assembly, at => at.types.ToArray());
    }

    public static Type[] GetImplementationsForInterface(this Type interfaceType, string path)
    {
        var assemblies = GetAllAssembliesFromPath(path);

        return assemblies.SelectMany(a => a.GetTypes().Where(t => t.IsImplementation(interfaceType))).Distinct().ToArray();
    }

    public static Type? GetServiceTypeFromAssembly(this string path, string serviceTypeName)
    {
        var assemblies = GetAllAssembliesFromPath(path);

        return assemblies.SelectMany(a => a.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.Name.Contains(serviceTypeName))).FirstOrDefault();
    }

    public static Type? GetInterfaceTypeFromAssembly(this string path, string serviceTypeName)
    {
        var assemblies = GetAllAssembliesFromPath(path);

        return assemblies.SelectMany(a => a.GetTypes().Where(t => t.IsInterface && t.Name.Contains(serviceTypeName))).FirstOrDefault();
    }
    public static Type? GetInterfaceType(this Assembly assembly, string serviceTypeName)
    {
        return assembly.GetTypes().Where(t => t.IsInterface && t.Name.Contains(serviceTypeName)).FirstOrDefault();
    }

    public static Type? GetServiceImplementationFromAssembly(this string path, Type interfaceType)
    {
        var assemblies = GetAllAssembliesFromPath(path);

        return assemblies.SelectMany(a => a.GetTypes().Where(t => t.IsImplementation(interfaceType))).FirstOrDefault();
    }

    public static bool IsImplementation(this Type type, Type interfaceType)
    {
        return type.IsClass && !type.IsAbstract && type.GetInterfaces().Contains(interfaceType);
    }

}
