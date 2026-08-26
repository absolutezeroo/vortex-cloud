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

        Print(report, command);

        if (command.Has("write"))
        {
            IReadOnlyList<string> written = CompletenessWriter.Write(
                Path.Combine(root, "generated"),
                report
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

    private static void Print(CompletenessReport report, CommandLine command)
    {
        Program.Heading($"Completeness against {report.TargetRevision}");
        Program.Rows([
            ("source", report.TargetOrigin ?? "-"),
            ("incoming obligations", Number(report.Obligations.Count)),
            ("unresolved surface", Number(report.UnresolvedSurface.Count)),
            ("protocol mapping", report.Share(report.Mapped)),
            ("implementation", report.Share(report.Implemented)),
            ("verified complete", report.Share(report.Count(ObligationStatus.Complete))),
        ]);

        Console.WriteLine();
        Program.Rows([
            .. ObligationStatusNames.Scored.Select(s => (s.Wire(), Number(report.Count(s)))),
        ]);

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
