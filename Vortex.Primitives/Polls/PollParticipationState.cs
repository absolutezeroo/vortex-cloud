namespace Vortex.Primitives.Polls;

/// <summary>Where a single player stands on a single poll.</summary>
public enum PollParticipationState
{
    /// <summary>The offer dialog was pushed to the player; they have neither accepted nor declined.</summary>
    Offered = 0,

    /// <summary>The player accepted the offer and received the questions.</summary>
    Started = 1,

    /// <summary>Every root question has an answer. The poll is never offered again.</summary>
    Completed = 2,

    /// <summary>The player declined the offer. The poll is never offered again.</summary>
    Rejected = 3,
}
