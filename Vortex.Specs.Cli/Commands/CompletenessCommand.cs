using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Vortex.Specs.Completeness;
using Vortex.Specs.Pipeline;
using Vortex.Specs.Reasoning;
using Vortex.Specs.Sources;

namespace Vortex.Specs.Cli.Commands;

/// <summary>
/// Measures the target client's incoming surface against what this repository does about it.
/// </summary>
/// <remarks>
/// Read-only unless <c>--write</c> is passed. It fixes no gap and touches no gameplay: the value of
/// a number nobody was tempted to improve while producing it is the whole point.
/// </remarks>
public static class CompletenessCommand
{
    public static int Run(SpecWorkspace workspace, CommandLine command)
    {
        string root = Path.Combine(workspace.RepositoryRoot, "docs", "completeness");

        SpecWorld world = new SpecPipeline(workspace).Scan(Program.Progress(command));
        List<string> notes = [];
        ResolvedSpecs specs = SpecPipeline.Resolve(world, notes);

        CompletenessReport report = new CompletenessAnalyzer().Analyze(
            world,
            specs,
            Directory.Exists(root) ? CompletenessLedger.Load(root) : CompletenessLedger.Empty
        );

        if (!report.HasTargetClient)
        {
            // No denominator, so no score. Reporting zero gaps out of zero obligations would be a
            // clean 100% and a lie, and it is the specific lie this program was written to prevent.
            Program.Heading("No target client");

            foreach (string problem in report.Problems)
            {
                Console.Error.WriteLine($"  {problem}");
            }

            return 2;
        }

        // The box surface, scored against the same build. Always computed: it is the number the
        // packet matrix cannot see, and leaving it behind a flag is how it stops being read.
        WiredSurfaceReport wired = WiredSurfaceAnalyzer.Analyze(workspace, world.Emulator.Revision);
        FurnitureSurfaceReport furniture = FurnitureSurfaceAnalyzer.Analyze(
            workspace.RepositoryRoot
        );

        Print(report, wired, furniture, command);

        if (command.Has("write"))
        {
            IReadOnlyList<string> written = CompletenessWriter.Write(
                Path.Combine(root, "generated"),
                report,
                wired,
                furniture
            );

            Program.Heading($"Wrote {written.Count} files");

            foreach (string file in written)
            {
                Console.WriteLine($"  docs/completeness/generated/{file}");
            }
        }

        if (report.Problems.Count > 0)
        {
            return 1;
        }

        string? failOn = command.Value("fail-on");

        if (failOn is null)
        {
            return 0;
        }

        if (!ObligationStatusNames.TryParse(failOn, out ObligationStatus threshold))
        {
            Console.Error.WriteLine($"unknown status '{failOn}' for --fail-on");
            return 2;
        }

        int offending = report.Count(threshold);

        if (offending == 0)
        {
            return 0;
        }

        Console.Error.WriteLine(
            $"{offending.ToString(CultureInfo.InvariantCulture)} obligations are "
                + $"{threshold.Wire()}"
        );

        return 1;
    }

    private static void Print(
        CompletenessReport report,
        WiredSurfaceReport wired,
        FurnitureSurfaceReport furniture,
        CommandLine command
    )
    {
        Program.Heading($"Completeness against {report.TargetRevision}");
        Program.Rows([
            ("source", report.TargetOrigin ?? "-"),
            ("incoming obligations", Number(report.Obligations.Count)),
            ("unresolved surface", Number(report.UnresolvedSurface.Count)),
            // Labelled for what it is: an obligation this emulator cannot receive is exactly what
            // `missing` means, so this is the denominator less missing and n/a, restated. It carries
            // nothing the status table below does not already carry, and reading the two as two
            // pieces of evidence is the mistake the label exists to prevent.
            ("protocol mapping (= not missing)", report.Share(report.Mapped)),
            ("implementation", report.Share(report.Implemented)),
            ("verified complete", report.Share(report.Count(ObligationStatus.Complete))),
        ]);

        Console.WriteLine();
        Program.Rows([
            .. ObligationStatusNames.Scored.Select(s => (s.Wire(), Number(report.Count(s)))),
        ]);

        // Printed next to the packet numbers on purpose. "wired: 42/43" and "wired boxes: 142/184"
        // describe the same subsystem and disagree by twenty points, because one counts the messages
        // that configure a box and the other counts the boxes.
        Program.Heading("Wired boxes (a second surface)");
        Program.Rows([
            ("configurable boxes", Number(wired.Boxes.Count)),
            ("bound to a logic", wired.Share),
            ("bound here, absent from the client", Number(wired.UnreachableInVortex.Count)),
            .. wired.ByFamily.Select(g =>
                (
                    g.Key.ToString().ToLowerInvariant(),
                    $"{g.Count(b => b.Implemented)} / {g.Count()}"
                )
            ),
        ]);

        foreach (string problem in wired.Problems)
        {
            Console.WriteLine($"  {problem}");
        }

        // The widest blast radius per gap of the three surfaces: one unbound name strands every
        // definition carrying it, and the furni just sits there.
        Program.Heading("Furniture logic (a third surface)");
        Program.Rows([
            ("logic names in the pass", Number(furniture.Logics.Count)),
            ("definitions covered", Number(furniture.Definitions)),
            ("answered by a logic", furniture.Share),
            ("stranded", Number(furniture.Stranded)),
        ]);

        foreach (
            FurnitureLogicObligation logic in furniture
                .Logics.Where(l => !l.Registered)
                .Take(command.ValueOrDefault("limit", 10))
        )
        {
            Console.WriteLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "  {0,8} definitions   {1}",
                    logic.Definitions,
                    logic.Logic
                )
            );
        }

        foreach (string problem in furniture.Problems)
        {
            Console.WriteLine($"  {problem}");
        }

        Program.Heading("By domain");

        foreach (DomainSummary domain in report.Domains)
        {
            Console.WriteLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "  {0,-20} {1,4} obligations   {2,4} missing   {3,4} partial   {4,4} implemented   {5,4} complete",
                    domain.Domain,
                    domain.Obligations.Count,
                    domain.Count(ObligationStatus.Missing),
                    domain.Count(ObligationStatus.Partial),
                    domain.Count(ObligationStatus.Implemented),
                    domain.Count(ObligationStatus.Complete)
                )
            );
        }

        string? wantedDomain = command.Value("domain");
        string? wantedStatus = command.Value("status");
        ObligationStatus? status = null;

        if (
            wantedStatus is not null
            && ObligationStatusNames.TryParse(wantedStatus, out ObligationStatus parsed)
        )
        {
            status = parsed;
        }

        List<Obligation> gaps =
        [
            .. report
                .Obligations.Where(o =>
                    (
                        wantedDomain is null
                        || string.Equals(o.Domain, wantedDomain, StringComparison.OrdinalIgnoreCase)
                    )
                    && (
                        status is null
                            ? o.Status is ObligationStatus.Missing or ObligationStatus.Partial
                            : o.Status == status
                    )
                )
                .OrderBy(o => o.Status)
                .ThenBy(o => o.Domain, StringComparer.Ordinal)
                .ThenBy(o => o.Name, StringComparer.Ordinal),
        ];

        int limit = command.ValueOrDefault("limit", 40);

        Program.Heading(
            status is null ? $"Gaps ({gaps.Count})" : $"{status.Value.Wire()} ({gaps.Count})"
        );

        foreach (Obligation gap in gaps.Take(limit))
        {
            Console.WriteLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "  {0,-18} {1,-16} {2,-42} {3}",
                    gap.Status.Wire(),
                    gap.Domain,
                    gap.Name,
                    gap.Reason
                )
            );
        }

        if (gaps.Count > limit)
        {
            Console.WriteLine($"  ...and {Number(gaps.Count - limit)} more");
        }

        if (report.Problems.Count == 0)
        {
            return;
        }

        Program.Heading($"Problems ({report.Problems.Count})");

        foreach (string problem in report.Problems)
        {
            Console.WriteLine($"  {problem}");
        }
    }

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
