using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Vortex.Specs.Model;
using Vortex.Specs.Pipeline;
using Vortex.Specs.Reasoning;
using Vortex.Specs.Sources;

namespace Vortex.Specs.Cli.Commands;

/// <summary>
/// Answers "what is known about this, and what is not" for one feature or packet.
/// </summary>
/// <remarks>
/// This is the command an agent runs before touching protocol behaviour, so the shape of the output
/// is the shape of the decision it has to make: what the client fixes, what this emulator currently
/// does, what other implementations do, where they disagree, and what nobody knows. The unknowns are
/// printed last and never omitted — an empty unknowns section means the question was asked and came
/// back clean, which is different from the section not being there.
/// </remarks>
public static class AnalyzeCommand
{
    public static int Run(SpecWorkspace workspace, CommandLine command)
    {
        if (command.Positional.Count == 0)
        {
            Console.Error.WriteLine(
                "analyze needs a feature id or packet name, e.g. room.move_floor_item_in_room"
            );
            return 2;
        }

        string target = command.Positional[0];
        SpecWorld world = new SpecPipeline(workspace).Scan(Program.Progress(command));
        List<string> notes = [];
        ResolvedSpecs specs = SpecPipeline.Resolve(world, notes);

        FeatureSpec? feature = specs.Features.FirstOrDefault(f =>
            string.Equals(f.Id, target, StringComparison.OrdinalIgnoreCase)
        );

        if (feature is not null)
        {
            return PrintFeature(feature, specs);
        }

        List<PacketSpec> packets =
        [
            .. specs.Packets.Where(p =>
                string.Equals(p.Name, target, StringComparison.OrdinalIgnoreCase)
            ),
        ];

        if (packets.Count > 0)
        {
            foreach (PacketSpec packet in packets)
            {
                PrintPacket(packet, specs);
            }

            return 0;
        }

        Console.Error.WriteLine($"Nothing named '{target}'. Closest matches:");

        foreach (
            string candidate in specs
                .Features.Select(f => f.Id)
                .Concat(specs.Packets.Select(p => p.Name))
                .Where(n => n.Contains(target, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(n => n, StringComparer.Ordinal)
                .Take(15)
        )
        {
            Console.Error.WriteLine($"  {candidate}");
        }

        return 2;
    }

    private static int PrintFeature(FeatureSpec feature, ResolvedSpecs specs)
    {
        Program.Heading($"Feature {feature.Id}");
        Program.Rows([
            ("domain", feature.Domain),
            ("triggers", string.Join(", ", feature.TriggerPackets)),
            ("observed in vortex", feature.ObservedInVortex ? "yes" : "no — the handler is a stub"),
            (
                "observed in references",
                feature.ObservedInReferences.Count == 0
                    ? "none"
                    : string.Join(", ", feature.ObservedInReferences)
            ),
            ("official behaviour", feature.OfficialBehaviourConfidence.Wire()),
            ("reaches persistence", feature.ReachesPersistence ? "yes" : "no"),
        ]);

        foreach (string trigger in feature.TriggerPackets)
        {
            PacketSpec? packet = specs.Packets.FirstOrDefault(p =>
                p.Direction == PacketDirection.Incoming
                && string.Equals(p.Name, trigger, StringComparison.Ordinal)
            );

            if (packet is null)
            {
                continue;
            }

            Program.Heading(
                $"Incoming {packet.Name} — structure {packet.StructureConfidence.Wire()}"
            );
            PrintFields(packet.Fields, indent: 2);
        }

        if (feature.Flow.Count > 0)
        {
            Program.Heading("Current implementation");

            foreach (FeatureFlowStep step in feature.Flow)
            {
                Console.WriteLine($"  {step.Order, 2}. [{step.Layer}] {step.Symbol}");
            }
        }

        if (feature.Checks.Count > 0)
        {
            Program.Heading($"Guards observed in vortex ({feature.Checks.Count})");

            foreach (FeatureCheck check in feature.Checks.Take(20))
            {
                Console.WriteLine($"  if ({check.Expression}) -> {check.OnFail}");
            }
        }

        if (feature.Mutations.Count > 0)
        {
            Program.Heading($"State changes observed in vortex ({feature.Mutations.Count})");

            foreach (FeatureMutation mutation in feature.Mutations.Take(20))
            {
                Console.WriteLine($"  {mutation.Target} = {mutation.Expression}");
            }
        }

        Program.Heading($"Outgoing (ordering: {feature.OutgoingOrdering})");

        foreach (FeatureOutgoing outgoing in feature.Outgoing)
        {
            Console.WriteLine(
                $"  {outgoing.Packet} -> {outgoing.Recipient.Wire()} ({outgoing.RecipientConfidence.Wire()})"
            );
        }

        if (feature.Outgoing.Count == 0)
        {
            Console.WriteLine("  nothing");
        }

        PrintConflicts(feature.ConflictIds, specs);
        PrintScenarios(feature, specs);
        PrintUnknowns(feature.UnknownIds, specs);

        Console.WriteLine();
        Console.WriteLine(
            "Existing emulator behaviour is evidence, not authority. Anything above marked unknown"
        );
        Console.WriteLine("must stay unknown until a capture or the official client settles it.");

        return 0;
    }

    private static void PrintPacket(PacketSpec packet, ResolvedSpecs specs)
    {
        string direction = packet.Direction == PacketDirection.Incoming ? "Incoming" : "Outgoing";
        Program.Heading($"{direction} {packet.Name}");
        Program.Rows([
            ("domain", packet.Domain),
            ("structure confidence", packet.StructureConfidence.Wire()),
            ("mapped in vortex", packet.MappedInVortex ? "yes" : "no"),
            ("handler", packet.VortexHandler ?? "none"),
        ]);

        Console.WriteLine();
        PrintFields(packet.Fields, indent: 2);

        Program.Heading("Layout by source");

        foreach (PacketLayoutObservation observation in packet.Observations)
        {
            Console.WriteLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "  {0,-28} {1,-22} {2} fields{3}",
                    observation.Origin,
                    observation.Authority.Wire(),
                    observation.Fields.Count,
                    observation.IsPartial
                        ? " (partial — the reader could not follow it all)"
                        : string.Empty
                )
            );
        }

        PrintConflicts(packet.ConflictIds, specs);
        PrintUnknowns(packet.UnknownIds, specs);
    }

    private static void PrintFields(IReadOnlyList<PacketFieldSpec> fields, int indent)
    {
        string pad = new(' ', indent);

        foreach (PacketFieldSpec field in fields)
        {
            Console.WriteLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}{1,2}. {2,-26} {3,-8} name:{4} type:{5}{6}",
                    pad,
                    field.Index,
                    field.Name,
                    field.Type.Wire(),
                    field.NameConfidence.Wire(),
                    field.TypeConfidence.Wire(),
                    field.Note is null ? string.Empty : "  // " + field.Note
                )
            );

            if (field.Children.Count > 0)
            {
                PrintFields(field.Children, indent + 4);
            }
        }

        if (fields.Count == 0)
        {
            Console.WriteLine($"{pad}(no layout could be read from any source)");
        }
    }

    private static void PrintConflicts(IReadOnlyList<string> ids, ResolvedSpecs specs)
    {
        Program.Heading($"Conflicts ({ids.Count})");

        foreach (string id in ids)
        {
            ConflictSpec? conflict = specs.Conflicts.FirstOrDefault(c =>
                string.Equals(c.Id, id, StringComparison.Ordinal)
            );

            if (conflict is null)
            {
                continue;
            }

            Console.WriteLine($"  {conflict.Id}  {conflict.Subject}");

            foreach (ConflictPosition position in conflict.Positions)
            {
                Console.WriteLine(
                    $"      {position.Origin} ({position.Authority.Wire()}): {position.Claim}"
                );
            }

            Console.WriteLine($"      official: {conflict.OfficialStatus.Wire()}");
        }

        if (ids.Count == 0)
        {
            Console.WriteLine("  none");
        }
    }

    private static void PrintUnknowns(IReadOnlyList<string> ids, ResolvedSpecs specs)
    {
        Program.Heading($"Unknown behaviours ({ids.Count})");

        foreach (string id in ids)
        {
            UnknownSpec? unknown = specs.Unknowns.FirstOrDefault(u =>
                string.Equals(u.Id, id, StringComparison.Ordinal)
            );

            if (unknown is null)
            {
                continue;
            }

            Console.WriteLine(
                $"  [{unknown.Severity.ToString().ToLowerInvariant()}] {unknown.Question}"
            );
            Console.WriteLine($"      closed by: {unknown.ResolvedBy}");
        }

        if (ids.Count == 0)
        {
            Console.WriteLine("  none recorded");
        }
    }

    private static void PrintScenarios(FeatureSpec feature, ResolvedSpecs specs)
    {
        List<ScenarioSpec> scenarios =
        [
            .. specs.Scenarios.Where(s =>
                string.Equals(s.FeatureId, feature.Id, StringComparison.Ordinal)
            ),
        ];

        Program.Heading($"Scenarios ({scenarios.Count})");

        foreach (ScenarioSpec scenario in scenarios)
        {
            Console.WriteLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "  {0,-52} expected: {1} ({2})",
                    scenario.Id,
                    scenario.Expected.Wire(),
                    scenario.Confidence.Wire()
                )
            );
        }
    }
}
