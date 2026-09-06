using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>
/// Opened a mystery box.
/// </summary>
/// <remarks>
/// Credited to whoever held the key, not to whoever owns the box: the key holder is the one who
/// acted, and rewarding the owner would pay for standing still in their own room.
/// </remarks>
public sealed class MysteryBoxTranslator : ISignalTranslator<MysteryBoxOpenedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.OpenMysteryBox, [Facts.Colour, Facts.Room], TargetKind: FactKind.Text)];

    public ImmutableArray<ProgressSignal> Translate(MysteryBoxOpenedEvent e) =>
        [
            new(
                e.KeyHolderPlayerId,
                SignalActions.OpenMysteryBox,
                1,
                e.Color,
                SignalFacts.Build().Text(Facts.Colour, e.Color).Id(Facts.Room, e.RoomId)
            ),
        ];
}
