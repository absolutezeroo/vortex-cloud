using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>
/// Room entries.
/// </summary>
/// <remarks>
/// The target is the room id, which is what a distinct-mode task deduplicates on — "visit 20
/// different rooms" counts twenty rooms, not twenty doorways.
/// <para>
/// <c>room_owner</c> is deliberately not declared. The event does carry an <c>OwnerId</c>, added so
/// that "join their flat" would be expressible, but the only publish site
/// (<c>PlayerPresenceGrain.Room</c>) never passes it, so it is always zero. Declaring a fact that is
/// always absent is the exact defect this subsystem exists to prevent; populating it at the publish
/// site first is a two-line job, and then the fact can be declared honestly.
/// </para>
/// </remarks>
public sealed class RoomEntryTranslator : ISignalTranslator<PlayerEnteredRoomEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.EnterOtherUsersRoom, [Facts.Room], TargetKind: FactKind.RoomId)];

    public ImmutableArray<ProgressSignal> Translate(PlayerEnteredRoomEvent e) =>
        [
            new(
                e.PlayerId.Value,
                SignalActions.EnterOtherUsersRoom,
                1,
                Target: SignalValue.Id(e.RoomId),
                SignalFacts.Build().Id(Facts.Room, e.RoomId)
            ),
        ];
}
