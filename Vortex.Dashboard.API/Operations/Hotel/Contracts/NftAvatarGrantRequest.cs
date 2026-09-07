using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

/// <summary>
/// <see cref="Note"/> is the provenance: what the copy was given for. It is carried separately from
/// the audit's own reason because it is the line read back months later, from the avatar's page
/// rather than from the log.
/// </summary>
public sealed record NftAvatarGrantRequest(int AvatarId, int PlayerId, string Note, string Reason)
    : IReasonedRequest;
