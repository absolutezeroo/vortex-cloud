using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record GrantMysteryBoxRequest(
    int PlayerId,
    int FurnitureDefinitionId,
    string Color,
    string Reason
) : IReasonedRequest;
