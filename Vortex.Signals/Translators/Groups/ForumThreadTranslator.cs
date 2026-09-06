using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Started a forum thread.</summary>
public sealed class ForumThreadTranslator : ISignalTranslator<ForumThreadCreatedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [
        new(
            SignalActions.CreateForumThread,
            [Facts.Group, Facts.Thread],
            TargetKind: FactKind.OpaqueId
        ),
    ];

    public ImmutableArray<ProgressSignal> Translate(ForumThreadCreatedEvent e) =>
        [
            new(
                e.ActorPlayerId,
                SignalActions.CreateForumThread,
                1,
                SignalValue.Id(e.ThreadId),
                SignalFacts.Build().Id(Facts.Group, e.GroupId).Id(Facts.Thread, e.ThreadId)
            ),
        ];
}
