using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Vortex.Specs.Model;
using Vortex.Specs.Persistence;
using Vortex.Specs.Pipeline;
using Vortex.Specs.Reasoning;
using Vortex.Specs.Sources;

namespace Vortex.Specs.Cli.Commands;

/// <summary>The commands that read the spec tree back and hold it to account.</summary>
public static class ReviewCommand
{
    public static int Validate(SpecWorkspace workspace)
    {
        SpecStore store = new(workspace.OutputRoot);
        IReadOnlyList<string> files = store.Enumerate();

        if (files.Count == 0)
        {
            Console.Error.WriteLine(
                $"No specs under {workspace.Relative(workspace.OutputRoot)}. Run `habbo-spec bootstrap` first."
            );
            return 2;
        }

        IReadOnlyList<ValidationIssue> issues = new SpecValidator().Validate(store);

        Program.Heading($"Validated {files.Count} spec files");

        int errors = issues.Count(i => i.Severity == ValidationSeverity.Error);
        int warnings = issues.Count - errors;

        Program.Rows([
            ("errors", errors.ToString(CultureInfo.InvariantCulture)),
            ("warnings", warnings.ToString(CultureInfo.InvariantCulture)),
        ]);

        foreach (ValidationIssue issue in issues.Take(200))
        {
            Console.WriteLine(
                $"  [{issue.Severity.ToString().ToLowerInvariant()}] {workspace.Relative(issue.Path)}: {issue.Message}"
            );
        }

        if (issues.Count > 200)
        {
            Console.WriteLine($"  ...and {issues.Count - 200} more");
        }

        return errors > 0 ? 1 : 0;
    }

    public static int Conflicts(SpecWorkspace workspace, CommandLine command)
    {
        ResolvedSpecs specs = Load(workspace, command);
        string? kind = command.Value("kind");

        List<ConflictSpec> conflicts =
        [
            .. specs.Conflicts.Where(c =>
                kind is null
                || string.Equals(
                    Vortex.Specs.Naming.PacketNaming.SnakeCase(c.Kind.ToString()),
                    kind,
                    StringComparison.OrdinalIgnoreCase
                )
            ),
        ];

        Program.Heading($"Conflicts ({conflicts.Count})");

        foreach (
            IGrouping<ConflictKind, ConflictSpec> group in conflicts
                .GroupBy(c => c.Kind)
                .OrderBy(g => g.Key)
        )
        {
            Console.WriteLine();
            Console.WriteLine(
                $"  {Vortex.Specs.Naming.PacketNaming.SnakeCase(group.Key.ToString())}: {group.Count()}"
            );

            foreach (ConflictSpec conflict in group.Take(command.ValueOrDefault("limit", 20)))
            {
                Console.WriteLine($"    {conflict.Id}  {conflict.Subject}");

                foreach (ConflictPosition position in conflict.Positions)
                {
                    Console.WriteLine(
                        $"        {position.Origin} ({position.Authority.Wire()}): {position.Claim}"
                    );
                }
            }
        }

        if (conflicts.Count == 0)
        {
            Console.WriteLine("  none");
        }

        Console.WriteLine();
        Console.WriteLine(
            "  No conflict here has been arbitrated. Where the official answer is unknown, the"
        );
        Console.WriteLine("  disagreement is the answer until evidence closes it.");

        return 0;
    }

    public static int Unknowns(SpecWorkspace workspace, CommandLine command)
    {
        ResolvedSpecs specs = Load(workspace, command);
        string? severity = command.Value("severity");

        List<UnknownSpec> unknowns =
        [
            .. specs.Unknowns.Where(u =>
                severity is null
                || string.Equals(
                    u.Severity.ToString(),
                    severity,
                    StringComparison.OrdinalIgnoreCase
                )
            ),
        ];

        Program.Heading($"Unknowns ({unknowns.Count})");

        foreach (
            IGrouping<UnknownSeverity, UnknownSpec> group in unknowns
                .GroupBy(u => u.Severity)
                .OrderByDescending(g => g.Key)
        )
        {
            Console.WriteLine();
            Console.WriteLine($"  {group.Key.ToString().ToLowerInvariant()}: {group.Count()}");

            foreach (UnknownSpec unknown in group.Take(command.ValueOrDefault("limit", 20)))
            {
                Console.WriteLine($"    {unknown.Id}  {unknown.Subject}");
                Console.WriteLine($"        {unknown.Question}");
                Console.WriteLine($"        closed by: {unknown.ResolvedBy}");
            }
        }

        if (unknowns.Count == 0)
        {
            Console.WriteLine("  none");
        }

        return 0;
    }

    /// <summary>
    /// Prints each source's header table side by side.
    /// </summary>
    /// <remarks>
    /// Deliberately a separate command from everything else. Header ids are the one thing that must
    /// never leak into a behavioural spec, so they are looked at here and nowhere else — and the
    /// output states which tables may legitimately be compared, because most of them may not.
    /// </remarks>
    public static int Headers(SpecWorkspace workspace, CommandLine command)
    {
        ResolvedSpecs specs = Load(workspace, command);

        Program.Heading("Revision registries");

        foreach (RevisionRegistry registry in specs.Registries)
        {
            Program.Rows([
                ("registry", registry.Id),
                ("origin", registry.Origin),
                ("authority", registry.Authority.Wire()),
                (
                    "comparable with this emulator",
                    registry.TargetsSameRevision
                        ? "yes — same client build"
                        : "no — different build"
                ),
                ("incoming", registry.Incoming.Count.ToString(CultureInfo.InvariantCulture)),
                ("outgoing", registry.Outgoing.Count.ToString(CultureInfo.InvariantCulture)),
            ]);
            Console.WriteLine();
        }

        string? packet = command.Positional.Count > 0 ? command.Positional[0] : null;

        if (packet is null)
        {
            return 0;
        }

        Program.Heading($"Header ids for {packet}");

        foreach (RevisionRegistry registry in specs.Registries)
        {
            string incoming = registry.Incoming.TryGetValue(packet, out int inId)
                ? inId.ToString(CultureInfo.InvariantCulture)
                : "-";
            string outgoing = registry.Outgoing.TryGetValue(packet, out int outId)
                ? outId.ToString(CultureInfo.InvariantCulture)
                : "-";

            Console.WriteLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "  {0,-40} incoming {1,-8} outgoing {2}",
                    registry.Origin,
                    incoming,
                    outgoing
                )
            );
        }

        Console.WriteLine();
        Console.WriteLine(
            "  Different numbers across registries are different builds, not disagreements."
        );

        return 0;
    }

    private static ResolvedSpecs Load(SpecWorkspace workspace, CommandLine command)
    {
        SpecWorld world = new SpecPipeline(workspace).Scan(Program.Progress(command));
        List<string> notes = [];

        return SpecPipeline.Resolve(world, notes);
    }
}
