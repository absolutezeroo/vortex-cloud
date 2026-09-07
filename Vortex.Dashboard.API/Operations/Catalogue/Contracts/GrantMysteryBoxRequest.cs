using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Catalogue.Contracts;

public sealed record GrantMysteryBoxRequest(
    int PlayerId,
    int FurnitureDefinitionId,
    string Color,
    string Reason
) : IReasonedRequest;
