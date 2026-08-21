using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Vortex.Specs.Model;

namespace Vortex.Specs.Analysis.Emulator;

/// <summary>One value read from or written to the wire, in the order the code touches it.</summary>
public sealed record WireOp
{
    public required WireType Type { get; init; }

    /// <summary>The name the implementation gives it, before snake-casing. Null when unnamed.</summary>
    public string? Name { get; init; }

    /// <summary>The declared type the implementation wraps it in, e.g. <c>RoomObjectId</c>.</summary>
    public string? SemanticType { get; init; }

    /// <summary>Set when the code writes a constant rather than a value.</summary>
    public string? ConstantValue { get; init; }

    /// <summary>The condition guarding this op, when it is not written unconditionally.</summary>
    public string? Condition { get; init; }

    /// <summary>Trailing <c>// comment</c> on the writing line. A hint for a reader, never a name.</summary>
    public string? Comment { get; init; }

    public IReadOnlyList<WireOp> Children { get; init; } = [];

    public required int Line { get; init; }
}

/// <summary>What the extractor could and could not follow.</summary>
public sealed record WireLayout(
    IReadOnlyList<WireOp> Ops,
    bool IsPartial,
    IReadOnlyList<string> Unresolved
);

/// <summary>
/// Reads the wire layout out of a parser's <c>Parse</c> or a serializer's <c>Serialize</c> body.
/// </summary>
/// <remarks>
/// The order is the whole point, and it is not the order the nodes appear in the syntax tree.
/// <c>packet.WriteInteger(a).WriteString(b)</c> nests the first call *inside* the second, so a plain
/// pre-order walk reports them backwards. This walker follows evaluation order — receiver before
/// call — which is the order the bytes actually go out in.
/// </remarks>
public sealed class WireLayoutExtractor(CSharpSourceIndex index)
{
    private const int MaxDepth = 6;

    private static readonly Dictionary<string, WireType> ReadOps = new(StringComparer.Ordinal)
    {
        ["PopInt"] = WireType.Int32,
        ["PopString"] = WireType.String,
        ["PopBoolean"] = WireType.Boolean,
        ["PopByte"] = WireType.Byte,
        ["PopShort"] = WireType.Short,
        ["PopUShort"] = WireType.Short,
        ["PopLong"] = WireType.Long,
    };

    private static readonly Dictionary<string, WireType> WriteOps = new(StringComparer.Ordinal)
    {
        ["WriteInteger"] = WireType.Int32,
        ["WriteString"] = WireType.String,
        ["WriteBoolean"] = WireType.Boolean,
        ["WriteByte"] = WireType.Byte,
        ["WriteShort"] = WireType.Short,
        ["WriteLong"] = WireType.Long,
        ["WriteFloat"] = WireType.Float,
        ["WriteDouble"] = WireType.Double,
    };

    private sealed record Context(
        bool Reading,
        int Depth,
        List<string> Unresolved,
        HashSet<string> Visiting
    );

    public WireLayout ExtractRead(TypeDeclarationSyntax parser)
    {
        MethodDeclarationSyntax? method = FindMethod(parser, "Parse");

        if (method is null)
        {
            return new WireLayout([], IsPartial: true, ["no Parse method"]);
        }

        return Extract(method, reading: true);
    }

    public WireLayout ExtractWrite(TypeDeclarationSyntax serializer)
    {
        MethodDeclarationSyntax? method = FindMethod(serializer, "Serialize");

        if (method is null)
        {
            return new WireLayout([], IsPartial: true, ["no Serialize method"]);
        }

        return Extract(method, reading: false);
    }

    private WireLayout Extract(MethodDeclarationSyntax method, bool reading)
    {
        List<string> unresolved = [];
        Context context = new(reading, 0, unresolved, new HashSet<string>(StringComparer.Ordinal));
        List<WireOp> ops = [];

        SyntaxNode? body = (SyntaxNode?)method.Body ?? method.ExpressionBody;

        if (body is not null)
        {
            Walk(body, ops, context);
        }

        return new WireLayout(ops, unresolved.Count > 0, unresolved);
    }

    private static MethodDeclarationSyntax? FindMethod(TypeDeclarationSyntax type, string name) =>
        type
            .Members.OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.ValueText == name);

    private void Walk(SyntaxNode node, List<WireOp> sink, Context context)
    {
        switch (node)
        {
            case InvocationExpressionSyntax invocation:
                WalkInvocation(invocation, sink, context);
                return;

            case ForStatementSyntax
            or ForEachStatementSyntax
            or WhileStatementSyntax
            or DoStatementSyntax:
                WalkLoop(node, sink, context);
                return;
        }

        foreach (SyntaxNode child in node.ChildNodes())
        {
            Walk(child, sink, context);
        }
    }

    private void WalkLoop(SyntaxNode loop, List<WireOp> sink, Context context)
    {
        SyntaxNode? body = loop switch
        {
            ForStatementSyntax f => f.Statement,
            ForEachStatementSyntax f => f.Statement,
            WhileStatementSyntax w => w.Statement,
            DoStatementSyntax d => d.Statement,
            _ => null,
        };

        if (body is null)
        {
            return;
        }

        List<WireOp> children = [];
        Walk(body, children, context);

        if (children.Count == 0)
        {
            return;
        }

        sink.Add(
            new WireOp
            {
                Type = WireType.Array,
                Name = LoopCollectionName(loop),
                Children = children,
                Line = CSharpSourceIndex.LineOf(loop),
                Comment =
                    "repeated block; the element count is the int written immediately before it",
            }
        );
    }

    private static string? LoopCollectionName(SyntaxNode loop) =>
        loop switch
        {
            ForEachStatementSyntax f => NameFromExpression(f.Expression),
            ForStatementSyntax f when f.Condition is BinaryExpressionSyntax binary =>
                NameFromExpression(binary.Right),
            _ => null,
        };

    private void WalkInvocation(
        InvocationExpressionSyntax invocation,
        List<WireOp> sink,
        Context context
    )
    {
        string? method = MethodName(invocation);

        if (method is null)
        {
            foreach (SyntaxNode child in invocation.ChildNodes())
            {
                Walk(child, sink, context);
            }

            return;
        }

        // Evaluation order: whatever the receiver did to the packet happened first.
        if (invocation.Expression is MemberAccessExpressionSyntax access)
        {
            Walk(access.Expression, sink, context);
        }

        Dictionary<string, WireType> table = context.Reading ? ReadOps : WriteOps;

        if (table.TryGetValue(method, out WireType type))
        {
            sink.Add(BuildPrimitive(invocation, type, context));
            return;
        }

        if (TryWalkNestedBlock(invocation, method, sink, context))
        {
            return;
        }

        foreach (ArgumentSyntax argument in invocation.ArgumentList.Arguments)
        {
            Walk(argument.Expression, sink, context);
        }
    }

    /// <summary>
    /// Follows a call into a shared sub-serializer or sub-parser, so a composed layout comes out flat
    /// enough to compare against a client that inlines the same bytes.
    /// </summary>
    private bool TryWalkNestedBlock(
        InvocationExpressionSyntax invocation,
        string method,
        List<WireOp> sink,
        Context context
    )
    {
        bool isNested = context.Reading
            ? method is "Parse" or "ParseArray"
            : method is "Serialize" or "SerializeArray";

        if (!isNested)
        {
            return false;
        }

        string? typeName = ReceiverTypeName(invocation);

        if (typeName is null)
        {
            return false;
        }

        if (context.Depth >= MaxDepth || !context.Visiting.Add(typeName))
        {
            context.Unresolved.Add($"{typeName}.{method} (recursion or depth limit)");
            sink.Add(
                new WireOp
                {
                    Type = WireType.Block,
                    Name = typeName,
                    Line = CSharpSourceIndex.LineOf(invocation),
                    Comment = "nested block not expanded: recursive or deeper than the walk limit",
                }
            );
            return true;
        }

        try
        {
            IndexedType? target = index.FindSingle(typeName);

            if (target is null)
            {
                context.Unresolved.Add($"{typeName}.{method} (type not in the indexed sources)");
                sink.Add(
                    new WireOp
                    {
                        Type = WireType.Block,
                        Name = typeName,
                        Line = CSharpSourceIndex.LineOf(invocation),
                        Comment = "nested block not expanded: declaring type not found",
                    }
                );
                return true;
            }

            MethodDeclarationSyntax? nested = FindMethod(target.Declaration, method);

            if (nested is null)
            {
                context.Unresolved.Add($"{typeName}.{method} (method not found)");
                return true;
            }

            List<WireOp> children = [];
            SyntaxNode? body = (SyntaxNode?)nested.Body ?? nested.ExpressionBody;

            if (body is not null)
            {
                Walk(body, children, context with { Depth = context.Depth + 1 });
            }

            sink.Add(
                new WireOp
                {
                    Type = WireType.Block,
                    Name = StripBlockSuffix(typeName),
                    Children = children,
                    Line = CSharpSourceIndex.LineOf(invocation),
                }
            );

            return true;
        }
        finally
        {
            context.Visiting.Remove(typeName);
        }
    }

    private static string StripBlockSuffix(string typeName)
    {
        foreach (string suffix in new[] { "SnapshotSerializer", "Serializer", "Parser" })
        {
            if (
                typeName.Length > suffix.Length
                && typeName.EndsWith(suffix, StringComparison.Ordinal)
            )
            {
                return typeName[..^suffix.Length];
            }
        }

        return typeName;
    }

    private WireOp BuildPrimitive(
        InvocationExpressionSyntax invocation,
        WireType type,
        Context context
    )
    {
        (string? name, string? semanticType) = context.Reading
            ? NameForRead(invocation)
            : NameForWrite(invocation);

        ExpressionSyntax? argument =
            invocation.ArgumentList.Arguments.Count > 0
                ? invocation.ArgumentList.Arguments[0].Expression
                : null;

        return new WireOp
        {
            Type = type,
            Name = name,
            SemanticType = semanticType,
            ConstantValue = context.Reading ? null : ConstantOf(argument),
            Condition = ConditionOf(invocation),
            Comment = TrailingComment(invocation),
            Line = CSharpSourceIndex.LineOf(invocation),
        };
    }

    /// <summary>
    /// For a read, the name is wherever the value lands: an object-initializer property, a local, or
    /// a named argument.
    /// </summary>
    private static (string? Name, string? SemanticType) NameForRead(
        InvocationExpressionSyntax invocation
    )
    {
        string? semanticType = null;

        for (SyntaxNode? current = invocation.Parent; current is not null; current = current.Parent)
        {
            switch (current)
            {
                case CastExpressionSyntax cast:
                    semanticType ??= cast.Type.ToString();
                    break;

                case ObjectCreationExpressionSyntax creation when creation.Initializer is null:
                    semanticType ??= creation.Type.ToString();
                    break;

                case AssignmentExpressionSyntax assignment
                    when assignment.Parent is InitializerExpressionSyntax:
                    return (NameFromExpression(assignment.Left), semanticType);

                case VariableDeclaratorSyntax declarator:
                    return (declarator.Identifier.ValueText, semanticType);

                case ArgumentSyntax { NameColon: not null } named:
                    return (named.NameColon!.Name.Identifier.ValueText, semanticType);

                case MethodDeclarationSyntax:
                    return (null, semanticType);
            }
        }

        return (null, semanticType);
    }

    /// <summary>For a write, the name is whatever expression supplied the value.</summary>
    private static (string? Name, string? SemanticType) NameForWrite(
        InvocationExpressionSyntax invocation
    )
    {
        if (invocation.ArgumentList.Arguments.Count == 0)
        {
            return (null, null);
        }

        ExpressionSyntax argument = invocation.ArgumentList.Arguments[0].Expression;
        string? semanticType = argument is CastExpressionSyntax cast ? cast.Type.ToString() : null;

        return (NameFromExpression(argument), semanticType);
    }

    private static string? NameFromExpression(ExpressionSyntax? expression) =>
        expression switch
        {
            null => null,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
            CastExpressionSyntax cast => NameFromExpression(cast.Expression),
            ParenthesizedExpressionSyntax parenthesized => NameFromExpression(
                parenthesized.Expression
            ),
            // `item.Z.ToString()` names the field Z, not ToString.
            InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax inner } =>
                NameFromExpression(inner.Expression),
            PostfixUnaryExpressionSyntax postfix => NameFromExpression(postfix.Operand),
            _ => null,
        };

    private static string? ConstantOf(ExpressionSyntax? expression) =>
        expression switch
        {
            LiteralExpressionSyntax literal => literal.Token.ValueText,
            PrefixUnaryExpressionSyntax { Operand: LiteralExpressionSyntax literal } prefix =>
                prefix.OperatorToken.ValueText + literal.Token.ValueText,
            _ => null,
        };

    /// <summary>
    /// The guard this op sits under, if any. A conditionally written field is not the same shape as
    /// an unconditional one, and flattening the two is how a spec ends up describing a packet the
    /// server never actually sends.
    /// </summary>
    private static string? ConditionOf(SyntaxNode node)
    {
        for (SyntaxNode? current = node.Parent; current is not null; current = current.Parent)
        {
            switch (current)
            {
                case IfStatementSyntax branch when !branch.Condition.Span.Contains(node.Span):
                    return Flatten(branch.Condition.ToString());

                case ConditionalExpressionSyntax conditional
                    when !conditional.Condition.Span.Contains(node.Span):
                    return Flatten(conditional.Condition.ToString());

                case MethodDeclarationSyntax:
                    return null;
            }
        }

        return null;
    }

    private static string? TrailingComment(SyntaxNode node)
    {
        SyntaxTriviaList trivia = node.GetLastToken().TrailingTrivia;

        foreach (SyntaxTrivia item in trivia)
        {
            if (item.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                return Flatten(item.ToString().TrimStart('/').Trim());
            }
        }

        // A fluent chain hangs the comment off the argument list's closing paren, one token further
        // along than the invocation node itself ends.
        SyntaxToken next = node.GetLastToken().GetNextToken();

        foreach (SyntaxTrivia item in next.TrailingTrivia)
        {
            if (item.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                return Flatten(item.ToString().TrimStart('/').Trim());
            }
        }

        return null;
    }

    private static string? MethodName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            _ => null,
        };

    private static string? ReceiverTypeName(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax member)
        {
            return null;
        }

        return member.Expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            ObjectCreationExpressionSyntax creation => creation.Type.ToString(),
            MemberAccessExpressionSyntax nested => nested.Name.Identifier.ValueText,
            _ => null,
        };
    }

    /// <summary>Collapses source whitespace so an expression stays one diffable line in YAML.</summary>
    public static string Flatten(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
