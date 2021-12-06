using System.ComponentModel;
using Spectre.Console.Cli;

namespace SpotifyToolbox.CLI.Commands.Remove;

// ReSharper disable once ClassNeverInstantiated.Global
public class RemoveSettings : CommandSettings
{
    [CommandOption("-d|--dry-run")]
    [Description("Runs in dry-run mode which just shows what would be removed when running the command without this flag without doing any changes in your library")]
    public bool DryRun { get; set; }
}