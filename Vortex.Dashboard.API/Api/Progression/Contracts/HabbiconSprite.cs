namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// Where an id sits on its sheet.
/// </summary>
/// <remarks>
/// Absent rather than zeroed when the pack does not carry the id: (0,0) is a real frame, so a
/// missing entry defaulting there would draw the first Habbicon under every id the pack forgot.
/// </remarks>
public sealed record HabbiconSprite(int X, int Y);
