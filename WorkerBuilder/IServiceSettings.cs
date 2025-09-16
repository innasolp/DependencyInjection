namespace DependencyInjection.WorkerBuilder;

public interface IServiceSettings
{
    string? ServiceTypeName { get; set; }

    string? ImplementationTypeName { get; set; }

    string? AssemblyPath { get; set; }

    string? ServiceProviderPath { get; set; }

    string? Value { get; set; }
}
