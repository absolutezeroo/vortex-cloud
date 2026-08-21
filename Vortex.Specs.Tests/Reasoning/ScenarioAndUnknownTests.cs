using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Vortex.Specs.Analysis.Emulator;
using Vortex.Specs.Captures;
using Vortex.Specs.Model;
using Vortex.Specs.Reasoning;
using Vortex.Specs.Tests.Fixtures;
using Xunit;

namespace Vortex.Specs.Tests.Reasoning;

public class ScenarioAndUnknownTests
{
    private static EvidenceRef VortexEvidence =>
        WorldBuilder.Evidence("vortex", EvidenceAuthority.VortexEmulator);

    private static EmulatorFlow Flow(
        string message,
        IEnumerable<(string Expression, string OnFail)>? checks = null,
        IEnumerable<string>? emits = null,
        bool mutates = false
    ) =>
        new()
        {
            HandlerType = message + "Handler",
            MessageType = message + "Message",
            PrimaryOperation = "DoTheThingAsync",
            Steps =
            [
                new FeatureFlowStep
                {
                    Order = 0,
                    Layer = "handler",
                    Symbol = message + "Handler.HandleAsync",
                    Evidence = VortexEvidence,
                },
                new FeatureFlowStep
                {
                    Order = 1,
                    Layer = "service",
                    Symbol = "SomeService.DoTheThingAsync",
                    Evidence = VortexEvidence,
                },
            ],
            Checks =
            [
                .. (checks ?? []).Select(c => new FeatureCheck
                {
                    Expression = c.Expression,
                    OnFail = c.OnFail,
                    Evidence = VortexEvidence,
                }),
            ],
            Mutations = mutates
                ?
                [
                    new FeatureMutation
                    {
                        Target = "item",
                        Expression = "item.SetPosition(x, y)",
                        Evidence = VortexEvidence,
                    },
                ]
                : [],
            Outgoing =
            [
                .. (emits ?? []).Select(p => new FeatureOutgoing
                {
                    Packet = p,
                    Recipient = Recipient.Actor,
                    RecipientConfidence = Confidence.ImplementationObserved,
                    Evidence = VortexEvidence,
                }),
            ],
        };

    private static (
        IReadOnlyList<FeatureSpec> Features,
        IReadOnlyList<ScenarioSpec> Scenarios
    ) Build(SpecWorld world, IList<string>? notes = null)
    {
        IReadOnlyList<PacketSpec> packets = new PacketResolver().Resolve(world);
        IReadOnlyList<FeatureSpec> features = new FeatureBuilder().Build(world, packets);
        IReadOnlyList<ScenarioSpec> scenarios = new ScenarioGenerator().Generate(
            world,
            features,
            packets,
            notes ?? new List<string>()
        );

        return (features, scenarios);
    }

    [Fact]
    public void Each_observed_guard_becomes_a_scenario_plus_one_for_the_happy_path()
    {
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming: [WorldBuilder.Incoming("MoveObject", ("X", WireType.Int32))],
                flows:
                [
                    Flow(
                        "MoveObject",
                        checks: [("item is null", "return"), ("!hasRights", "return")],
                        emits: ["ObjectUpdate"]
                    ),
                ]
            )
        );

        (IReadOnlyList<FeatureSpec> features, IReadOnlyList<ScenarioSpec> scenarios) = Build(world);

        features.Should().ContainSingle();
        scenarios.Should().HaveCount(3);
        scenarios
            .Should()
            .ContainSingle(s => s.Id.EndsWith(".success", System.StringComparison.Ordinal));
        scenarios
            .Count(s => s.Id.Contains(".guard_", System.StringComparison.Ordinal))
            .Should()
            .Be(2);
    }

    [Fact]
    public void Without_a_capture_every_expected_outcome_stays_unknown()
    {
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming: [WorldBuilder.Incoming("MoveObject", ("X", WireType.Int32))],
                flows:
                [
                    Flow(
                        "MoveObject",
                        checks: [("item is null", "return")],
                        emits: ["ObjectUpdate"]
                    ),
                ]
            )
        );

        (_, IReadOnlyList<ScenarioSpec> scenarios) = Build(world);

        // The emulator returning without answering is a fact about the emulator. What Habbo does is
        // a different question, and nothing here can answer it.
        scenarios.Should().OnlyContain(s => s.Expected == ScenarioOutcome.Unknown);
        scenarios.Should().OnlyContain(s => s.Confidence == Confidence.Unknown);
        scenarios.Should().OnlyContain(s => s.NeedsEvidence != null);
        scenarios.Should().OnlyContain(s => !s.Executable);
    }

    [Fact]
    public void What_each_implementation_does_is_still_recorded_beside_the_unknown()
    {
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming: [WorldBuilder.Incoming("MoveObject", ("X", WireType.Int32))],
                flows: [Flow("MoveObject", emits: ["ObjectUpdate"])]
            ),
            references:
            [
                WorldBuilder.Reference(
                    "arcturus",
                    WorldBuilder.Behaviour(
                        "MoveObject",
                        "arcturus",
                        fields: [("x", WireType.Int32)],
                        emits: ["NotificationDialog", "ObjectUpdate"]
                    )
                ),
            ]
        );

        (_, IReadOnlyList<ScenarioSpec> scenarios) = Build(world);
        ScenarioSpec success = scenarios.Single(s =>
            s.Id.EndsWith(".success", System.StringComparison.Ordinal)
        );

        success.Expected.Should().Be(ScenarioOutcome.Unknown);
        success.Claims.Select(c => c.Origin).Should().Contain(["vortex", "arcturus"]);
        success
            .Claims.Single(c => c.Origin == "arcturus")
            .EmittedPackets.Should()
            .Equal("NotificationDialog", "ObjectUpdate");
    }

    [Fact]
    public void An_official_capture_settles_the_happy_path_and_makes_it_executable()
    {
        EvidenceRef captureEvidence = new()
        {
            Kind = EvidenceKind.Capture,
            Authority = EvidenceAuthority.OfficialCapture,
            Origin = "capture:x",
            Source = "docs/habbo-specs/evidence/captures/x.json",
        };

        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming: [WorldBuilder.Incoming("MoveObject", ("X", WireType.Int32))],
                flows: [Flow("MoveObject", emits: ["ObjectUpdate"])]
            )
        ) with
        {
            TriggerSummaries =
            [
                new TriggerSummary
                {
                    TriggerPacket = "MoveObject",
                    ObservationCount = 3,
                    Sequences = [(new[] { "ObjectUpdate" }, 3)],
                    BestAuthority = EvidenceAuthority.OfficialCapture,
                },
            ],
            Observations =
            [
                new CaptureObservation
                {
                    CaptureId = "x",
                    TriggerPacket = "MoveObject",
                    EmittedPackets = ["ObjectUpdate"],
                    TriggerIndex = 0,
                    Authority = EvidenceAuthority.OfficialCapture,
                    Evidence = captureEvidence,
                },
            ],
        };

        (IReadOnlyList<FeatureSpec> features, IReadOnlyList<ScenarioSpec> scenarios) = Build(world);
        ScenarioSpec success = scenarios.Single(s =>
            s.Id.EndsWith(".success", System.StringComparison.Ordinal)
        );

        success.Expected.Should().Be(ScenarioOutcome.Success);
        success.Confidence.Should().Be(Confidence.CaptureConfirmed);
        success.Executable.Should().BeTrue();
        success.NeedsEvidence.Should().BeNull();

        // And the emission order is only called strict once repeated captures agree on it.
        features[0].OutgoingOrdering.Should().Be("strict");
    }

    [Fact]
    public void A_guard_scenario_stays_unknown_even_when_a_capture_of_the_happy_path_exists()
    {
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming: [WorldBuilder.Incoming("MoveObject", ("X", WireType.Int32))],
                flows:
                [
                    Flow("MoveObject", checks: [("!hasRights", "return")], emits: ["ObjectUpdate"]),
                ]
            )
        ) with
        {
            TriggerSummaries =
            [
                new TriggerSummary
                {
                    TriggerPacket = "MoveObject",
                    ObservationCount = 3,
                    Sequences = [(new[] { "ObjectUpdate" }, 3)],
                    BestAuthority = EvidenceAuthority.OfficialCapture,
                },
            ],
        };

        (_, IReadOnlyList<ScenarioSpec> scenarios) = Build(world);
        ScenarioSpec guard = scenarios.Single(s =>
            s.Id.Contains(".guard_", System.StringComparison.Ordinal)
        );

        // A capture of the request succeeding says nothing about what happens when it is refused.
        guard.Expected.Should().Be(ScenarioOutcome.Unknown);
        guard.NeedsEvidence.Should().Contain("hasRights");
    }

    [Fact]
    public void A_feature_with_more_guards_than_the_cap_reports_what_was_left_out()
    {
        List<(string, string)> many =
        [
            .. Enumerable.Range(0, 30).Select(i => ($"guard{i} is bad", "return")),
        ];

        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming: [WorldBuilder.Incoming("Busy", ("X", WireType.Int32))],
                flows: [Flow("Busy", checks: many)]
            )
        );

        List<string> notes = [];
        Build(world, notes);

        // No silent caps: the report says how many guards were seen and how many became scenarios.
        notes.Should().ContainSingle();
        notes[0].Should().Contain("30 guards observed");
    }

    [Fact]
    public void A_handler_that_reaches_nothing_is_recorded_as_a_critical_unknown()
    {
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming: [WorldBuilder.Incoming("Stubbed", ("X", WireType.Int32))],
                flows:
                [
                    new EmulatorFlow
                    {
                        HandlerType = "StubbedMessageHandler",
                        MessageType = "StubbedMessage",
                        Steps =
                        [
                            new FeatureFlowStep
                            {
                                Order = 0,
                                Layer = "handler",
                                Symbol = "StubbedMessageHandler.HandleAsync",
                                Evidence = VortexEvidence,
                            },
                        ],
                    },
                ]
            )
        );

        IReadOnlyList<PacketSpec> packets = new PacketResolver().Resolve(world);
        IReadOnlyList<FeatureSpec> features = new FeatureBuilder().Build(world, packets);
        IReadOnlyList<ScenarioSpec> scenarios = new ScenarioGenerator().Generate(
            world,
            features,
            packets,
            new List<string>()
        );

        IReadOnlyList<UnknownSpec> unknowns = new UnknownCollector().Collect(
            world,
            packets,
            features,
            scenarios
        );

        unknowns
            .Should()
            .Contain(u =>
                u.Severity == UnknownSeverity.Critical
                && u.Question.Contains("Stubbed", System.StringComparison.Ordinal)
            );
    }

    [Fact]
    public void A_state_changing_feature_with_unanswered_guards_asks_for_the_capture_that_would_settle_it()
    {
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming: [WorldBuilder.Incoming("MoveObject", ("X", WireType.Int32))],
                flows:
                [
                    Flow(
                        "MoveObject",
                        checks: [("!hasRights", "return")],
                        emits: ["ObjectUpdate"],
                        mutates: true
                    ),
                ]
            )
        );

        IReadOnlyList<PacketSpec> packets = new PacketResolver().Resolve(world);
        IReadOnlyList<FeatureSpec> features = new FeatureBuilder().Build(world, packets);
        IReadOnlyList<ScenarioSpec> scenarios = new ScenarioGenerator().Generate(
            world,
            features,
            packets,
            new List<string>()
        );

        UnknownSpec unknown = new UnknownCollector()
            .Collect(world, packets, features, scenarios)
            .First(u => u.Subject.Contains("refuses", System.StringComparison.Ordinal));

        unknown.Severity.Should().Be(UnknownSeverity.Medium);
        unknown.ResolvedBy.Should().Contain("capture");
    }

    [Fact]
    public void Unnamed_fields_are_recorded_as_a_low_severity_question()
    {
        SpecWorld world = WorldBuilder.World(
            clients:
            [
                WorldBuilder.Client(
                    "official",
                    EvidenceAuthority.ClientCode,
                    sameRevision: true,
                    WorldBuilder.Packet(
                        "Mystery",
                        PacketDirection.Outgoing,
                        EvidenceAuthority.ClientCode,
                        "official",
                        fields: [(null, WireType.Int32)]
                    )
                ),
            ]
        );

        IReadOnlyList<PacketSpec> packets = new PacketResolver().Resolve(world);

        new UnknownCollector()
            .Collect(world, packets, [], [])
            .Should()
            .Contain(u =>
                u.Severity == UnknownSeverity.Low
                && u.Subject.Contains("Mystery", System.StringComparison.Ordinal)
            );
    }
}
