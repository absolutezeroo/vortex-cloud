using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

public sealed record UpdateNftAvatarRequest(
    int AvatarId,
    string AvatarCode,
    string Name,
    string Figure,
    string Gender,
    string ContractKey,
    int EditionSize,
    bool Enabled,
    int SortOrder,
    string Reason
) : IReasonedRequest;
