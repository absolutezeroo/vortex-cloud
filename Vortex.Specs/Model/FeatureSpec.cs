using System.Collections.Generic;

namespace Vortex.Specs.Model;

/// <summary>
/// Who an outgoing message goes to. The recipient is part of the behaviour: a packet that reaches
/// the actor when it should reach the room is a bug that "the right packet was sent" hides.
/// </summary>
public enum Recipient
{
    /// <summary>Nothing in the analyzed source said. Never guess this one.</summary>
    Unknown = 0,

    /// <summary>The session that sent the triggering packet.</summary>
    Actor,

    /// <summary>Every session in the room, actor included.</summary>
    RoomUsers,

    /// <summary>Every session in the room except the actor.</summary>
    OtherRoomUsers,

    /// <summary>A specific player who is not the actor.</summary>
    TargetUser,

    /// <summary>The room's owner.</summary>
    Owner,

    /// <summary>Sessions holding a moderation capability.</summary>
    Moderators,

    /// <summary>Every connected session.</summary>
    Global,
}

public static class RecipientNames
{
    public static string Wire(this Recipient recipient) =>
        recipient switch
        {
            Recipient.Actor => "actor",
            Recipient.RoomUsers => "room_users",
            Recipient.OtherRoomUsers => "other_room_users",
            Recipient.TargetUser => "target_user",
            Recipient.Owner => "owner",
            Recipient.Moderators => "moderators",
            Recipient.Global => "global",
            _ => "unknown",
        };
}

/// <summary>
/// A guard observed on the code path. The text is the source expression verbatim: paraphrasing a
/// condition is the point at which a spec starts describing something no implementation does.
/// </summary>
public sealed record FeatureCheck
{
    public required string Expression { get; init; }

    /// <summary>What the code does when the guard trips: <c>return</c>, <c>throw</c>, <c>send</c>.</summary>
    public required string OnFail { get; init; }

    public required EvidenceRef Evidence { get; init; }
}

/// <summary>One hop of the observed call chain.</summary>
public sealed record FeatureFlowStep
{
    public required int Order { get; init; }

    /// <summary><c>handler</c>, <c>service</c>, <c>grain</c>, <c>module</c>, <c>persistence</c>.</summary>
    public required string Layer { get; init; }

    public required string Symbol { get; init; }

    public required EvidenceRef Evidence { get; init; }
}

public sealed record FeatureOutgoing
{
    public required string Packet { get; init; }

    public required Recipient Recipient { get; init; }

    public Confidence RecipientConfidence { get; init; } = Confidence.Unknown;

    /// <summary>
    /// Position in the emitted sequence when the order is known from a capture. Null means the order
    /// was never observed — which is not the same as "the order does not matter".
    /// </summary>
    public int? Order { get; init; }

    /// <summary>
    /// Why the packet could not be named, when it could not. A send whose payload comes back from a
    /// virtual call is still a send, and recording it as "something goes to the room here" beats
    /// leaving the broadcast out of the spec entirely.
    /// </summary>
    public string? Note { get; init; }

    public required EvidenceRef Evidence { get; init; }

    /// <summary>Placeholder used when a send's payload could not be resolved to one packet.</summary>
    public const string UnresolvedPacket = "unresolved";
}

/// <summary>A field an implementation writes on the way through. Recorded verbatim, not summarised.</summary>
public sealed record FeatureMutation
{
    public required string Target { get; init; }

    public required string Expression { get; init; }

    public required EvidenceRef Evidence { get; init; }
}

public sealed record FeatureSpec
{
    /// <summary>Dotted id, e.g. <c>room.move_floor_item_in_room</c>.</summary>
    public required string Id { get; init; }

    public required string Domain { get; init; }

    public required string Title { get; init; }

    /// <summary>Packets that enter this feature.</summary>
    public IReadOnlyList<string> TriggerPackets { get; init; } = [];

    public IReadOnlyList<FeatureFlowStep> Flow { get; init; } = [];

    public IReadOnlyList<FeatureCheck> Checks { get; init; } = [];

    public IReadOnlyList<FeatureMutation> Mutations { get; init; } = [];

    public IReadOnlyList<FeatureOutgoing> Outgoing { get; init; } = [];

    /// <summary>
    /// <c>strict</c> once a capture pins the emission order down, <c>unknown</c> until then. The
    /// order a single emulator happens to use is not evidence of the order Habbo uses.
    /// </summary>
    public string OutgoingOrdering { get; init; } = "unknown";

    public bool ReachesPersistence { get; init; }

    /// <summary>True when Vortex implements this. Says nothing about official behaviour.</summary>
    public bool ObservedInVortex { get; init; }

    /// <summary>Reference emulators observed implementing the same trigger.</summary>
    public IReadOnlyList<string> ObservedInReferences { get; init; } = [];

    public Confidence OfficialBehaviourConfidence { get; init; } = Confidence.Unknown;

    public IReadOnlyList<EvidenceRef> Evidence { get; init; } = [];

    public IReadOnlyList<string> ScenarioIds { get; init; } = [];

    public IReadOnlyList<string> ConflictIds { get; init; } = [];

    public IReadOnlyList<string> UnknownIds { get; init; } = [];
}
