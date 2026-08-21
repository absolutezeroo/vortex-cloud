using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Vortex.Specs.Analysis.Emulator;

/// <summary>One parsed C# file, kept with the path so evidence can point back at it.</summary>
public sealed record IndexedFile(string Path, SyntaxTree Tree)
{
    public CompilationUnitSyntax Root => (CompilationUnitSyntax)Tree.GetRoot();
}

/// <summary>A type declaration found somewhere in the indexed trees.</summary>
public sealed record IndexedType(
    string Name,
    string Namespace,
    TypeDeclarationSyntax Declaration,
    IndexedFile File
)
{
    public string FullName => Namespace.Length == 0 ? Name : $"{Namespace}.{Name}";

    public int Line =>
        Declaration.SyntaxTree.GetLineSpan(Declaration.Identifier.Span).StartLinePosition.Line + 1;

    /// <summary>Base types and interfaces as written, generics included.</summary>
    public IReadOnlyList<string> BaseTypes =>
        Declaration.BaseList is null
            ? []
            : [.. Declaration.BaseList.Types.Select(t => t.Type.ToString())];
}

/// <summary>
/// A syntax-level index of the repository's own C#.
/// </summary>
/// <remarks>
/// Syntax trees rather than a full <see cref="Compilation"/>: binding the solution needs MSBuild,
/// a successful restore and roughly a minute, and buys nothing this analysis actually uses. What it
/// needs is declaration order, argument order and call targets by name, all of which are syntactic.
/// The type index carries the same lookups a symbol model would be asked for, so swapping a bound
/// compilation in later is a change to this class and nothing above it.
/// </remarks>
public sealed class CSharpSourceIndex
{
    private readonly Dictionary<string, List<IndexedType>> _typesByName;

    private CSharpSourceIndex(IReadOnlyList<IndexedFile> files, IReadOnlyList<IndexedType> types)
    {
        Files = files;
        Types = types;
        _typesByName = types
            .GroupBy(t => t.Name, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
    }

    public IReadOnlyList<IndexedFile> Files { get; }

    public IReadOnlyList<IndexedType> Types { get; }

    public static CSharpSourceIndex Build(string root, IReadOnlyList<string>? projectFilter = null)
    {
        List<string> paths = EnumerateSources(root, projectFilter);
        ConcurrentBag<IndexedFile> files = [];

        Parallel.ForEach(
            paths,
            path =>
            {
                string text = File.ReadAllText(path);
                files.Add(new IndexedFile(path, CSharpSyntaxTree.ParseText(text, path: path)));
            }
        );

        // Ordinal ordering by path so every downstream enumeration is reproducible: the parallel
        // parse above finishes in whatever order the thread pool feels like.
        List<IndexedFile> ordered = [.. files.OrderBy(f => f.Path, StringComparer.Ordinal)];
        List<IndexedType> types = [];

        foreach (IndexedFile file in ordered)
        {
            foreach (
                TypeDeclarationSyntax declaration in file
                    .Root.DescendantNodes()
                    .OfType<TypeDeclarationSyntax>()
            )
            {
                types.Add(
                    new IndexedType(
                        declaration.Identifier.ValueText,
                        NamespaceOf(declaration),
                        declaration,
                        file
                    )
                );
            }
        }

        return new CSharpSourceIndex(ordered, types);
    }

    private static List<string> EnumerateSources(string root, IReadOnlyList<string>? projectFilter)
    {
        IEnumerable<string> roots = projectFilter is { Count: > 0 }
            ? projectFilter.Select(p => Path.Combine(root, p)).Where(Directory.Exists)
            : [root];

        List<string> paths = [];

        foreach (string directory in roots)
        {
            foreach (
                string path in Directory.EnumerateFiles(
                    directory,
                    "*.cs",
                    SearchOption.AllDirectories
                )
            )
            {
                string normalized = path.Replace('\\', '/');

                // obj/ holds generated Orleans proxies and AssemblyInfo copies; indexing them
                // doubles the tree and reports generated code as if a human had written it.
                if (
                    normalized.Contains("/obj/", StringComparison.Ordinal)
                    || normalized.Contains("/bin/", StringComparison.Ordinal)
                )
                {
                    continue;
                }

                paths.Add(path);
            }
        }

        paths.Sort(StringComparer.Ordinal);
        return paths;
    }

    private static string NamespaceOf(SyntaxNode node)
    {
        List<string> parts = [];

        for (SyntaxNode? current = node.Parent; current is not null; current = current.Parent)
        {
            switch (current)
            {
                case FileScopedNamespaceDeclarationSyntax fileScoped:
                    parts.Insert(0, fileScoped.Name.ToString());
                    break;
                case NamespaceDeclarationSyntax declared:
                    parts.Insert(0, declared.Name.ToString());
                    break;
            }
        }

        return string.Join('.', parts);
    }

    public IReadOnlyList<IndexedType> FindByName(string name) =>
        _typesByName.TryGetValue(name, out List<IndexedType>? found) ? found : [];

    public IndexedType? FindSingle(string name)
    {
        IReadOnlyList<IndexedType> found = FindByName(name);

        // More than one match is normal for partial classes; the first by path is deterministic and
        // every caller here only needs a location to point evidence at.
        return found.Count > 0 ? found[0] : null;
    }

    /// <summary>All declarations of a type, so partial classes are followed rather than truncated.</summary>
    public IReadOnlyList<IndexedType> FindAllParts(string name) => FindByName(name);

    /// <summary>Types whose base list mentions <paramref name="baseTypeName"/>.</summary>
    public IReadOnlyList<IndexedType> FindImplementing(string baseTypeName) =>
        [
            .. Types.Where(t =>
                t.BaseTypes.Any(b =>
                    b.Equals(baseTypeName, StringComparison.Ordinal)
                    || b.StartsWith(baseTypeName + "<", StringComparison.Ordinal)
                )
            ),
        ];

    public static int LineOf(SyntaxNode node) =>
        node.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
}
