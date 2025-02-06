using System.IO;
using System.Reflection;

namespace DependencyInjection.WorkerBuilder;

public static class TypeExtensions
{
    private static IEnumerable<Assembly> GetAllAssembliesFromPath(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException(path);

        var assembliesDir = Directory.GetDirectories(path);

        List<Assembly> assemblies = [];

        if (assembliesDir.Length != 0)
            assemblies = assembliesDir.SelectMany(d => Directory.GetFiles(d, "*.dll")).Select(Assembly.LoadFrom).ToList();
        if (assemblies.Count == 0)
            assemblies = Directory.GetFiles(path, "*.dll").Select(Assembly.LoadFrom).ToList();

        return assemblies;
    }

    public static Dictionary<Assembly, Type[]> GetAssemblyImplmentationsForInterface(this Type interfaceType, string path)
    {
        var assemblies = GetAllAssembliesFromPath(path);

        return assemblies.Select(a => new { assembly = a, types = a.GetTypes().Where(t => t.IsImplementation(interfaceType)) })
             .ToDictionary(at => at.assembly, at => at.types.ToArray());
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
