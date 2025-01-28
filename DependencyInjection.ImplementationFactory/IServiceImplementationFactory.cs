namespace DependencyInjection.ImplementationFactory;

public interface IServiceImplementationFactory
{
    object GetService(IServiceProvider serviceProvider, Type serviceType, object? key);

    object GetService(IServiceProvider serviceProvider, Type serviceType);
}

public interface IServiceImplementationFactory<T>: IServiceImplementationFactory
{
    T GetService(IServiceProvider serviceProvider, object? key);

    T GetService(IServiceProvider serviceProvider);
}
