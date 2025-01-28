using System.Reflection;

namespace DependencyInjection.WorkerBuilder;

public static class TypeExtensions
{
    public static Dictionary<Assembly, Type[]> GetAssemblyImplmentationsForInterface(this Type interfaceType, string path)
    {
        if (!Directory.Exists(path))
            throw new Exception($"folder for {interfaceType}  does not exists");
        var assembliesDir = Directory.GetDirectories(path);

        List<Assembly> assemblies = [];

        if (assembliesDir.Length != 0)
            assemblies = assembliesDir.SelectMany(d => Directory.GetFiles(d, "*.dll")).Select(Assembly.LoadFrom).ToList();
        if (assemblies.Count == 0)
            assemblies = Directory.GetFiles(path, "*.dll").Select(Assembly.LoadFrom).ToList();

        return assemblies.Select(a => new { assembly = a, types = a.GetTypes().Where(t => t.IsImplementation(interfaceType)) })
             .ToDictionary(at => at.assembly, at => at.types.ToArray());
    }

    public static bool IsImplementation(this Type type, Type interfaceType)
    {
        return type.IsClass && !type.IsAbstract && type.GetInterfaces().Contains(interfaceType);
    }

}
