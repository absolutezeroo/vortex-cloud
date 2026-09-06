using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Request bodies for the fishing content tables, each carrying a mandatory audited <c>Reason</c>.
/// </summary>
/// <remarks>
/// The units are the table's, not the page's: <c>CatchRate</c> and <c>HookHavocChance</c> are tenths
/// of a percent, the multipliers are thousandths, and the two calendars are bit masks. The page
/// renders them as percentages, factors and checkboxes; nothing between here and the database
/// reinterprets them.
/// </remarks>
public sealed record CreateFishingZoneRequest(
    string NameKey,
    string FurniClass,
    int RequiredLevel,
    int MinCatches,
    int MaxCatches,
    string Reason
) : IReasonedRequest;
