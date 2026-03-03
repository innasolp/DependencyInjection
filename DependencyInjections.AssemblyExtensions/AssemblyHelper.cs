using System.Reflection;

namespace DependencyInjection.AssemblyExtensions;

public static class AssemblyHelper
{
    public static IEnumerable<Assembly> GetAllAssembliesFromPath(string path)
    {
        if (!Path.Exists(path))
            throw new DirectoryNotFoundException($"Path {path} not found.");

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
            catch (BadImageFormatException)
            {
                continue;
            }
            catch (FileLoadException)
            {
                continue;
            }
        }

        return assemblies;
    }

    public static Type[] GetLoadableTypes(this Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return  [.. ex.Types.Where(t => t != null)];
        }
    }
}