using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Vortex.Specs.Analysis.Emulator;

public sealed record IndexedMethod(IndexedType Owner, MethodDeclarationSyntax Declaration)
{
    public string Name => Declaration.Identifier.ValueText;

    public string ReturnType => Declaration.ReturnType.ToString();

    public int Line => CSharpSourceIndex.LineOf(Declaration);
}

/// <summary>
/// Name-based method lookup across the indexed sources, plus the interface-to-implementation step.
/// </summary>
/// <remarks>
/// This is the piece a bound <see cref="Microsoft.CodeAnalysis.Compilation"/> would do properly.
/// Resolving by name is an approximation, and it is treated as one: every consumer marks what it
/// could not resolve rather than assuming the call chain simply ended there.
/// </remarks>
public sealed class MethodIndex
{
    private readonly Dictionary<string, List<IndexedMethod>> _byName;
    private readonly Dictionary<string, List<IndexedType>> _implementationsByInterface;
    private readonly Dictionary<string, string> _memberTypes;

    public MethodIndex(CSharpSourceIndex index)
    {
        Dictionary<string, List<IndexedMethod>> byName = new(StringComparer.Ordinal);
        Dictionary<string, List<IndexedType>> implementations = new(StringComparer.Ordinal);
        Dictionary<string, string> memberTypes = new(StringComparer.Ordinal);

        foreach (IndexedType type in index.Types)
        {
            foreach (
                MethodDeclarationSyntax method in type.Declaration.Members.OfType<MethodDeclarationSyntax>()
            )
            {
                string name = method.Identifier.ValueText;

                if (!byName.TryGetValue(name, out List<IndexedMethod>? bucket))
                {
                    bucket = [];
                    byName[name] = bucket;
                }

                bucket.Add(new IndexedMethod(type, method));
            }

            foreach (
                PropertyDeclarationSyntax property in type.Declaration.Members.OfType<PropertyDeclarationSyntax>()
            )
            {
                memberTypes.TryAdd(
                    MemberKey(type.Name, property.Identifier.ValueText),
                    property.Type.ToString()
                );
            }

            foreach (
                FieldDeclarationSyntax field in type.Declaration.Members.OfType<FieldDeclarationSyntax>()
            )
            {
                foreach (VariableDeclaratorSyntax declarator in field.Declaration.Variables)
                {
                    memberTypes.TryAdd(
                        MemberKey(type.Name, declarator.Identifier.ValueText),
                        field.Declaration.Type.ToString()
                    );
                }
            }

            foreach (string baseType in type.BaseTypes)
            {
                string bare = StripGenerics(baseType);

                if (!implementations.TryGetValue(bare, out List<IndexedType>? list))
                {
                    list = [];
                    implementations[bare] = list;
                }

                list.Add(type);
            }
        }

        _byName = byName;
        _implementationsByInterface = implementations;
        _memberTypes = memberTypes;
    }

    private static string MemberKey(string typeName, string memberName) =>
        typeName + "." + memberName;

    /// <summary>
    /// The declared type of a field or property on a named type.
    /// </summary>
    /// <remarks>
    /// Needed to follow chains such as <c>_roomGrain.FurniModule.MoveFloorItemByIdAsync(...)</c>:
    /// without it the receiver's type is unknown, the call resolves to nothing, and everything the
    /// module does — including the broadcast that is the visible half of the feature — falls out of
    /// the flow.
    /// </remarks>
    public string? MemberType(string typeName, string memberName) =>
        _memberTypes.TryGetValue(MemberKey(StripGenerics(typeName), memberName), out string? type)
            ? type
            : null;

    public static string StripGenerics(string typeName)
    {
        int angle = typeName.IndexOf('<', StringComparison.Ordinal);
        string bare = angle < 0 ? typeName : typeName[..angle];
        int dot = bare.LastIndexOf('.');

        return dot < 0 ? bare : bare[(dot + 1)..];
    }

    public IReadOnlyList<IndexedMethod> ByName(string name) =>
        _byName.TryGetValue(name, out List<IndexedMethod>? found) ? found : [];

    public IReadOnlyList<IndexedType> ImplementationsOf(string interfaceName) =>
        _implementationsByInterface.TryGetValue(interfaceName, out List<IndexedType>? found)
            ? found
            : [];

    /// <summary>
    /// Finds the body behind a call. Prefers a method declared on the receiver's own type or on a
    /// type implementing it; falls back to a globally unique name, and gives up rather than guessing
    /// when several unrelated types declare the same name.
    /// </summary>
    public IndexedMethod? Resolve(string? receiverType, string methodName)
    {
        IReadOnlyList<IndexedMethod> candidates = ByName(methodName);

        if (candidates.Count == 0)
        {
            return null;
        }

        if (receiverType is not null)
        {
            string bare = StripGenerics(receiverType);

            IndexedMethod? direct = candidates.FirstOrDefault(c =>
                string.Equals(c.Owner.Name, bare, StringComparison.Ordinal) && HasBody(c)
            );

            if (direct is not null)
            {
                return direct;
            }

            foreach (IndexedType implementation in ImplementationsOf(bare))
            {
                IndexedMethod? onImplementation = candidates.FirstOrDefault(c =>
                    string.Equals(c.Owner.Name, implementation.Name, StringComparison.Ordinal)
                    && HasBody(c)
                );

                if (onImplementation is not null)
                {
                    return onImplementation;
                }
            }

            // Interfaces here are named IFoo and implemented by Foo, sometimes across several
            // partial files. That convention is load-bearing in this repository, so it is worth
            // one more attempt before giving up.
            if (bare.Length > 1 && bare[0] == 'I' && char.IsUpper(bare[1]))
            {
                IndexedMethod? byConvention = candidates.FirstOrDefault(c =>
                    string.Equals(c.Owner.Name, bare[1..], StringComparison.Ordinal) && HasBody(c)
                );

                if (byConvention is not null)
                {
                    return byConvention;
                }
            }

            return null;
        }

        List<IndexedMethod> withBodies = [.. candidates.Where(HasBody)];

        return withBodies.Count == 1 ? withBodies[0] : null;
    }

    private static bool HasBody(IndexedMethod method) =>
        method.Declaration.Body is not null || method.Declaration.ExpressionBody is not null;
}
