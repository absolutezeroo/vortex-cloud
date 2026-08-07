using Orleans;

namespace Vortex.Primitives.Help;

/// <summary>
/// What became of a help request, or of a guide's answer to one.
/// </summary>
/// <remarks>
/// One shape for both, because the two questions have the same three answers: it went to a guide,
/// it started a session, or it failed. The caller sends packets from this and never has to ask the
/// grain a second question to know who to send them to.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record GuideRequestOutcome
{
    /// <summary>The guide the request is now sitting in front of, or 0 when it is not.</summary>
    [Id(0)]
    public int OfferedGuideId { get; init; }

    /// <summary>The pair, once a guide has accepted. Null until then.</summary>
    [Id(1)]
    public GuideSessionSnapshot? Session { get; init; }

    /// <summary>
    /// Set when the request cannot go anywhere. The client subtracts one before switching on it, so
    /// 1 is its "rejected" case — nobody is on duty for this queue, or the guide said no and there
    /// was no one else to ask.
    /// </summary>
    [Id(2)]
    public int ErrorCode { get; init; }

    /// <summary>Who asked, so the caller can answer them without looking it up again.</summary>
    [Id(3)]
    public int RequesterId { get; init; }

    /// <summary>Carried on the outcome so the offer packet can be built without asking the grain
    /// what the request was about.</summary>
    [Id(4)]
    public int HelpRequestType { get; init; }

    [Id(5)]
    public string Description { get; init; } = string.Empty;

    public bool Failed => ErrorCode > 0;
}
