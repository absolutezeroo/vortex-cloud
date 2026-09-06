using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>A friend request was accepted — one signal per side, since both gained a friend.</summary>
public sealed class FriendAcceptedTranslator : ISignalTranslator<FriendRequestAcceptedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.AcceptFriend, [Facts.Player], TargetKind: FactKind.PlayerId)];

    public ImmutableArray<ProgressSignal> Translate(FriendRequestAcceptedEvent e) =>
        [
            new(
                e.ActorPlayerId,
                SignalActions.AcceptFriend,
                1,
                SignalValue.Id(e.TargetPlayerId),
                SignalFacts.Build().Id(Facts.Player, e.TargetPlayerId)
            ),
            new(
                e.TargetPlayerId,
                SignalActions.AcceptFriend,
                1,
                SignalValue.Id(e.ActorPlayerId),
                SignalFacts.Build().Id(Facts.Player, e.ActorPlayerId)
            ),
        ];
}
