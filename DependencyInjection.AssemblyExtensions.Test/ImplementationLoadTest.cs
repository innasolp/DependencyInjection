using DependencyInjection.ImplementationFactory;


namespace DependencyInjection.AssemblyExtensions.Test;

public class ImplementationLoadTest
{
    private readonly string _libsPath = "..\\..\\..\\..\\Libs\\DependencyInjection";
    
    [Fact]
    public void LoadImplementationsFromPath()
    {
        var serviceType = typeof(IServiceImplementationFactory);
        var libImplementations = serviceType.GetImplementationsForInterface(_libsPath);
        var localImplementations = serviceType.GetImplementationsForInterface(Directory.GetCurrentDirectory());
        Assert.Empty(libImplementations);
        Assert.Single(localImplementations);
    }
}