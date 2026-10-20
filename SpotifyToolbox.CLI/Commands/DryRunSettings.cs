using System.ComponentModel;
using Spectre.Console.Cli;

namespace SpotifyToolbox.CLI.Commands;

// ReSharper disable once ClassNeverInstantiated.Global
public class DryRunSettings : CommandSettings
{
    [CommandOption("-d|--dry-run")]
    [Description("Shows what the command would do without making any changes to your library")]
    public bool DryRun { get; init; }
}
