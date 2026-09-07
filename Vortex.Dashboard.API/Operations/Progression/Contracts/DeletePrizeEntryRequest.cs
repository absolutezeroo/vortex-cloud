using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Progression.Contracts;

public sealed record DeletePrizeEntryRequest(int EntryId, string Reason) : IReasonedRequest;
