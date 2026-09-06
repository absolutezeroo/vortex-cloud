using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record UpdateMysteryBoxPrizeRequest(
    int PrizeId,
    string Pool,
    string Color,
    string ProductType,
    int FurnitureDefinitionId,
    string ExtraParam,
    int Weight,
    bool Enabled,
    string Reason
) : IReasonedRequest;
