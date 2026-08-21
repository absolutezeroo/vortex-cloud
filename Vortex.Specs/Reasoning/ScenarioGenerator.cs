using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Vortex.Specs.Analysis.Reference;
using Vortex.Specs.Captures;
using Vortex.Specs.Model;
using Vortex.Specs.Naming;

namespace Vortex.Specs.Reasoning;

/// <summary>
/// Turns each feature into the scenarios worth knowing the answer to.
/// </summary>
/// <remarks>
/// One scenario per observed guard plus the happy path. The guards are real code, so the scenarios
/// are real questions rather than a generic checklist — and almost all of them come out
/// <c>unknown</c>, because a guard tells you this emulator refuses something and says nothing about
/// what Habbo sends back when it refuses. That is the correct output: a scenario marked unknown with
/// a note saying which capture would settle it is a task, where a scenario filled in from the
/// emulator's own behaviour is a fiction that will be read as fact.
/// </remarks>
public sealed class ScenarioGenerator
{
    private const int MaxGuardScenariosPerFeature = 12;

    public IReadOnlyList<ScenarioSpec> Generate(
        SpecWorld world,
        IReadOnlyList<FeatureSpec> features,
        IReadOnlyList<PacketSpec> packets,
        IList<string> notes
    )
    {
        List<ScenarioSpec> scenarios = [];

        foreach (FeatureSpec feature in features)
        {
            string trigger =
                feature.TriggerPackets.Count > 0 ? feature.TriggerPackets[0] : "unknown";

            scenarios.Add(SuccessScenario(feature, trigger, world));

            List<FeatureCheck> guards = [.. feature.Checks];

            if (guards.Count > MaxGuardScenariosPerFeature)
            {
                notes.Add(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}: {1} guards observed, {2} turned into scenarios; the rest are listed in the feature spec's checks",
                        feature.Id,
                        guards.Count,
                        MaxGuardScenariosPerFeature
                    )
                );

                guards = [.. guards.Take(MaxGuardScenariosPerFeature)];
            }

            for (int i = 0; i < guards.Count; i++)
            {
                scenarios.Add(GuardScenario(feature, trigger, guards[i], i, world));
            }
        }

        return
        [
            .. scenarios
                .OrderBy(s => s.FeatureId, StringComparer.Ordinal)
                .ThenBy(s => s.Id, StringComparer.Ordinal),
        ];
    }

    private static ScenarioSpec SuccessScenario(
        FeatureSpec feature,
        string trigger,
        SpecWorld world
    )
    {
        List<ScenarioClaim> claims = [.. Claims(feature, trigger, world)];

        TriggerSummary? captured = world.TriggerSummaries.FirstOrDefault(s =>
            string.Equals(s.TriggerPacket, trigger, StringComparison.Ordinal)
        );

        // Only a capture of an official server settles the happy path. Anything less leaves the
        // outcome open with every implementation's answer recorded beside it.
        bool settled = captured is { BestAuthority: EvidenceAuthority.OfficialCapture };

        return new ScenarioSpec
        {
            Id = $"{feature.Id}.success",
            FeatureId = feature.Id,
            Title = $"{feature.Title}: the request is accepted",
            Given = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["preconditions"] = "all guards satisfied",
            },
            WhenPacket = trigger,
            Expected = settled ? ScenarioOutcome.Success : ScenarioOutcome.Unknown,
            Confidence = settled ? Confidence.CaptureConfirmed : Confidence.Unknown,
            Claims = claims,
            NeedsEvidence = settled
                ? null
                : $"an official capture of {trigger} succeeding, with every packet the server sends back and in what order",
            Executable = settled,
        };
    }

    private static ScenarioSpec GuardScenario(
        FeatureSpec feature,
        string trigger,
        FeatureCheck guard,
        int index,
        SpecWorld world
    )
    {
        List<ScenarioClaim> claims =
        [
            new ScenarioClaim
            {
                Origin = "vortex",
                Authority = EvidenceAuthority.VortexEmulator,
                Outcome = guard.OnFail switch
                {
                    "throw" => ScenarioOutcome.Rejected,
                    "send_and_return" => ScenarioOutcome.Rejected,
                    _ => ScenarioOutcome.Ignored,
                },
                EmittedPackets =
                    guard.OnFail == "send_and_return"
                        ?
                        [
                            .. feature
                                .Outgoing.Select(o => o.Packet)
                                .Distinct(StringComparer.Ordinal),
                        ]
                        : [],
                Evidence = guard.Evidence,
            },
        ];

        claims.AddRange(ReferenceClaims(trigger, world));

        return new ScenarioSpec
        {
            Id = $"{feature.Id}.guard_{index.ToString(CultureInfo.InvariantCulture)}",
            FeatureId = feature.Id,
            Title = $"{feature.Title}: {Describe(guard)}",
            Given = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["guard"] = guard.Expression,
                ["guard_holds"] = "true",
            },
            WhenPacket = trigger,
            // Never derived from the guard. That this emulator returns without answering is a fact
            // about this emulator; the official server may answer, and only a capture can say.
            Expected = ScenarioOutcome.Unknown,
            Confidence = Confidence.Unknown,
            Claims = claims,
            NeedsEvidence =
                $"an official capture of {trigger} sent while '{guard.Expression}' holds: does the server reply, stay silent, or disconnect?",
            Executable = false,
        };
    }

    private static IEnumerable<ScenarioClaim> Claims(
        FeatureSpec feature,
        string trigger,
        SpecWorld world
    )
    {
        yield return new ScenarioClaim
        {
            Origin = "vortex",
            Authority = EvidenceAuthority.VortexEmulator,
            Outcome =
                feature.Outgoing.Count > 0 ? ScenarioOutcome.Success : ScenarioOutcome.Ignored,
            EmittedPackets =
            [
                .. feature.Outgoing.Select(o => o.Packet).Distinct(StringComparer.Ordinal),
            ],
            Evidence =
                feature.Evidence.Count > 0 ? feature.Evidence[0] : world.Emulator.Registry.Evidence,
        };

        foreach (ScenarioClaim claim in ReferenceClaims(trigger, world))
        {
            yield return claim;
        }

        foreach (
            TriggerSummary summary in world.TriggerSummaries.Where(s =>
                string.Equals(s.TriggerPacket, trigger, StringComparison.Ordinal)
            )
        )
        {
            yield return new ScenarioClaim
            {
                Origin = $"capture ({summary.ObservationCount} observations)",
                Authority = summary.BestAuthority,
                Outcome = ScenarioOutcome.Success,
                EmittedPackets = summary.Sequences.Count > 0 ? summary.Sequences[0].Sequence : [],
                Evidence = new EvidenceRef
                {
                    Kind = EvidenceKind.Capture,
                    Authority = summary.BestAuthority,
                    Origin = "capture",
                    Source = "docs/habbo-specs/evidence/captures",
                    Symbol = summary.TriggerPacket,
                },
            };
        }
    }

    private static IEnumerable<ScenarioClaim> ReferenceClaims(string trigger, SpecWorld world)
    {
        foreach (ReferenceScan reference in world.References)
        {
            ReferenceBehaviour? behaviour = reference.Behaviours.FirstOrDefault(b =>
                string.Equals(b.Canonical, trigger, StringComparison.Ordinal)
            );

            if (behaviour is null)
            {
                continue;
            }

            yield return new ScenarioClaim
            {
                Origin = reference.Origin,
                Authority = reference.Authority,
                Outcome =
                    behaviour.Outgoing.Count > 0
                        ? ScenarioOutcome.Success
                        : ScenarioOutcome.Ignored,
                EmittedPackets = [.. behaviour.Outgoing.Select(o => o.Packet)],
                Evidence = behaviour.Evidence,
            };
        }
    }

    private static string Describe(FeatureCheck guard)
    {
        string condition =
            guard.Expression.Length > 60 ? guard.Expression[..57] + "..." : guard.Expression;

        return $"the guard '{condition}' holds";
    }

    /// <summary>
    /// The scenario names the task description asked for, derived from a feature's own guards rather
    /// than from a fixed list, so a feature with unusual preconditions gets scenarios about them.
    /// </summary>
    public static string SuggestedName(string featureId, string suffix) =>
        $"{PacketNaming.SnakeCase(featureId.Replace('.', '_'))}_{suffix}";
}
