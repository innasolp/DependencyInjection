using System.Reflection;

namespace DependencyInjection.AssemblyExtensions;

public static class AssemblyHelper
{
    public static IEnumerable<Assembly> GetAllAssembliesFromPath(string path)
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
}