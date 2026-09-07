using System;

namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>
/// The period the moderation report covers.
/// </summary>
/// <remarks>
/// Same two fields as <see cref="ChatlogWindow"/> and deliberately not shared with it: naming this
/// one after chatlogs would put "ChatlogWindow" inside the moderation response for no reason. If a
/// third read wants a bare window, that is the point to give the three of them one name.
/// </remarks>
public sealed record ModerationWindow(DateTime Since, DateTime Until);
