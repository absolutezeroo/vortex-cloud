using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Posted on a forum thread.</summary>
public sealed class ForumPostTranslator : ISignalTranslator<ForumPostCreatedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [
        new(
            SignalActions.CreateForumPost,
            [Facts.Group, Facts.Thread],
            TargetKind: FactKind.OpaqueId
        ),
    ];

    public ImmutableArray<ProgressSignal> Translate(ForumPostCreatedEvent e) =>
        [
            new(
                e.ActorPlayerId,
                SignalActions.CreateForumPost,
                1,
                SignalValue.Id(e.ThreadId),
                SignalFacts.Build().Id(Facts.Group, e.GroupId).Id(Facts.Thread, e.ThreadId)
            ),
        ];
}
