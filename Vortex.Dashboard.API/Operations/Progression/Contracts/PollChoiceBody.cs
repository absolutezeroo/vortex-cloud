using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// One selectable answer. <c>Value</c> is what the client sends back and what the results are keyed
/// on; <c>ChoiceType</c> is the NPS branch key (0 = picking it leads to no follow-up).
/// </summary>
public sealed record PollChoiceBody(string Value, string ChoiceText, int ChoiceType, int SortOrder);
