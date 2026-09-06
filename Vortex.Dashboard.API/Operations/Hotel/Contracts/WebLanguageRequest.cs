using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record WebLanguageRequest(
    int LanguageId,
    string Code,
    string Label,
    bool IsDefault,
    bool Enabled,
    int SortOrder,
    string Reason
) : IReasonedRequest;
