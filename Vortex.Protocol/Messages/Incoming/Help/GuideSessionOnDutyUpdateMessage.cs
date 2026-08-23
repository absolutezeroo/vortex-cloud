using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Help;

/// <summary>
/// A guide going on or off duty, with the three queues they are willing to cover.
/// </summary>
/// <remarks>
/// The client sends all four every time any one of them changes, and the checkbox names give the
/// mapping away: <c>guidetool.handle.tour_requests</c> is the guide queue,
/// <c>handle.help_requests</c> the helper queue and <c>handle.chat_reviews</c> the guardian queue.
/// </remarks>
public record GuideSessionOnDutyUpdateMessage : IMessageEvent
{
    public required bool OnDuty { get; init; }
    public required bool HandlesGuideRequests { get; init; }
    public required bool HandlesHelperRequests { get; init; }
    public required bool HandlesGuardianRequests { get; init; }
}
