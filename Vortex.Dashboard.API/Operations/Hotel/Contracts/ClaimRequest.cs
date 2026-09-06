using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record ClaimRequest(
    int PlayerId,
    string ProductCode,
    string SetId,
    string DefaultCollectionName,
    string Collection,
    int ClaimLimit,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    string Reason
) : IReasonedRequest;
