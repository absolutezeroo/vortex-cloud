using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Vortex.Specs.Model;
using Vortex.Specs.Persistence;
using Vortex.Specs.Pipeline;
using Vortex.Specs.Sources;

namespace Vortex.Specs.Cli.Commands;

public static class BootstrapCommand
{
    public static int Run(SpecWorkspace workspace, CommandLine command)
    {
        BootstrapReport report = new SpecBootstrapper(workspace).Run(
            command.Has("force"),
            Program.Progress(command)
        );

        Program.Heading("Habbo Specs");
        Program.Rows(report.SourcesScanned.Select(source => ("source", source)));

        Program.Heading("Packets discovered");
        Program.Rows([
            ("Incoming", report.IncomingPackets.ToString(CultureInfo.InvariantCulture)),
            ("Outgoing", report.OutgoingPackets.ToString(CultureInfo.InvariantCulture)),
        ]);

        Program.Heading("Behaviour");
        Program.Rows([
            ("Features discovered", report.Features.ToString(CultureInfo.InvariantCulture)),
            ("Scenarios generated", report.Scenarios.ToString(CultureInfo.InvariantCulture)),
            ("Captures imported", report.Captures.ToString(CultureInfo.InvariantCulture)),
            (
                "Capture observations",
                report.CaptureObservations.ToString(CultureInfo.InvariantCulture)
            ),
        ]);

        Program.Heading("Confidence in packet structure");
        int total = report.IncomingPackets + report.OutgoingPackets;

        Program.Rows(
            Enum.GetValues<Confidence>()
                .OrderByDescending(level => (int)level)
                .Select(level =>
                    (level, count: report.StructureConfidence.GetValueOrDefault(level))
                )
                .Where(pair => pair.count > 0)
                .Select(pair =>
                    (
                        pair.level.Wire(),
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0,5}  {1}",
                            pair.count,
                            Percent(pair.count, total)
                        )
                    )
                )
        );

        Program.Heading("Open questions");
        Program.Rows([
            ("Conflicts", report.Conflicts.ToString(CultureInfo.InvariantCulture)),
            ("Critical unknowns", report.CriticalUnknowns.ToString(CultureInfo.InvariantCulture)),
            ("Unknowns in total", report.TotalUnknowns.ToString(CultureInfo.InvariantCulture)),
            (
                "Fields with no attested name",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} of {1}",
                    report.PlaceholderFieldNames,
                    report.TotalFields
                )
            ),
        ]);

        Program.Heading("Files");
        Program.Rows([
            ("written", report.FilesWritten.ToString(CultureInfo.InvariantCulture)),
            ("unchanged", report.FilesUnchanged.ToString(CultureInfo.InvariantCulture)),
            ("blocked by hand edits", report.Blocked.Count.ToString(CultureInfo.InvariantCulture)),
        ]);

        foreach (SpecWriteResult blocked in report.Blocked.Take(20))
        {
            Console.WriteLine($"  ! {workspace.Relative(blocked.Path)}: {blocked.Detail}");
        }

        if (report.Captures == 0)
        {
            Console.WriteLine();
            Console.WriteLine(
                "  No captures were available. Every behavioural question in this tree is therefore"
            );
            Console.WriteLine(
                "  open — see docs/habbo-specs/evidence/captures/README.md for how to add some."
            );
        }

        if (report.Notes.Count > 0)
        {
            Program.Heading("Coverage the scan bounded");

            foreach (string note in report.Notes.Take(10))
            {
                Console.WriteLine($"  {note}");
            }

            if (report.Notes.Count > 10)
            {
                Console.WriteLine(
                    $"  ...and {report.Notes.Count - 10} more, all listed in REPORT.md"
                );
            }
        }

        Console.WriteLine();
        Console.WriteLine(
            $"Report written to {workspace.Relative(workspace.OutputRoot)}/REPORT.md"
        );

        // A blocked write is not a failure of the scan; it is the safety net doing its job, and the
        // exit code says so distinctly from a crash.
        return report.Blocked.Count > 0 ? 1 : 0;
    }

    private static string Percent(int count, int total) =>
        total == 0
            ? "0%"
            : ((count * 100.0) / total).ToString("0.#", CultureInfo.InvariantCulture) + "%";
}
