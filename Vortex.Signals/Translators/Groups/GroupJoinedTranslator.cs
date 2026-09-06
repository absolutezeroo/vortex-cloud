using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Joined a guild.</summary>
public sealed class GroupJoinedTranslator : ISignalTranslator<GroupMemberJoinedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.JoinGroup, [Facts.Group], TargetKind: FactKind.OpaqueId)];

    public ImmutableArray<ProgressSignal> Translate(GroupMemberJoinedEvent e) =>
        [
            new(
                e.ActorPlayerId,
                SignalActions.JoinGroup,
                1,
                SignalValue.Id(e.GroupId),
                SignalFacts.Build().Id(Facts.Group, e.GroupId)
            ),
        ];
}
