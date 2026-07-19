namespace Vortex.Primitives.Moderation;

/// <summary>Matches Daybreak's three-outcome close flow (reviewed for behavior only, not code):
/// each reason sends the reporter a different <c>MyCfhReportStatus</c> outcome.</summary>
public enum CfhTicketCloseReason
{
    Useless = 1,
    Sanctioned = 2,
    Resolved = 3,
}
