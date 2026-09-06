using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record DeleteSongRequest(int SongId, string Reason) : IReasonedRequest;
