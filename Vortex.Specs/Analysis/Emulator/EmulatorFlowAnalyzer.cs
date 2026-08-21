using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Vortex.Specs.Model;
using Vortex.Specs.Naming;
using Vortex.Specs.Sources;

namespace Vortex.Specs.Analysis.Emulator;

/// <summary>
/// Follows each packet handler into the domain and records what this emulator actually does with the
/// message: which layers it crosses, which guards it applies, what it writes, and which composers
/// come back out to whom.
/// </summary>
/// <remarks>
/// Every observation is attributed to <see cref="EvidenceAuthority.VortexEmulator"/>. The output
/// describes this codebase, and the pipeline above is built so that "Vortex does X" can never on its
/// own become "Habbo does X".
/// </remarks>
public sealed class EmulatorFlowAnalyzer(
    SpecWorkspace workspace,
    CSharpSourceIndex index,
    int maxDepth = 6,
    int maxMethods = 140
)
{
    private const string Origin = "vortex";

    /// <summary>How an outgoing composer reached the wire, and therefore who received it.</summary>
    private static readonly (
        string Method,
        string? ReceiverType,
        Recipient Recipient,
        Confidence Confidence
    )[] SendRules =
    [
        ("SendComposerToRoomAsync", null, Recipient.RoomUsers, Confidence.ImplementationObserved),
        ("BroadcastAsync", null, Recipient.RoomUsers, Confidence.ImplementationObserved),
        (
            "SendComposerAsync",
            "ISessionContext",
            Recipient.Actor,
            Confidence.ImplementationObserved
        ),
        ("SendComposerAsync", "MessageContext", Recipient.Actor, Confidence.ImplementationObserved),
        ("SendComposerAsync", "IPlayerPresenceGrain", Recipient.TargetUser, Confidence.Inferred),
    ];

    private readonly MethodIndex _methods = new(index);

    public IReadOnlyList<EmulatorFlow> Scan()
    {
        List<EmulatorFlow> flows = [];

        foreach (
            IndexedType handler in index.Types.OrderBy(t => t.File.Path, StringComparer.Ordinal)
        )
        {
            string? messageType = HandledMessageType(handler);

            if (messageType is null)
            {
                continue;
            }

            MethodDeclarationSyntax? handle = handler
                .Declaration.Members.OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.ValueText == "HandleAsync");

            if (handle is null)
            {
                continue;
            }

            flows.Add(Trace(handler, handle, messageType));
        }

        return flows;
    }

    private static string? HandledMessageType(IndexedType type)
    {
        foreach (string baseType in type.BaseTypes)
        {
            if (!baseType.StartsWith("IMessageHandler<", StringComparison.Ordinal))
            {
                continue;
            }

            return baseType["IMessageHandler<".Length..].TrimEnd('>').Trim();
        }

        return null;
    }

    private sealed class Walk
    {
        public List<FeatureFlowStep> Steps { get; } = [];

        public List<FeatureCheck> Checks { get; } = [];

        public List<FeatureMutation> Mutations { get; } = [];

        public List<FeatureOutgoing> Outgoing { get; } = [];

        public List<string> Unresolved { get; } = [];

        public HashSet<string> Visited { get; } = new(StringComparer.Ordinal);

        public bool ReachesPersistence { get; set; }

        /// <summary>The first call the handler makes for effect. Anchors the feature id.</summary>
        public string? PrimaryOperation { get; set; }

        /// <summary>
        /// The last call the handler makes for its return value, used only when it never makes one
        /// for effect. A handler that only looks things up is still named after what it looked up
        /// rather than after a generic helper it happened to reach first.
        /// </summary>
        public string? ValueProducingOperation { get; set; }

        public int Order { get; set; }
    }

    private EmulatorFlow Trace(
        IndexedType handler,
        MethodDeclarationSyntax handle,
        string messageType
    )
    {
        Walk walk = new();

        walk.Steps.Add(
            new FeatureFlowStep
            {
                Order = walk.Order++,
                Layer = "handler",
                Symbol = $"{handler.Name}.HandleAsync",
                Evidence = Evidence(EvidenceKind.EmulatorHandler, handler, handler.Line),
            }
        );

        VisitMethod(new IndexedMethod(handler, handle), walk, depth: 0);

        return new EmulatorFlow
        {
            HandlerType = handler.Name,
            MessageType = messageType,
            PrimaryOperation = walk.PrimaryOperation ?? walk.ValueProducingOperation,
            Steps = walk.Steps,
            Checks = Distinct(walk.Checks, c => c.Expression + "|" + c.OnFail),
            Mutations = Distinct(walk.Mutations, m => m.Target + "=" + m.Expression),
            Outgoing = Distinct(walk.Outgoing, o => o.Packet + "|" + o.Recipient),
            ReachesPersistence = walk.ReachesPersistence,
            IsOrchestrationOnly = IsOrchestrationOnly(handle),
            Unresolved =
            [
                .. walk
                    .Unresolved.Distinct(StringComparer.Ordinal)
                    .OrderBy(u => u, StringComparer.Ordinal),
            ],
        };
    }

    private static IReadOnlyList<T> Distinct<T>(List<T> items, Func<T, string> key)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        List<T> result = [];

        foreach (T item in items)
        {
            if (seen.Add(key(item)))
            {
                result.Add(item);
            }
        }

        return result;
    }

    /// <summary>
    /// A handler that only forwards satisfies this repository's orchestration-only rule. Recorded
    /// because a handler that stops satisfying it is a contract break the spec should show.
    /// </summary>
    private static bool IsOrchestrationOnly(MethodDeclarationSyntax handle) =>
        !handle
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Any(i =>
                i.Expression is MemberAccessExpressionSyntax member
                && member.Name.Identifier.ValueText
                    is "SaveChangesAsync"
                        or "ToListAsync"
                        or "FirstOrDefaultAsync"
                        or "ExecuteUpdateAsync"
                        or "ExecuteDeleteAsync"
            );

    private void VisitMethod(IndexedMethod method, Walk walk, int depth)
    {
        string key = $"{method.Owner.FullName}.{method.Name}";

        if (depth > maxDepth || walk.Visited.Count >= maxMethods || !walk.Visited.Add(key))
        {
            if (depth > maxDepth)
            {
                walk.Unresolved.Add($"{key} (deeper than the {maxDepth}-hop walk limit)");
            }
            else if (walk.Visited.Count >= maxMethods)
            {
                walk.Unresolved.Add(
                    $"walk stopped at {maxMethods} methods; the flow is not exhaustive"
                );
            }

            return;
        }

        SyntaxNode? body =
            (SyntaxNode?)method.Declaration.Body ?? method.Declaration.ExpressionBody;

        if (body is null)
        {
            return;
        }

        LocalTypeTable locals = new(
            method.Owner,
            method.Declaration,
            _methods,
            index.FindAllParts(method.Owner.Name)
        );

        CollectChecks(body, method, walk);
        CollectMutations(body, method, locals, walk);
        CollectOutgoing(body, method, locals, walk);

        foreach (
            InvocationExpressionSyntax invocation in body.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
        )
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax member)
            {
                continue;
            }

            string name = member.Name.Identifier.ValueText;

            if (name is "ConfigureAwait" or "Ignore" or "LogAndForget" or "ToString")
            {
                continue;
            }

            string? receiverType = locals.TypeOfExpression(member.Expression);

            if (IsPersistence(name, receiverType))
            {
                walk.ReachesPersistence = true;
            }

            IndexedMethod? target = _methods.Resolve(receiverType, name);

            if (target is null || !IsDomainHop(target))
            {
                continue;
            }

            // A handler calling something on what it was handed — `ctx.AsActionContext()`,
            // `message.Something` — is doing its own plumbing, not entering the domain. Recording
            // those as flow steps puts MessageContext in the middle of every feature's call chain.
            if (depth == 0 && locals.IsMethodParameterExpression(member.Expression))
            {
                continue;
            }

            string layer = LayerOf(target);

            // Only what the handler itself calls on one of its injected collaborators names the
            // feature. Two restrictions, both earned: following the chain deeper turns whatever
            // shared lookup a service reaches first into the feature name, and counting calls on the
            // message context names every feature after SendComposerAsync. Either way dozens of
            // unrelated handlers collapse into one meaningless feature.
            if (depth == 0 && locals.IsDependencyExpression(member.Expression))
            {
                if (IsCalledForEffect(invocation))
                {
                    walk.PrimaryOperation ??= name;
                }
                else
                {
                    walk.ValueProducingOperation = name;
                }
            }

            walk.Steps.Add(
                new FeatureFlowStep
                {
                    Order = walk.Order++,
                    Layer = layer,
                    Symbol = $"{target.Owner.Name}.{target.Name}",
                    Evidence = Evidence(EvidenceKind.EmulatorFlow, target.Owner, target.Line),
                }
            );

            VisitMethod(target, walk, depth + 1);
        }
    }

    /// <summary>
    /// True when the call's result is discarded — the statement exists to make something happen
    /// rather than to fetch a value. <c>await x.DoAsync(...).ConfigureAwait(false);</c> is effectful;
    /// <c>Foo f = await x.GetAsync(...);</c> is not.
    /// </summary>
    private static bool IsCalledForEffect(SyntaxNode invocation)
    {
        for (SyntaxNode? current = invocation.Parent; current is not null; current = current.Parent)
        {
            switch (current)
            {
                case AwaitExpressionSyntax
                or ParenthesizedExpressionSyntax
                or MemberAccessExpressionSyntax
                or InvocationExpressionSyntax:
                    continue;

                case ExpressionStatementSyntax
                or ReturnStatementSyntax
                or ArrowExpressionClauseSyntax:
                    return true;

                default:
                    return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Keeps the walk inside the emulator's own domain code. Without this the chain wanders into
    /// logging, LINQ and framework helpers and the resulting "flow" says nothing about the feature.
    /// </summary>
    private bool IsDomainHop(IndexedMethod method)
    {
        string path = workspace.Relative(method.Owner.File.Path);

        if (!path.StartsWith("Vortex.", StringComparison.Ordinal))
        {
            return false;
        }

        return !path.StartsWith("Vortex.Revisions/", StringComparison.Ordinal)
            && !path.StartsWith("Vortex.Logging/", StringComparison.Ordinal)
            && !path.StartsWith("Vortex.Networking/", StringComparison.Ordinal)
            && !path.Contains(".Tests/", StringComparison.Ordinal);
    }

    private static string LayerOf(IndexedMethod method)
    {
        string owner = method.Owner.Name;

        if (owner.EndsWith("PersistenceGrain", StringComparison.Ordinal))
        {
            return "persistence";
        }

        if (owner.EndsWith("Grain", StringComparison.Ordinal))
        {
            return "grain";
        }

        if (owner.EndsWith("Service", StringComparison.Ordinal))
        {
            return "service";
        }

        if (
            owner.EndsWith("Module", StringComparison.Ordinal)
            || owner.EndsWith("System", StringComparison.Ordinal)
        )
        {
            return "module";
        }

        return "domain";
    }

    private static bool IsPersistence(string method, string? receiverType) =>
        method
            is "SaveChangesAsync"
                or "ExecuteUpdateAsync"
                or "ExecuteDeleteAsync"
                or "AddAsync"
                or "FlushAsync"
        || (receiverType?.EndsWith("DbContext", StringComparison.Ordinal) ?? false);

    private void CollectChecks(SyntaxNode body, IndexedMethod method, Walk walk)
    {
        foreach (IfStatementSyntax branch in body.DescendantNodes().OfType<IfStatementSyntax>())
        {
            string? onFail = GuardOutcome(branch.Statement);

            if (onFail is null)
            {
                continue;
            }

            walk.Checks.Add(
                new FeatureCheck
                {
                    Expression = WireLayoutExtractor.Flatten(branch.Condition.ToString()),
                    OnFail = onFail,
                    Evidence = Evidence(
                        EvidenceKind.EmulatorFlow,
                        method.Owner,
                        CSharpSourceIndex.LineOf(branch)
                    ),
                }
            );
        }
    }

    /// <summary>
    /// Classifies what a guarded branch does. Only early exits count: an <c>if</c> whose body is the
    /// happy path is control flow, not a precondition.
    /// </summary>
    private static string? GuardOutcome(StatementSyntax statement)
    {
        List<StatementSyntax> statements = statement is BlockSyntax block
            ? [.. block.Statements]
            : [statement];

        if (statements.Count == 0)
        {
            return null;
        }

        bool sends = statements.Any(s =>
            s.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Any(i =>
                    i.Expression is MemberAccessExpressionSyntax m
                    && m.Name.Identifier.ValueText.StartsWith("Send", StringComparison.Ordinal)
                )
        );

        StatementSyntax last = statements[^1];

        return last switch
        {
            ThrowStatementSyntax => "throw",
            ReturnStatementSyntax when sends => "send_and_return",
            ReturnStatementSyntax => "return",
            _ => null,
        };
    }

    private void CollectMutations(
        SyntaxNode body,
        IndexedMethod method,
        LocalTypeTable locals,
        Walk walk
    )
    {
        foreach (
            AssignmentExpressionSyntax assignment in body.DescendantNodes()
                .OfType<AssignmentExpressionSyntax>()
        )
        {
            // Object-initializer assignments build a value; they do not mutate existing state.
            if (assignment.Parent is InitializerExpressionSyntax)
            {
                continue;
            }

            if (assignment.Left is not MemberAccessExpressionSyntax target)
            {
                continue;
            }

            // A write to an object the method built itself is scratch work. A write to one it
            // looked up — `_state.ItemsById.TryGetValue(id, out IRoomItem item); item.X = x;` — is
            // the state change the feature exists to make, and excluding every local lost it.
            if (
                target.Expression is IdentifierNameSyntax root
                && locals.IsFreshlyConstructed(root.Identifier.ValueText)
            )
            {
                continue;
            }

            walk.Mutations.Add(
                new FeatureMutation
                {
                    Target = WireLayoutExtractor.Flatten(target.ToString()),
                    Expression = WireLayoutExtractor.Flatten(assignment.Right.ToString()),
                    Evidence = Evidence(
                        EvidenceKind.EmulatorFlow,
                        method.Owner,
                        CSharpSourceIndex.LineOf(assignment)
                    ),
                }
            );
        }

        CollectMutatorCalls(body, method, locals, walk);
    }

    /// <summary>
    /// Records state changes made through a setter rather than an assignment.
    /// </summary>
    /// <remarks>
    /// The room objects here expose <c>item.SetPosition(x, y)</c> rather than a settable property, so
    /// an assignment-only scan reports "this feature changes nothing" for moving a piece of
    /// furniture. Recognising the call by its name is a heuristic and is labelled as one in the
    /// output: what is recorded is the call as written, never an interpretation of what it does.
    /// </remarks>
    private void CollectMutatorCalls(
        SyntaxNode body,
        IndexedMethod method,
        LocalTypeTable locals,
        Walk walk
    )
    {
        foreach (
            InvocationExpressionSyntax invocation in body.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
        )
        {
            if (
                invocation.Expression is not MemberAccessExpressionSyntax member
                || !IsMutatorName(member.Name.Identifier.ValueText)
            )
            {
                continue;
            }

            if (
                member.Expression is IdentifierNameSyntax root
                && locals.IsFreshlyConstructed(root.Identifier.ValueText)
            )
            {
                continue;
            }

            walk.Mutations.Add(
                new FeatureMutation
                {
                    Target = WireLayoutExtractor.Flatten(member.Expression.ToString()),
                    Expression = WireLayoutExtractor.Flatten(invocation.ToString()),
                    Evidence = Evidence(
                        EvidenceKind.EmulatorFlow,
                        method.Owner,
                        CSharpSourceIndex.LineOf(invocation)
                    ),
                }
            );
        }
    }

    private static readonly string[] MutatorPrefixes =
    [
        "Set",
        "Add",
        "Remove",
        "Clear",
        "Update",
        "Apply",
        "Reset",
        "Toggle",
        "Increment",
        "Decrement",
        "Place",
        "Delete",
        "Enqueue",
        "Assign",
    ];

    private static bool IsMutatorName(string name)
    {
        foreach (string prefix in MutatorPrefixes)
        {
            if (
                name.Length > prefix.Length
                && name.StartsWith(prefix, StringComparison.Ordinal)
                && char.IsUpper(name[prefix.Length])
            )
            {
                return true;
            }
        }

        return false;
    }

    private void CollectOutgoing(
        SyntaxNode body,
        IndexedMethod method,
        LocalTypeTable locals,
        Walk walk
    )
    {
        foreach (
            ObjectCreationExpressionSyntax creation in body.DescendantNodes()
                .OfType<ObjectCreationExpressionSyntax>()
        )
        {
            string typeName = MethodIndex.StripGenerics(creation.Type.ToString());

            if (!typeName.EndsWith("Composer", StringComparison.Ordinal))
            {
                continue;
            }

            (Recipient recipient, Confidence confidence) = RecipientOf(creation, locals);

            walk.Outgoing.Add(
                new FeatureOutgoing
                {
                    Packet = PacketNaming.Canonical(typeName),
                    Recipient = recipient,
                    RecipientConfidence = confidence,
                    Evidence = Evidence(
                        EvidenceKind.EmulatorFlow,
                        method.Owner,
                        CSharpSourceIndex.LineOf(creation)
                    ),
                }
            );
        }

        CollectIndirectSends(body, method, locals, walk);
    }

    /// <summary>
    /// Records sends whose payload is not built on the spot.
    /// </summary>
    /// <remarks>
    /// <c>SendComposerToRoomAsync(item.GetUpdateComposer())</c> is the single most consequential
    /// pattern in this codebase's outbound path and a scan for <c>new SomethingComposer</c> misses it
    /// completely. Left unhandled, a feature that broadcasts a furniture update to the whole room
    /// reads as if it only answers the actor — the recipient is part of the behaviour, so that is not
    /// a small omission.
    /// </remarks>
    private void CollectIndirectSends(
        SyntaxNode body,
        IndexedMethod method,
        LocalTypeTable locals,
        Walk walk
    )
    {
        foreach (
            InvocationExpressionSyntax invocation in body.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
        )
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax member)
            {
                continue;
            }

            string name = member.Name.Identifier.ValueText;

            if (!IsSendMethod(name))
            {
                continue;
            }

            foreach (ArgumentSyntax argument in invocation.ArgumentList.Arguments)
            {
                if (argument.Expression is ObjectCreationExpressionSyntax)
                {
                    // Already recorded by the direct scan above, with the same recipient.
                    continue;
                }

                IReadOnlyList<string> candidates = ResolveComposerNames(
                    argument.Expression,
                    locals
                );

                if (candidates.Count == 0 && !LooksLikeComposer(argument.Expression, locals))
                {
                    continue;
                }

                (Recipient recipient, Confidence confidence) = RecipientOf(invocation, locals);
                EvidenceRef evidence = Evidence(
                    EvidenceKind.EmulatorFlow,
                    method.Owner,
                    CSharpSourceIndex.LineOf(invocation)
                );

                if (candidates.Count == 0)
                {
                    walk.Outgoing.Add(
                        new FeatureOutgoing
                        {
                            Packet = FeatureOutgoing.UnresolvedPacket,
                            Recipient = recipient,
                            RecipientConfidence = confidence,
                            Note =
                                "the payload comes back from "
                                + WireLayoutExtractor.Flatten(argument.Expression.ToString())
                                + ", which this walk could not resolve to one packet",
                            Evidence = evidence,
                        }
                    );

                    continue;
                }

                foreach (string candidate in candidates)
                {
                    walk.Outgoing.Add(
                        new FeatureOutgoing
                        {
                            Packet = candidate,
                            Recipient = recipient,
                            RecipientConfidence =
                                candidates.Count == 1 ? confidence : Confidence.Inferred,
                            Note =
                                candidates.Count == 1
                                    ? null
                                    : "one of several composers the call can return, depending on the item type",
                            Evidence = evidence,
                        }
                    );
                }
            }
        }
    }

    private static bool IsSendMethod(string name) =>
        name is "SendComposerAsync" or "SendComposerToRoomAsync" or "SendAsync"
        || name.StartsWith("Broadcast", StringComparison.Ordinal);

    private bool LooksLikeComposer(ExpressionSyntax expression, LocalTypeTable locals)
    {
        string? type = locals.TypeOfExpression(expression);

        return type is not null
            && (
                type.EndsWith("Composer", StringComparison.Ordinal)
                || type is "IComposer"
                || type.Contains("IComposer", StringComparison.Ordinal)
            );
    }

    /// <summary>
    /// Works out which packet a send's argument carries. A concrete composer type answers it
    /// directly; a virtual call declared to return <c>IComposer</c> is answered by looking at what
    /// its implementations construct, which can legitimately be more than one.
    /// </summary>
    private IReadOnlyList<string> ResolveComposerNames(
        ExpressionSyntax expression,
        LocalTypeTable locals
    )
    {
        switch (expression)
        {
            case ObjectCreationExpressionSyntax creation:
            {
                string type = MethodIndex.StripGenerics(creation.Type.ToString());

                return type.EndsWith("Composer", StringComparison.Ordinal)
                    ? [PacketNaming.Canonical(type)]
                    : [];
            }

            case InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member }:
            {
                if (member.Name.Identifier.ValueText == "ConfigureAwait")
                {
                    return ResolveComposerNames(member.Expression, locals);
                }

                string method = member.Name.Identifier.ValueText;
                string? receiverType = locals.TypeOfExpression(member.Expression);
                IndexedMethod? exact = receiverType is null
                    ? null
                    : _methods.Resolve(receiverType, method);

                // When the receiver's type is known, only that implementation speaks. Otherwise every
                // override of the name is a candidate, and several of them is a real answer: a
                // virtual GetUpdateComposer legitimately returns a different packet per item kind.
                IEnumerable<IndexedMethod> candidates = exact is not null
                    ? [exact]
                    : _methods.ByName(method);
                List<string> names = [];

                foreach (IndexedMethod candidate in candidates)
                {
                    string returnType = MethodIndex.StripGenerics(candidate.ReturnType);

                    if (
                        returnType.EndsWith("Composer", StringComparison.Ordinal)
                        && returnType is not ("IComposer" or "Composer")
                    )
                    {
                        names.Add(PacketNaming.Canonical(returnType));
                        continue;
                    }

                    if (returnType is not ("IComposer" or "Composer"))
                    {
                        continue;
                    }

                    names.AddRange(ComposersConstructedIn(candidate));
                }

                return
                [
                    .. names
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(n => n, StringComparer.Ordinal),
                ];
            }

            case AwaitExpressionSyntax awaited:
                return ResolveComposerNames(awaited.Expression, locals);

            case IdentifierNameSyntax identifier:
            {
                string? type = locals.TypeOf(identifier.Identifier.ValueText);

                return
                    type is not null
                    && type.EndsWith("Composer", StringComparison.Ordinal)
                    && type != "IComposer"
                    ? [PacketNaming.Canonical(MethodIndex.StripGenerics(type))]
                    : [];
            }

            default:
                return [];
        }
    }

    private static IEnumerable<string> ComposersConstructedIn(IndexedMethod method)
    {
        SyntaxNode? body =
            (SyntaxNode?)method.Declaration.Body ?? method.Declaration.ExpressionBody;

        if (body is null)
        {
            yield break;
        }

        foreach (
            ObjectCreationExpressionSyntax creation in body.DescendantNodes()
                .OfType<ObjectCreationExpressionSyntax>()
        )
        {
            string type = MethodIndex.StripGenerics(creation.Type.ToString());

            if (type.EndsWith("Composer", StringComparison.Ordinal))
            {
                yield return PacketNaming.Canonical(type);
            }
        }
    }

    /// <summary>
    /// Works out who receives a composer from the call that carries it. When no send call encloses
    /// the construction — the composer is handed to something else first — the recipient stays
    /// <see cref="Recipient.Unknown"/> rather than defaulting to the actor.
    /// </summary>
    private static (Recipient Recipient, Confidence Confidence) RecipientOf(
        SyntaxNode payload,
        LocalTypeTable locals
    )
    {
        // Starts at the node itself, not its parent: callers pass either the composer construction
        // (whose enclosing call is the send) or the send call directly, and the second case has no
        // enclosing send to find.
        for (SyntaxNode? current = payload; current is not null; current = current.Parent)
        {
            if (current is not InvocationExpressionSyntax invocation)
            {
                if (current is MethodDeclarationSyntax)
                {
                    break;
                }

                continue;
            }

            if (invocation.Expression is not MemberAccessExpressionSyntax member)
            {
                continue;
            }

            string name = member.Name.Identifier.ValueText;
            string? receiverType = locals.TypeOfExpression(member.Expression);

            foreach (
                (string rule, string? type, Recipient recipient, Confidence confidence) in SendRules
            )
            {
                if (!string.Equals(rule, name, StringComparison.Ordinal))
                {
                    continue;
                }

                if (type is null)
                {
                    return (recipient, confidence);
                }

                if (receiverType is not null && MethodIndex.StripGenerics(receiverType) == type)
                {
                    return (recipient, confidence);
                }
            }

            if (name.StartsWith("Broadcast", StringComparison.Ordinal))
            {
                return (Recipient.RoomUsers, Confidence.Inferred);
            }
        }

        return (Recipient.Unknown, Confidence.Unknown);
    }

    private EvidenceRef Evidence(EvidenceKind kind, IndexedType type, int line) =>
        new()
        {
            Kind = kind,
            Authority = EvidenceAuthority.VortexEmulator,
            Origin = Origin,
            Source = workspace.Relative(type.File.Path),
            Symbol = type.Name,
            Line = line,
        };
}
