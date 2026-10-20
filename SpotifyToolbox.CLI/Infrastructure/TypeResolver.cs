using Spectre.Console.Cli;

namespace SpotifyToolbox.CLI.Infrastructure;

/// <summary>
/// Type resolver to be used for dependency injection. Taken from SpectreConsole's examples:
/// https://github.com/spectreconsole/spectre.console/blob/main/examples/Cli/Injection/Infrastructure/TypeResolver.cs
/// </summary>
public sealed class TypeResolver(IServiceProvider provider) : ITypeResolver, IDisposable
{
    public object? Resolve(Type? type) => type == null ? null : provider.GetService(type);

    public void Dispose() => (provider as IDisposable)?.Dispose();
}
