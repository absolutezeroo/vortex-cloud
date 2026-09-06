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
/// <para>
/// The line itself is a fact, which is what "say something with the word X in it" is made of. It is
/// only ever useful with <c>Contains</c>; an exact match on something a player typed is a filter
/// that never fires, and <see cref="Facts.ChatMessage"/> says so.
/// </para>
/// </remarks>
public sealed class ChatTranslator : ISignalTranslator<PlayerChattedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.ChatWithSomeone, [Facts.Room, Facts.ChatMessage])];

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
                    SignalFacts.Build().Id(Facts.Room, e.RoomId).Text(Facts.ChatMessage, e.Text)
                ),
            ];
}
