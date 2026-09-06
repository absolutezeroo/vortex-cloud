using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Friend requests sent. The asking, not the accepting — the client's own task says "make friends".</summary>
public sealed class FriendRequestTranslator : ISignalTranslator<FriendRequestSentEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.RequestFriend, [Facts.Player], TargetKind: FactKind.PlayerId)];

    public ImmutableArray<ProgressSignal> Translate(FriendRequestSentEvent e) =>
        [
            new(
                e.ActorPlayerId,
                SignalActions.RequestFriend,
                1,
                Target: SignalValue.Id(e.TargetPlayerId),
                SignalFacts.Build().Id(Facts.Player, e.TargetPlayerId)
            ),
        ];
}
