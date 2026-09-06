using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record NftAvatarRevokeRequest(int CopyId, string Reason) : IReasonedRequest;
