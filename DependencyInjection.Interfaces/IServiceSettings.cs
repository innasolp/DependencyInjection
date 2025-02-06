using System.Text.Json.Nodes;

namespace DependencyInjection.Interfaces;

public interface IServiceSettings
{
    string? ServiceTypeName { get; set; }

    string? ImplementationTypeName { get; set; }

    string? AssemblyPath { get; set; }

    string? ServiceProviderPath { get; set; }

    JsonObject? Value { get; set; }
}
