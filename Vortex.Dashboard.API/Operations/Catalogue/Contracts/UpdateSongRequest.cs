using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Catalogue.Contracts;

public sealed record UpdateSongRequest(
    int SongId,
    string Name,
    string Creator,
    int LengthSeconds,
    string OfficialSongId,
    string Data,
    string Reason
) : IReasonedRequest;
