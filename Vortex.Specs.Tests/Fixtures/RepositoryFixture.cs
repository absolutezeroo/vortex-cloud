using System;
using System.Collections.Generic;
using System.IO;
using Vortex.Specs.Analysis.Emulator;
using Vortex.Specs.Sources;
using Xunit;

namespace Vortex.Specs.Tests.Fixtures;

/// <summary>
/// Indexes the real repository once for the whole test run.
/// </summary>
/// <remarks>
/// The analyzers are tested against this checkout rather than against toy snippets. A parser
/// extractor that works on a hand-written sample and falls over on the fluent chains and shared
/// sub-serializers the revision tree actually uses would pass a snippet test and be useless, which
/// is the failure mode this fixture exists to prevent. Snippet tests still cover the edge cases the
/// repository happens not to contain.
/// </remarks>
public sealed class RepositoryFixture
{
    public RepositoryFixture()
    {
        Workspace = SpecWorkspace.Discover(AppContext.BaseDirectory);
        Index = CSharpSourceIndex.Build(
            Workspace.RepositoryRoot,
            [
                "Vortex.Revisions",
                "Vortex.PacketHandlers",
                "Vortex.Primitives",
                "Vortex.Rooms",
                "Vortex.Players",
                "Vortex.Catalog",
                "Vortex.Inventory",
                "Vortex.Navigator",
                "Vortex.Furniture",
                "Vortex.Messages",
                "Vortex.Marketplace",
            ]
        );
        Scan = new EmulatorAnalyzer(Workspace, Index).Scan();
    }

    public SpecWorkspace Workspace { get; }

    public CSharpSourceIndex Index { get; }

    public EmulatorScan Scan { get; }

    /// <summary>True when the sibling client and reference checkouts are present on this machine.</summary>
    public bool HasExternalTrees => Workspace.Trees.Count > 1;

    public IReadOnlyList<SourceTree> Trees => Workspace.Trees;

    public static bool RepositoryIsAvailable
    {
        get
        {
            DirectoryInfo? current = new(AppContext.BaseDirectory);

            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "Vortex.Cloud.sln")))
                {
                    return true;
                }

                current = current.Parent;
            }

            return false;
        }
    }
}

[CollectionDefinition(Name)]
public sealed class RepositoryCollection : ICollectionFixture<RepositoryFixture>
{
    public const string Name = "repository";
}
