using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vortex.Specs.Cli.Commands;
using Vortex.Specs.Sources;

namespace Vortex.Specs.Cli;

/// <summary>
/// The <c>habbo-spec</c> entry point.
/// </summary>
/// <remarks>
/// Written to be read by a person at a terminal and by an agent reading stdout. Both want the same
/// thing: the answer, what backs it, and what is still unknown — so the output is plain text with a
/// stable shape rather than either a table nobody can parse or JSON nobody can read.
/// </remarks>
public static class Program
{
    public static int Main(string[] args)
    {
        CommandLine command = CommandLine.Parse(args);

        try
        {
            SpecWorkspace workspace = SpecWorkspace.Discover(
                command.Value("repo") ?? Directory.GetCurrentDirectory(),
                command.Value("out")
            );

            return command.Verb switch
            {
                "bootstrap" => BootstrapCommand.Run(workspace, command),
                "scan-emulator" => ScanCommand.Emulator(workspace),
                "scan-client" => ScanCommand.Clients(workspace),
                "scan-references" => ScanCommand.References(workspace),
                "import-capture" => CaptureCommand.Import(workspace, command),
                "analyze" => AnalyzeCommand.Run(workspace, command),
                "validate" => ReviewCommand.Validate(workspace),
                "conflicts" => ReviewCommand.Conflicts(workspace, command),
                "unknowns" => ReviewCommand.Unknowns(workspace, command),
                "headers" => ReviewCommand.Headers(workspace, command),
                "diff" => DiffCommand.Run(workspace, command),
                _ => Help(),
            };
        }
        catch (DirectoryNotFoundException error)
        {
            Console.Error.WriteLine(error.Message);
            return 2;
        }
        catch (IOException error)
        {
            Console.Error.WriteLine($"i/o error: {error.Message}");
            return 2;
        }
    }

    private static int Help()
    {
        Console.WriteLine(
            """
            habbo-spec — build and query the Habbo behavioural specs from evidence.

              bootstrap              scan every source, regenerate docs/habbo-specs, print a report
              scan-emulator          what this emulator maps, without writing anything
              scan-client            what the client sources describe
              scan-references        what the reference emulators do
              import-capture <file>  read a capture and print the observations it yields
              analyze <feature|packet>
                                     everything known about one feature or packet, and what is not
              validate               check the spec tree for unbacked claims and broken links
              conflicts              disagreements between sources that nobody has settled
              unknowns               questions the sources do not answer, worst first
              headers                the per-revision header registries side by side
              diff <a.json> <b.json> compare two capture traces packet by packet

            Options:
              --repo <path>          checkout to analyze (default: the current directory's)
              --out <path>           where specs are written (default: <repo>/docs/habbo-specs)
              --force                replace generated blocks that were hand-edited
              --limit <n>            cap list output
              --severity <level>     unknowns: critical | medium | low
              --kind <kind>          conflicts: field_count | field_type | header_id | behaviour
              --verbose              show progress while scanning

            Nothing this tool prints is authority. Emulator behaviour is evidence, reference emulator
            behaviour is evidence, and official behaviour nobody has captured stays unknown.
            """
        );

        return 0;
    }

    /// <summary>Shared progress printer, silent unless asked.</summary>
    public static Action<string>? Progress(CommandLine command) =>
        command.Has("verbose") ? message => Console.Error.WriteLine($"  {message}") : null;

    /// <summary>Prints a heading the same way everywhere so output is skimmable.</summary>
    public static void Heading(string text)
    {
        Console.WriteLine();
        Console.WriteLine(text);
        Console.WriteLine(new string('-', text.Length));
    }

    public static void Rows(IEnumerable<(string Label, string Value)> rows)
    {
        List<(string Label, string Value)> materialized = [.. rows];

        if (materialized.Count == 0)
        {
            return;
        }

        int width = materialized.Max(r => r.Label.Length);

        foreach ((string label, string value) in materialized)
        {
            Console.WriteLine($"  {label.PadRight(width)}  {value}");
        }
    }
}
