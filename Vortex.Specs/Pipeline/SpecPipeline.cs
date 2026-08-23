using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vortex.Specs.Analysis.Client;
using Vortex.Specs.Analysis.Emulator;
using Vortex.Specs.Analysis.Reference;
using Vortex.Specs.Captures;
using Vortex.Specs.Model;
using Vortex.Specs.Reasoning;
using Vortex.Specs.Sources;

namespace Vortex.Specs.Pipeline;

/// <summary>Which C# projects the emulator scan indexes. Kept out of the analyzer so it stays data.</summary>
public static class EmulatorProjects
{
    /// <summary>
    /// The protocol layer plus the domain modules a handler can reach. Deliberately not the whole
    /// solution: the dashboard, benchmarks and test projects contribute nothing to protocol truth and
    /// indexing them only makes every run slower.
    /// </summary>
    public static readonly string[] Default =
    [
        "Vortex.Revisions",
        "Vortex.PacketHandlers",
        "Vortex.Primitives",
        "Vortex.Protocol",
        "Vortex.Messages",
        "Vortex.Rooms",
        "Vortex.Players",
        "Vortex.Collectibles",
        "Vortex.Social",
        "Vortex.Catalog",
        "Vortex.Inventory",
        "Vortex.Navigator",
        "Vortex.Furniture",
        "Vortex.Marketplace",
        "Vortex.Authentication",
        "Vortex.Events",
    ];
}

/// <summary>
/// Runs every analyzer and assembles the world they collectively describe.
/// </summary>
/// <remarks>
/// Split from the writer on purpose: scanning is expensive and pure, writing is cheap and touches
/// disk. Every command in the CLI that needs facts calls <see cref="Scan"/>; only the ones that
/// persist go on to the writer.
/// </remarks>
public sealed class SpecPipeline(SpecWorkspace workspace)
{
    public SpecWorkspace Workspace { get; } = workspace;

    public SpecWorld Scan(Action<string>? progress = null)
    {
        List<string> problems = [];

        progress?.Invoke("indexing the emulator's C#");
        CSharpSourceIndex index = CSharpSourceIndex.Build(
            Workspace.RepositoryRoot,
            EmulatorProjects.Default
        );

        progress?.Invoke(
            $"reading {index.Files.Count} files for parsers, serializers and handlers"
        );
        EmulatorScan emulator = new EmulatorAnalyzer(Workspace, index).Scan();

        List<ClientScan> clients = [];

        foreach (SourceTree tree in Workspace.Clients)
        {
            IClientAnalyzer analyzer =
                tree.Kind == SourceTreeKind.OfficialClient
                    ? new As3ClientAnalyzer(Workspace, tree, emulator.Revision)
                    : new NitroClientAnalyzer(Workspace, tree);

            progress?.Invoke($"reading client {tree.Id}");

            try
            {
                ClientScan scan = analyzer.Scan();
                clients.Add(scan);
                problems.AddRange(scan.Unresolved.Select(u => $"{scan.Origin}: {u}"));
            }
            catch (IOException error)
            {
                // A tree that cannot be read degrades the output; it does not invalidate the rest.
                problems.Add($"{analyzer.Origin}: could not be read ({error.Message})");
            }
        }

        List<ReferenceScan> references = [];

        foreach (SourceTree tree in Workspace.References)
        {
            progress?.Invoke($"reading reference {tree.Id}");

            try
            {
                ReferenceScan scan = new ArcturusReferenceAnalyzer(Workspace, tree).Scan();
                references.Add(scan);
                problems.AddRange(scan.Unresolved.Select(u => $"{scan.Origin}: {u}"));
            }
            catch (IOException error)
            {
                problems.Add($"{tree.Id}: could not be read ({error.Message})");
            }
        }

        progress?.Invoke("importing captures");
        CaptureImporter importer = new();
        IReadOnlyList<CaptureDocument> captures = importer.ReadAll(Workspace.CaptureRoot, problems);

        List<CaptureObservation> observations = [];

        foreach (CaptureDocument capture in captures)
        {
            observations.AddRange(importer.Observe(capture, emulator.Registry));
        }

        return new SpecWorld
        {
            Emulator = emulator,
            Clients = clients,
            References = references,
            Captures = captures,
            Observations = observations,
            TriggerSummaries = importer.Summarize(observations),
            Problems =
            [
                .. problems
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(p => p, StringComparer.Ordinal),
            ],
        };
    }

    /// <summary>Reconciles a scan into the spec documents, with cross-references wired up.</summary>
    public static ResolvedSpecs Resolve(SpecWorld world, IList<string> notes)
    {
        IReadOnlyList<PacketSpec> packets = new PacketResolver().Resolve(world, notes);
        IReadOnlyList<FeatureSpec> features = new FeatureBuilder().Build(world, packets);
        IReadOnlyList<ScenarioSpec> scenarios = new ScenarioGenerator().Generate(
            world,
            features,
            packets,
            notes
        );
        IReadOnlyList<ConflictSpec> conflicts = new ConflictDetector().Detect(world, packets);
        IReadOnlyList<UnknownSpec> unknowns = new UnknownCollector().Collect(
            world,
            packets,
            features,
            scenarios
        );

        return Link(packets, features, scenarios, conflicts, unknowns, world);
    }

    /// <summary>
    /// Points each document at the conflicts, unknowns and scenarios that concern it, so a reader who
    /// opens one packet spec is told there is an argument about it rather than having to go looking.
    /// </summary>
    private static ResolvedSpecs Link(
        IReadOnlyList<PacketSpec> packets,
        IReadOnlyList<FeatureSpec> features,
        IReadOnlyList<ScenarioSpec> scenarios,
        IReadOnlyList<ConflictSpec> conflicts,
        IReadOnlyList<UnknownSpec> unknowns,
        SpecWorld world
    )
    {
        ILookup<string, ConflictSpec> conflictsByPacket = conflicts
            .Where(c => c.PacketName is not null)
            .ToLookup(c => c.PacketName!, StringComparer.Ordinal);
        ILookup<string, ConflictSpec> conflictsByFeature = conflicts
            .Where(c => c.FeatureId is not null)
            .ToLookup(c => c.FeatureId!, StringComparer.Ordinal);
        ILookup<string, UnknownSpec> unknownsByPacket = unknowns
            .Where(u => u.PacketName is not null)
            .ToLookup(u => u.PacketName!, StringComparer.Ordinal);
        ILookup<string, UnknownSpec> unknownsByFeature = unknowns
            .Where(u => u.FeatureId is not null)
            .ToLookup(u => u.FeatureId!, StringComparer.Ordinal);
        ILookup<string, ScenarioSpec> scenariosByFeature = scenarios.ToLookup(
            s => s.FeatureId,
            StringComparer.Ordinal
        );

        List<PacketSpec> linkedPackets =
        [
            .. packets.Select(p =>
                p with
                {
                    ConflictIds =
                    [
                        .. conflictsByPacket[p.Name]
                            .Select(c => c.Id)
                            .Distinct(StringComparer.Ordinal),
                    ],
                    UnknownIds =
                    [
                        .. unknownsByPacket[p.Name]
                            .Select(u => u.Id)
                            .Distinct(StringComparer.Ordinal),
                    ],
                }
            ),
        ];

        List<FeatureSpec> linkedFeatures =
        [
            .. features.Select(f =>
                f with
                {
                    ScenarioIds = [.. scenariosByFeature[f.Id].Select(s => s.Id)],
                    ConflictIds =
                    [
                        .. conflictsByFeature[f.Id]
                            .Select(c => c.Id)
                            .Concat(
                                f.TriggerPackets.SelectMany(t =>
                                    conflictsByPacket[t].Select(c => c.Id)
                                )
                            )
                            .Distinct(StringComparer.Ordinal)
                            .OrderBy(c => c, StringComparer.Ordinal),
                    ],
                    UnknownIds =
                    [
                        .. unknownsByFeature[f.Id]
                            .Select(u => u.Id)
                            .Distinct(StringComparer.Ordinal)
                            .OrderBy(u => u, StringComparer.Ordinal),
                    ],
                }
            ),
        ];

        return new ResolvedSpecs
        {
            Packets = linkedPackets,
            Features = linkedFeatures,
            Scenarios = scenarios,
            Conflicts = conflicts,
            Unknowns = unknowns,
            Registries = BuildRegistries(world),
        };
    }

    private static IReadOnlyList<RevisionRegistry> BuildRegistries(SpecWorld world)
    {
        List<RevisionRegistry> registries = [world.Emulator.Registry];

        foreach (ClientScan client in world.Clients)
        {
            if (client.IncomingHeaders.Count == 0 && client.OutgoingHeaders.Count == 0)
            {
                continue;
            }

            registries.Add(
                new RevisionRegistry
                {
                    Id = client.Revision ?? client.Origin,
                    RevisionString = client.Revision,
                    Origin = client.Origin,
                    Authority = client.Authority,
                    TargetsSameRevision = client.TargetsSameRevision,
                    Incoming = client.IncomingHeaders,
                    Outgoing = client.OutgoingHeaders,
                    Evidence =
                        client.Packets.Count > 0
                            ? client.Packets[0].Evidence
                            : world.Emulator.Registry.Evidence,
                }
            );
        }

        foreach (ReferenceScan reference in world.References)
        {
            if (reference.IncomingHeaders.Count == 0)
            {
                continue;
            }

            registries.Add(
                new RevisionRegistry
                {
                    Id = reference.Origin,
                    Origin = reference.Origin,
                    Authority = reference.Authority,
                    TargetsSameRevision = false,
                    Incoming = reference.IncomingHeaders,
                    Outgoing = reference.OutgoingHeaders,
                    Evidence =
                        reference.Behaviours.Count > 0
                            ? reference.Behaviours[0].Evidence
                            : world.Emulator.Registry.Evidence,
                }
            );
        }

        return [.. registries.OrderBy(r => r.Origin, StringComparer.Ordinal)];
    }
}

public sealed record ResolvedSpecs
{
    public required IReadOnlyList<PacketSpec> Packets { get; init; }

    public required IReadOnlyList<FeatureSpec> Features { get; init; }

    public required IReadOnlyList<ScenarioSpec> Scenarios { get; init; }

    public required IReadOnlyList<ConflictSpec> Conflicts { get; init; }

    public required IReadOnlyList<UnknownSpec> Unknowns { get; init; }

    public required IReadOnlyList<RevisionRegistry> Registries { get; init; }
}
