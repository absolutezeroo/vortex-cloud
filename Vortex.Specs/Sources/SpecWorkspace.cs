using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Vortex.Specs.Sources;

/// <summary>One analyzable tree on disk.</summary>
public sealed record SourceTree
{
    /// <summary>Stable id used in evidence origins: <c>vortex</c>, <c>nitro</c>, <c>arcturus</c>.</summary>
    public required string Id { get; init; }

    public required string Root { get; init; }

    public required SourceTreeKind Kind { get; init; }

    /// <summary>The client build the tree targets, when it declares one.</summary>
    public string? Revision { get; init; }
}

public enum SourceTreeKind
{
    /// <summary>This repository.</summary>
    Emulator,

    /// <summary>An official Habbo client.</summary>
    OfficialClient,

    /// <summary>A community client whose protocol tables were reverse-engineered.</summary>
    CommunityClient,

    /// <summary>A third-party server implementation.</summary>
    ReferenceEmulator,
}

/// <summary>
/// Where everything the analyzers read lives.
/// </summary>
/// <remarks>
/// Discovery is by convention with an explicit override, never by a baked-in absolute path: the
/// sibling checkouts are one developer's layout, and a spec tool that only runs on one machine is
/// not a spec tool. Every tree is optional — a missing client or reference degrades the confidence
/// of the output, it does not fail the run.
/// </remarks>
public sealed class SpecWorkspace
{
    private SpecWorkspace(string repositoryRoot, string outputRoot, IReadOnlyList<SourceTree> trees)
    {
        RepositoryRoot = repositoryRoot;
        OutputRoot = outputRoot;
        Trees = trees;
    }

    public string RepositoryRoot { get; }

    public string OutputRoot { get; }

    public IReadOnlyList<SourceTree> Trees { get; }

    public SourceTree Emulator => Trees.First(t => t.Kind == SourceTreeKind.Emulator);

    public IReadOnlyList<SourceTree> Clients =>
        [
            .. Trees.Where(t =>
                t.Kind is SourceTreeKind.OfficialClient or SourceTreeKind.CommunityClient
            ),
        ];

    public IReadOnlyList<SourceTree> References =>
        [.. Trees.Where(t => t.Kind == SourceTreeKind.ReferenceEmulator)];

    public string CaptureRoot => Path.Combine(OutputRoot, "evidence", "captures");

    /// <summary>
    /// Walks up from <paramref name="start"/> for the directory holding the solution, then looks for
    /// the known sibling checkouts next to it.
    /// </summary>
    public static SpecWorkspace Discover(string? start = null, string? outputRoot = null)
    {
        string repositoryRoot = FindRepositoryRoot(start ?? Directory.GetCurrentDirectory());
        List<SourceTree> trees =
        [
            new SourceTree
            {
                Id = "vortex",
                Root = repositoryRoot,
                Kind = SourceTreeKind.Emulator,
            },
        ];

        string? sources = FindClientSources(repositoryRoot);

        if (sources is not null)
        {
            trees.AddRange(DiscoverExternalTrees(sources));
        }

        return new SpecWorkspace(
            repositoryRoot,
            outputRoot ?? Path.Combine(repositoryRoot, "docs", "habbo-specs"),
            trees
        );
    }

    /// <summary>Used by tests and by callers that lay their checkouts out differently.</summary>
    public static SpecWorkspace ForTrees(
        string repositoryRoot,
        string outputRoot,
        IReadOnlyList<SourceTree> trees
    ) => new(repositoryRoot, outputRoot, trees);

    private static IEnumerable<SourceTree> DiscoverExternalTrees(string sources)
    {
        foreach (
            string directory in Directory
                .EnumerateDirectories(sources)
                .OrderBy(d => d, StringComparer.Ordinal)
        )
        {
            string name = Path.GetFileName(directory);

            // The official Flash clients are shipped as a decompiled `src/com/sulake` tree; the
            // build id is the directory name and doubles as the revision string.
            if (Directory.Exists(Path.Combine(directory, "src", "com", "sulake")))
            {
                yield return new SourceTree
                {
                    Id = $"as3:{name}",
                    Root = directory,
                    Kind = SourceTreeKind.OfficialClient,
                    Revision = name,
                };
                continue;
            }

            if (
                Directory.Exists(
                    Path.Combine(directory, "packages", "nitro-shared", "src", "packets")
                )
            )
            {
                yield return new SourceTree
                {
                    Id = "nitro",
                    Root = directory,
                    Kind = SourceTreeKind.CommunityClient,
                };
                continue;
            }

            if (
                Directory.Exists(
                    Path.Combine(directory, "src", "main", "java", "com", "eu", "habbo")
                )
            )
            {
                yield return new SourceTree
                {
                    Id = "arcturus",
                    Root = directory,
                    Kind = SourceTreeKind.ReferenceEmulator,
                };
            }
        }
    }

    private static string FindRepositoryRoot(string start)
    {
        DirectoryInfo? current = new(Path.GetFullPath(start));

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Vortex.Cloud.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            $"No Vortex.Cloud.sln found at or above '{start}'. Pass --repo to point at the checkout."
        );
    }

    /// <summary>
    /// Locates the client/reference checkouts. They live outside this repository on purpose (they
    /// are other people's code), so the search is for a sibling holding a <c>sources</c> directory.
    /// </summary>
    private static string? FindClientSources(string repositoryRoot)
    {
        DirectoryInfo? parent = Directory.GetParent(repositoryRoot);

        if (parent is null)
        {
            return null;
        }

        foreach (
            string sibling in Directory
                .EnumerateDirectories(parent.FullName)
                .OrderBy(d => d, StringComparer.Ordinal)
        )
        {
            string candidate = Path.Combine(sibling, "sources");

            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        string direct = Path.Combine(parent.FullName, "sources");

        return Directory.Exists(direct) ? direct : null;
    }

    /// <summary>Repository-relative, forward-slashed path for evidence sources.</summary>
    public string Relative(string absolutePath)
    {
        string full = Path.GetFullPath(absolutePath);
        string root = Path.GetFullPath(RepositoryRoot);

        if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return full[root.Length..].TrimStart('\\', '/').Replace('\\', '/');
        }

        // External trees are addressed relative to the parent of the checkout so the evidence stays
        // meaningful on another machine with the same sibling layout.
        DirectoryInfo? parent = Directory.GetParent(root);

        if (
            parent is not null
            && full.StartsWith(parent.FullName, StringComparison.OrdinalIgnoreCase)
        )
        {
            return "../" + full[parent.FullName.Length..].TrimStart('\\', '/').Replace('\\', '/');
        }

        return full.Replace('\\', '/');
    }
}
