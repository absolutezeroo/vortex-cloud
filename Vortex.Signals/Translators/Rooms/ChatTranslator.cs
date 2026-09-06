using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>
/// Chat the room accepted.
/// </summary>
/// <remarks>
/// Whispers do not count: a "chat with users" task is about talking to a room, and a whisper to
/// yourself would otherwise farm it. Deliberately driven by <c>PlayerChattedEvent</c> rather than
/// the cancellable <c>PlayerChattingEvent</c>, which fires for lines a behaviour then drops.
/// </remarks>
public sealed class ChatTranslator : ISignalTranslator<PlayerChattedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.ChatWithSomeone, [Facts.Room])];

    public ImmutableArray<ProgressSignal> Translate(PlayerChattedEvent e) =>
        e.Whisper
            ? []
            :
            [
                new(
                    e.PlayerId.Value,
                    SignalActions.ChatWithSomeone,
                    1,
                    Target: null,
                    SignalFacts.Build().Id(Facts.Room, e.RoomId)
                ),
            ];
}
