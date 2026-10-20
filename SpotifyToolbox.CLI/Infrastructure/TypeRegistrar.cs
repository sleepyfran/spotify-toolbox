using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace SpotifyToolbox.CLI.Infrastructure;

/// <summary>
/// Type registrar to be used for dependency injection. Taken from SpectreConsole's examples:
/// https://github.com/spectreconsole/spectre.console/blob/main/examples/Cli/Injection/Infrastructure/TypeRegistrar.cs
/// </summary>
public sealed class TypeRegistrar(IServiceCollection builder) : ITypeRegistrar
{
    public ITypeResolver Build() => new TypeResolver(builder.BuildServiceProvider());

    public void Register(Type service, Type implementation) => builder.AddSingleton(service, implementation);

    public void RegisterInstance(Type service, object implementation) => builder.AddSingleton(service, implementation);

    public void RegisterLazy(Type service, Func<object> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        builder.AddSingleton(service, _ => factory());
    }
}
