using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Vortex.Specs.Analysis.Emulator;
using Vortex.Specs.Captures;
using Vortex.Specs.Diff;
using Vortex.Specs.Model;
using Vortex.Specs.Sources;

namespace Vortex.Specs.Cli.Commands;

/// <summary>
/// Compares two capture traces of the same triggers.
/// </summary>
/// <remarks>
/// The differential-testing seam. Today both sides are capture files, which already covers the
/// useful case of "record the same action against Habbo and against this emulator, then look at what
/// differs". When the emulator can be driven headlessly its trace becomes the second argument and
/// nothing else here changes.
/// </remarks>
public static class DiffCommand
{
    public static int Run(SpecWorkspace workspace, CommandLine command)
    {
        if (command.Positional.Count < 2)
        {
            Console.Error.WriteLine(
                "diff needs two capture files: diff <reference.json> <actual.json>"
            );
            return 2;
        }

        CaptureImporter importer = new();
        CSharpSourceIndex index = CSharpSourceIndex.Build(
            workspace.RepositoryRoot,
            ["Vortex.Revisions"]
        );
        RevisionRegistry registry = new EmulatorAnalyzer(workspace, index).Scan().Registry;

        if (
            !TryLoad(
                importer,
                registry,
                command.Positional[0],
                out IReadOnlyList<PacketTrace> reference
            )
        )
        {
            return 2;
        }

        if (
            !TryLoad(
                importer,
                registry,
                command.Positional[1],
                out IReadOnlyList<PacketTrace> actual
            )
        )
        {
            return 2;
        }

        TraceDiffer differ = new();
        int differing = 0;
        int compared = 0;

        foreach (PacketTrace expected in reference)
        {
            PacketTrace? match = actual.FirstOrDefault(a =>
                string.Equals(a.Trigger, expected.Trigger, StringComparison.Ordinal)
            );

            if (match is null)
            {
                Console.WriteLine();
                Console.WriteLine($"trigger: {expected.Trigger}");
                Console.WriteLine("  not present in the second trace at all");
                differing++;
                continue;
            }

            compared++;
            IReadOnlyList<TraceDifference> differences = differ.Compare(expected, match);

            if (differences.Count == 0)
            {
                continue;
            }

            differing++;
            Console.WriteLine();
            Console.WriteLine(TraceDiffer.Render(expected, match, differences));
        }

        Program.Heading("Summary");
        Program.Rows([
            ("triggers compared", compared.ToString(CultureInfo.InvariantCulture)),
            ("triggers differing", differing.ToString(CultureInfo.InvariantCulture)),
        ]);

        return differing > 0 ? 1 : 0;
    }

    private static bool TryLoad(
        CaptureImporter importer,
        RevisionRegistry registry,
        string path,
        out IReadOnlyList<PacketTrace> traces
    )
    {
        traces = [];

        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"No such file: {path}");
            return false;
        }

        try
        {
            CaptureDocument capture = importer.Read(path);
            traces = PacketTrace.FromCapture(capture, importer.Observe(capture, registry));
            return true;
        }
        catch (CaptureImportException error)
        {
            Console.Error.WriteLine(error.Message);
            return false;
        }
    }
}
