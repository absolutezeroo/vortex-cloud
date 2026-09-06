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

/// <summary>Respect given. The target is who received it, so a distinct task can require different people.</summary>
public sealed class RespectGivenTranslator : ISignalTranslator<RespectGivenEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.GiveRespect, [Facts.Player], TargetKind: FactKind.PlayerId)];

    public ImmutableArray<ProgressSignal> Translate(RespectGivenEvent e) =>
        [
            new(
                e.ActorPlayerId,
                SignalActions.GiveRespect,
                1,
                Target: SignalValue.Id(e.TargetPlayerId),
                SignalFacts.Build().Id(Facts.Player, e.TargetPlayerId)
            ),
        ];
}

/// <summary>Private messages the messenger accepted.</summary>
public sealed class MessengerTranslator : ISignalTranslator<MessengerMessageSentEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.SendMessengerMessage, [Facts.Player], TargetKind: FactKind.PlayerId)];

    public ImmutableArray<ProgressSignal> Translate(MessengerMessageSentEvent e) =>
        [
            new(
                e.PlayerId.Value,
                SignalActions.SendMessengerMessage,
                1,
                Target: SignalValue.Id(e.ReceiverId.Value),
                SignalFacts.Build().Id(Facts.Player, e.ReceiverId.Value)
            ),
        ];
}

/// <summary>Figure changes.</summary>
public sealed class FigureTranslator : ISignalTranslator<PlayerFigureChangedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.ChangeFigure, [])];

    public ImmutableArray<ProgressSignal> Translate(PlayerFigureChangedEvent e) =>
        [new(e.PlayerId.Value, SignalActions.ChangeFigure, 1, Target: null, [])];
}

/// <summary>Motto changes.</summary>
public sealed class MottoTranslator : ISignalTranslator<PlayerMottoChangedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.ChangeMotto, [])];

    public ImmutableArray<ProgressSignal> Translate(PlayerMottoChangedEvent e) =>
        [new(e.PlayerId.Value, SignalActions.ChangeMotto, 1, Target: null, [])];
}

/// <summary>Badges equipped. One signal per badge worn, so a target can name a specific one.</summary>
public sealed class BadgeTranslator : ISignalTranslator<BadgesEquippedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.WearBadge, [Facts.Badge], TargetKind: FactKind.BadgeCode)];

    public ImmutableArray<ProgressSignal> Translate(BadgesEquippedEvent e)
    {
        if (e.BadgeCodes.IsDefaultOrEmpty)
        {
            return [];
        }

        ImmutableArray<ProgressSignal>.Builder builder =
            ImmutableArray.CreateBuilder<ProgressSignal>(e.BadgeCodes.Length);

        foreach (string badgeCode in e.BadgeCodes)
        {
            builder.Add(
                new ProgressSignal(
                    e.PlayerId.Value,
                    SignalActions.WearBadge,
                    1,
                    badgeCode,
                    SignalFacts.Build().Text(Facts.Badge, badgeCode)
                )
            );
        }

        return builder.MoveToImmutable();
    }
}
