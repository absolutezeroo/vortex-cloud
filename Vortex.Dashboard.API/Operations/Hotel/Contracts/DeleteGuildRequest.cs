using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

/// <summary>Disband a guild: its members, its pending requests and its hold on its base room.</summary>
public sealed record DeleteGuildRequest(int GuildId, string Reason) : IReasonedRequest;
