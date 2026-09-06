using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// One test on a signal's facts. <c>Op</c> rather than <c>Operator</c>, which is a keyword in most
/// of the languages this JSON passes through.
/// </summary>
public sealed record RewardTrackStepFilterBody(string FactKey, int Op, string Value);
