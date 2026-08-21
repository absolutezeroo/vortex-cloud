using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Specs.Captures;
using Vortex.Specs.Model;
using Vortex.Specs.Yaml;

namespace Vortex.Specs.Persistence;

/// <summary>
/// Turns spec records into the YAML document tree.
/// </summary>
/// <remarks>
/// Key order here is the on-disk order, and it is chosen for reading: what the thing is, then what is
/// known about it, then how well that is known, then where it came from. Confidence sits next to the
/// claim it qualifies rather than in a block at the bottom, because a reader who skims should not be
/// able to pick up a field name without also picking up how much it is worth.
/// </remarks>
public static class SpecWriter
{
    public static YamlMapping Packet(PacketSpec packet)
    {
        YamlMapping body = YamlNode
            .Mapping()
            .Set("name", packet.Name)
            .Set(
                "direction",
                packet.Direction == PacketDirection.Incoming ? "incoming" : "outgoing"
            )
            .Set("domain", packet.Domain)
            .Set("structure_confidence", packet.StructureConfidence.Wire())
            .Set("mapped_in_vortex", packet.MappedInVortex);

        body.SetIfPresent("vortex_handler", packet.VortexHandler);
        body.Set("fields", YamlNode.Sequence(packet.Fields.Select(Field)));

        if (packet.Observations.Count > 0)
        {
            body.Set(
                "layouts_by_source",
                YamlNode.Sequence(packet.Observations.Select(LayoutObservation))
            );
        }

        body.Set("evidence", YamlNode.Sequence(packet.Evidence.Select(Evidence)));
        body.SetIfAny(
            "conflicts",
            YamlNode.Sequence(packet.ConflictIds.Select(c => YamlNode.Scalar(c)))
        );
        body.SetIfAny(
            "unknowns",
            YamlNode.Sequence(packet.UnknownIds.Select(u => YamlNode.Scalar(u)))
        );

        return body;
    }

    private static YamlMapping Field(PacketFieldSpec field)
    {
        YamlMapping node = YamlNode
            .Mapping()
            .Set("index", field.Index)
            .Set("name", field.Name)
            .Set("type", field.Type.Wire());

        if (field.IsPlaceholderName)
        {
            // Said out loud rather than left to be inferred from the name shape: a reader who does
            // not know the convention would otherwise read unknown_3 as a field called "unknown_3".
            node.Set("name_is_placeholder", true);
        }

        node.Set("name_confidence", field.NameConfidence.Wire());
        node.Set("type_confidence", field.TypeConfidence.Wire());
        node.SetIfPresent("semantic_type", field.SemanticType);
        node.SetIfPresent("note", field.Note);
        node.SetIfAny(
            "evidence",
            YamlNode.Sequence(field.EvidenceIds.Select(e => YamlNode.Scalar(e)))
        );

        if (field.Children.Count > 0)
        {
            node.Set("fields", YamlNode.Sequence(field.Children.Select(Field)));
        }

        return node;
    }

    private static YamlMapping LayoutObservation(PacketLayoutObservation observation)
    {
        YamlMapping node = YamlNode
            .Mapping()
            .Set("origin", observation.Origin)
            .Set("authority", observation.Authority.Wire())
            .Set("field_count", observation.Fields.Count)
            .Set("partial", observation.IsPartial)
            .Set(
                "types",
                YamlNode.Sequence(observation.Fields.Select(f => YamlNode.Scalar(f.Type.Wire())))
            )
            .Set("evidence", observation.Evidence.Id);

        return node;
    }

    public static YamlMapping Evidence(EvidenceRef evidence)
    {
        YamlMapping node = YamlNode
            .Mapping()
            .Set("id", evidence.Id)
            .Set("kind", ToWire(evidence.Kind))
            .Set("authority", evidence.Authority.Wire())
            .Set("origin", evidence.Origin)
            .Set("source", evidence.Source);

        node.SetIfPresent("symbol", evidence.Symbol);

        if (evidence.Line is int line)
        {
            node.Set("line", line);
        }

        node.SetIfPresent("note", evidence.Note);

        return node;
    }

    public static YamlMapping Feature(FeatureSpec feature)
    {
        YamlMapping body = YamlNode
            .Mapping()
            .Set("id", feature.Id)
            .Set("domain", feature.Domain)
            .Set("title", feature.Title)
            .Set(
                "trigger_packets",
                YamlNode.Sequence(feature.TriggerPackets.Select(t => YamlNode.Scalar(t)))
            );

        body.Set(
            "observed_in",
            YamlNode
                .Mapping()
                .Set("vortex", feature.ObservedInVortex)
                .Set(
                    "references",
                    YamlNode.Sequence(feature.ObservedInReferences.Select(r => YamlNode.Scalar(r)))
                )
        );

        // The distinction this whole system exists for. `observed_in` is what implementations do;
        // `official_behavior` is what Habbo does, and it stays unknown until a capture says otherwise.
        body.Set(
            "official_behavior",
            YamlNode.Mapping().Set("status", feature.OfficialBehaviourConfidence.Wire())
        );

        body.Set(
            "flow",
            YamlNode.Sequence(
                feature.Flow.Select(step =>
                    YamlNode
                        .Mapping()
                        .Set("order", step.Order)
                        .Set("layer", step.Layer)
                        .Set("symbol", step.Symbol)
                        .Set("evidence", step.Evidence.Id)
                )
            )
        );

        body.Set(
            "checks",
            YamlNode.Sequence(
                feature.Checks.Select(check =>
                    YamlNode
                        .Mapping()
                        .Set("expression", check.Expression)
                        .Set("on_fail", check.OnFail)
                        .Set("observed_in", "vortex")
                        .Set("evidence", check.Evidence.Id)
                )
            )
        );

        body.Set(
            "state_changes",
            YamlNode.Sequence(
                feature.Mutations.Select(mutation =>
                    YamlNode
                        .Mapping()
                        .Set("target", mutation.Target)
                        .Set("value", mutation.Expression)
                        .Set("observed_in", "vortex")
                        .Set("evidence", mutation.Evidence.Id)
                )
            )
        );

        body.Set(
            "outgoing",
            YamlNode
                .Mapping()
                .Set("ordering", feature.OutgoingOrdering)
                .Set(
                    "packets",
                    YamlNode.Sequence(
                        feature.Outgoing.Select(outgoing =>
                        {
                            YamlMapping node = YamlNode
                                .Mapping()
                                .Set("packet", outgoing.Packet)
                                .Set("recipient", outgoing.Recipient.Wire())
                                .Set("recipient_confidence", outgoing.RecipientConfidence.Wire());

                            if (outgoing.Order is int order)
                            {
                                node.Set("order", order);
                            }

                            node.SetIfPresent("note", outgoing.Note);

                            return node.Set("evidence", outgoing.Evidence.Id);
                        })
                    )
                )
        );

        body.Set("reaches_persistence", feature.ReachesPersistence);
        body.Set("evidence", YamlNode.Sequence(feature.Evidence.Select(Evidence)));
        body.SetIfAny(
            "scenarios",
            YamlNode.Sequence(feature.ScenarioIds.Select(s => YamlNode.Scalar(s)))
        );
        body.SetIfAny(
            "conflicts",
            YamlNode.Sequence(feature.ConflictIds.Select(c => YamlNode.Scalar(c)))
        );
        body.SetIfAny(
            "unknowns",
            YamlNode.Sequence(feature.UnknownIds.Select(u => YamlNode.Scalar(u)))
        );

        return body;
    }

    public static YamlMapping Scenarios(string featureId, IReadOnlyList<ScenarioSpec> scenarios) =>
        YamlNode
            .Mapping()
            .Set("feature", featureId)
            .Set("scenarios", YamlNode.Sequence(scenarios.Select(Scenario)))
            .Set(
                "evidence",
                YamlNode.Sequence(
                    scenarios
                        .SelectMany(scenario => scenario.Claims.Select(claim => claim.Evidence))
                        .DistinctBy(e => e.Id)
                        .Order(EvidenceRef.ByAuthorityThenId.Instance)
                        .Select(Evidence)
                )
            );

    private static YamlMapping Scenario(ScenarioSpec scenario)
    {
        YamlMapping given = YamlNode.Mapping();

        foreach (
            KeyValuePair<string, string> entry in scenario.Given.OrderBy(
                e => e.Key,
                StringComparer.Ordinal
            )
        )
        {
            given.Set(entry.Key, entry.Value);
        }

        YamlMapping node = YamlNode
            .Mapping()
            .Set("scenario", scenario.Id)
            .Set("title", scenario.Title)
            .Set("given", given)
            .Set("when", YamlNode.Mapping().Set("packet", scenario.WhenPacket))
            .Set(
                "expected",
                YamlNode
                    .Mapping()
                    .Set("status", scenario.Expected.Wire())
                    .Set("confidence", scenario.Confidence.Wire())
            );

        node.SetIfPresent("needs_evidence", scenario.NeedsEvidence);
        node.Set("executable", scenario.Executable);

        node.Set(
            "claims",
            YamlNode.Sequence(
                scenario.Claims.Select(claim =>
                    YamlNode
                        .Mapping()
                        .Set("origin", claim.Origin)
                        .Set("authority", claim.Authority.Wire())
                        .Set("outcome", claim.Outcome.Wire())
                        .Set(
                            "emits",
                            YamlNode.Sequence(claim.EmittedPackets.Select(p => YamlNode.Scalar(p)))
                        )
                        .Set("evidence", claim.Evidence.Id)
                )
            )
        );

        return node;
    }

    public static YamlMapping Conflict(ConflictSpec conflict)
    {
        YamlMapping body = YamlNode
            .Mapping()
            .Set("id", conflict.Id)
            .Set("kind", ToWire(conflict.Kind))
            .Set("subject", conflict.Subject);

        body.SetIfPresent("packet", conflict.PacketName);
        body.SetIfPresent("feature", conflict.FeatureId);

        body.Set(
            "positions",
            YamlNode.Sequence(
                conflict.Positions.Select(position =>
                    YamlNode
                        .Mapping()
                        .Set("origin", position.Origin)
                        .Set("authority", position.Authority.Wire())
                        .Set("claim", position.Claim)
                        .Set("evidence", position.Evidence.Id)
                )
            )
        );

        body.Set(
            "evidence",
            YamlNode.Sequence(
                conflict
                    .Positions.Select(position => position.Evidence)
                    .DistinctBy(e => e.Id)
                    .Order(EvidenceRef.ByAuthorityThenId.Instance)
                    .Select(Evidence)
            )
        );

        // Never filled in by the generator. A conflict is closed by a person writing evidence into
        // the verified block, not by the tool deciding which implementation it likes.
        body.Set("official", YamlNode.Mapping().Set("status", conflict.OfficialStatus.Wire()));
        body.SetIfPresent("resolution", conflict.Resolution);

        return body;
    }

    public static YamlMapping Unknown(UnknownSpec unknown)
    {
        YamlMapping body = YamlNode
            .Mapping()
            .Set("id", unknown.Id)
            .Set("subject", unknown.Subject)
            .Set("severity", unknown.Severity.ToString().ToLowerInvariant())
            .Set("question", unknown.Question)
            .Set("resolved_by", unknown.ResolvedBy);

        body.SetIfPresent("packet", unknown.PacketName);
        body.SetIfPresent("feature", unknown.FeatureId);
        body.Set("known_evidence", YamlNode.Sequence(unknown.KnownEvidence.Select(Evidence)));

        return body;
    }

    public static YamlMapping Registry(RevisionRegistry registry)
    {
        YamlMapping incoming = YamlNode.Mapping();
        YamlMapping outgoing = YamlNode.Mapping();

        foreach (
            KeyValuePair<string, int> entry in registry.Incoming.OrderBy(
                e => e.Key,
                StringComparer.Ordinal
            )
        )
        {
            incoming.Set(entry.Key, entry.Value);
        }

        foreach (
            KeyValuePair<string, int> entry in registry.Outgoing.OrderBy(
                e => e.Key,
                StringComparer.Ordinal
            )
        )
        {
            outgoing.Set(entry.Key, entry.Value);
        }

        return YamlNode
            .Mapping()
            .Set("revision", registry.Id)
            .Set("origin", registry.Origin)
            .Set("authority", registry.Authority.Wire())
            .Set("targets_same_revision_as_emulator", registry.TargetsSameRevision)
            .Set("evidence", Evidence(registry.Evidence))
            .Set("incoming", incoming)
            .Set("outgoing", outgoing);
    }

    public static YamlMapping CaptureObservations(
        CaptureDocument capture,
        IReadOnlyList<CaptureObservation> observations
    )
    {
        YamlMapping body = YamlNode
            .Mapping()
            .Set("capture", capture.Id)
            .Set("source", capture.Source.ToString().ToLowerInvariant())
            .Set("authority", capture.Authority.Wire())
            .Set("message_count", capture.Messages.Count);

        body.SetIfPresent("revision", capture.Revision);
        body.SetIfPresent("recorded_utc", capture.RecordedUtc);
        body.SetIfPresent("note", capture.Note);

        body.Set(
            "observations",
            YamlNode.Sequence(
                observations.Select(observation =>
                    YamlNode
                        .Mapping()
                        .Set("trigger", observation.TriggerPacket)
                        .Set("trigger_index", observation.TriggerIndex)
                        .Set(
                            "emitted",
                            YamlNode.Sequence(
                                observation.EmittedPackets.Select(p => YamlNode.Scalar(p))
                            )
                        )
                        .Set("evidence", observation.Evidence.Id)
                )
            )
        );

        return body;
    }

    private static string ToWire(EvidenceKind kind) =>
        Naming.PacketNaming.SnakeCase(kind.ToString());

    private static string ToWire(ConflictKind kind) =>
        Naming.PacketNaming.SnakeCase(kind.ToString());
}
