using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Vortex.Specs.Analysis.Emulator;

/// <summary>
/// The declared type of every identifier visible inside one method: fields and primary-constructor
/// parameters of the owning type, the method's own parameters, and its locals.
/// </summary>
/// <remarks>
/// Enough to answer "what is <c>roomGrain</c>?", which is what turns
/// <c>roomGrain.MoveFloorItemByIdAsync(...)</c> from an unresolvable name into a hop in the call
/// chain. <c>var</c> is followed one step through an initializer whose method is in the index.
/// </remarks>
public sealed class LocalTypeTable
{
    private readonly Dictionary<string, string> _types = new(StringComparer.Ordinal);
    private readonly HashSet<string> _locals = new(StringComparer.Ordinal);
    private readonly HashSet<string> _typeMembers = new(StringComparer.Ordinal);
    private readonly HashSet<string> _methodParameters = new(StringComparer.Ordinal);
    private readonly HashSet<string> _freshLocals = new(StringComparer.Ordinal);
    private readonly MethodIndex _methods;

    public LocalTypeTable(
        IndexedType owner,
        MethodDeclarationSyntax method,
        MethodIndex methods,
        IReadOnlyList<IndexedType>? ownerParts = null
    )
    {
        // Every partial declaration of the owning type, not just the file this method sits in. The
        // grains here are split across a dozen files and their modules are declared in whichever one
        // the author found tidiest; reading only the local part loses the receiver's type and the
        // call chain stops dead at the grain.
        _methods = methods;

        IReadOnlyList<IndexedType> parts = ownerParts is { Count: > 0 } ? ownerParts : [owner];

        foreach (IndexedType part in parts)
        {
            Index(part);
        }

        foreach (ParameterSyntax parameter in method.ParameterList.Parameters)
        {
            _methodParameters.Add(parameter.Identifier.ValueText);

            if (parameter.Type is not null)
            {
                _types[parameter.Identifier.ValueText] = parameter.Type.ToString();
            }
        }

        SyntaxNode? body = (SyntaxNode?)method.Body ?? method.ExpressionBody;

        if (body is null)
        {
            return;
        }

        foreach (
            VariableDeclarationSyntax declaration in body.DescendantNodes()
                .OfType<VariableDeclarationSyntax>()
        )
        {
            string declared = declaration.Type.ToString();

            foreach (VariableDeclaratorSyntax declarator in declaration.Variables)
            {
                string name = declarator.Identifier.ValueText;
                _locals.Add(name);

                if (
                    declarator.Initializer?.Value
                    is ObjectCreationExpressionSyntax
                        or ImplicitObjectCreationExpressionSyntax
                )
                {
                    _freshLocals.Add(name);
                }

                _types[name] = declared is "var" or "var?"
                    ? InferFromInitializer(declarator.Initializer?.Value, methods) ?? declared
                    : declared;
            }
        }
    }

    private void Index(IndexedType owner)
    {
        foreach (
            FieldDeclarationSyntax field in owner.Declaration.Members.OfType<FieldDeclarationSyntax>()
        )
        {
            foreach (VariableDeclaratorSyntax declarator in field.Declaration.Variables)
            {
                _types[declarator.Identifier.ValueText] = field.Declaration.Type.ToString();
                _typeMembers.Add(declarator.Identifier.ValueText);
            }
        }

        foreach (
            PropertyDeclarationSyntax property in owner.Declaration.Members.OfType<PropertyDeclarationSyntax>()
        )
        {
            // Grains expose their modules as properties, so skipping these stops the walk at the
            // grain and loses every mutation the module performs.
            _types[property.Identifier.ValueText] = property.Type.ToString();
            _typeMembers.Add(property.Identifier.ValueText);
        }

        if (owner.Declaration.ParameterList is { } primary)
        {
            foreach (ParameterSyntax parameter in primary.Parameters)
            {
                if (parameter.Type is not null)
                {
                    _types[parameter.Identifier.ValueText] = parameter.Type.ToString();
                    _typeMembers.Add(parameter.Identifier.ValueText);
                }
            }
        }

        foreach (
            ConstructorDeclarationSyntax constructor in owner.Declaration.Members.OfType<ConstructorDeclarationSyntax>()
        )
        {
            foreach (ParameterSyntax parameter in constructor.ParameterList.Parameters)
            {
                if (parameter.Type is not null)
                {
                    _types.TryAdd(parameter.Identifier.ValueText, parameter.Type.ToString());
                    _typeMembers.Add(parameter.Identifier.ValueText);
                }
            }
        }
    }

    private string? InferFromInitializer(ExpressionSyntax? initializer, MethodIndex methods)
    {
        switch (initializer)
        {
            case ObjectCreationExpressionSyntax creation:
                return creation.Type.ToString();

            case AwaitExpressionSyntax awaited:
                return Unwrap(InferFromInitializer(awaited.Expression, methods));

            case InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member }:
            {
                string method = member.Name.Identifier.ValueText;

                if (method is "ConfigureAwait")
                {
                    return InferFromInitializer(member.Expression, methods);
                }

                IndexedMethod? resolved = methods.Resolve(null, method);
                return Unwrap(resolved?.ReturnType);
            }

            default:
                return null;
        }
    }

    /// <summary>Peels <c>Task&lt;T&gt;</c> and friends so the useful type is what comes back.</summary>
    private static string? Unwrap(string? type)
    {
        if (type is null)
        {
            return null;
        }

        foreach (string wrapper in new[] { "Task<", "ValueTask<", "Nullable<" })
        {
            if (type.StartsWith(wrapper, StringComparison.Ordinal) && type.EndsWith('>'))
            {
                return Unwrap(type[wrapper.Length..^1]);
            }
        }

        return type.TrimEnd('?');
    }

    public string? TypeOf(string identifier) =>
        _types.TryGetValue(identifier, out string? type) ? Unwrap(type) : null;

    /// <summary>True when the identifier is declared inside the method rather than handed to it.</summary>
    public bool IsLocal(string identifier) => _locals.Contains(identifier);

    /// <summary>
    /// True when the identifier is an injected dependency of the owning type rather than something
    /// the method was handed. That distinction separates a handler's domain collaborators from its
    /// own plumbing: <c>_roomService</c> is a dependency, <c>ctx</c> and <c>message</c> are not.
    /// </summary>
    public bool IsDependency(string identifier) => _typeMembers.Contains(identifier);

    /// <summary>
    /// True when the local was constructed in this method rather than fetched from somewhere. A
    /// write to a freshly built object is scratch work; a write to something pulled out of the
    /// room's item dictionary is a state change, and the two must not be conflated.
    /// </summary>
    public bool IsFreshlyConstructed(string identifier) => _freshLocals.Contains(identifier);

    /// <summary>True when the identifier is one of the method's own parameters.</summary>
    public bool IsMethodParameter(string identifier) => _methodParameters.Contains(identifier);

    public bool IsMethodParameterExpression(ExpressionSyntax expression) =>
        expression switch
        {
            IdentifierNameSyntax identifier => IsMethodParameter(identifier.Identifier.ValueText),
            MemberAccessExpressionSyntax member => IsMethodParameterExpression(member.Expression),
            ParenthesizedExpressionSyntax parenthesized => IsMethodParameterExpression(
                parenthesized.Expression
            ),
            _ => false,
        };

    public bool IsDependencyExpression(ExpressionSyntax expression) =>
        expression switch
        {
            IdentifierNameSyntax identifier => IsDependency(identifier.Identifier.ValueText),
            MemberAccessExpressionSyntax member => IsDependencyExpression(member.Expression),
            ParenthesizedExpressionSyntax parenthesized => IsDependencyExpression(
                parenthesized.Expression
            ),
            _ => false,
        };

    public string? TypeOfExpression(ExpressionSyntax expression)
    {
        switch (expression)
        {
            case IdentifierNameSyntax identifier:
                return TypeOf(identifier.Identifier.ValueText);

            case ParenthesizedExpressionSyntax parenthesized:
                return TypeOfExpression(parenthesized.Expression);

            case AwaitExpressionSyntax awaited:
                return TypeOfExpression(awaited.Expression);

            // `_grainFactory.GetRoomMap(id).ClickTileAsync(...)` — the canonical grain access this
            // repository mandates. The receiver is a call, not a variable, so without this the
            // whole chain resolves to nothing and every feature reached that way looks empty.
            case InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax call }:
            {
                string method = call.Name.Identifier.ValueText;

                if (method == "ConfigureAwait")
                {
                    return TypeOfExpression(call.Expression);
                }

                IndexedMethod? resolved =
                    _methods.Resolve(TypeOfExpression(call.Expression), method)
                    ?? _methods.Resolve(null, method);

                return Unwrap(resolved?.ReturnType);
            }

            case MemberAccessExpressionSyntax member:
            {
                // Follow the chain: resolve what the receiver is, then ask that type what the member
                // is. Falling straight through to the bare member name works only when the name
                // happens to be in scope here too, which for a chain across two types it is not.
                string? receiver = TypeOfExpression(member.Expression);
                string memberName = member.Name.Identifier.ValueText;

                if (receiver is not null)
                {
                    string? declared = _methods.MemberType(receiver, memberName);

                    if (declared is not null)
                    {
                        return Unwrap(declared);
                    }
                }

                return TypeOf(memberName);
            }

            default:
                return null;
        }
    }
}
