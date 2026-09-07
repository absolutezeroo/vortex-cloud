using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>How many items the account owns, and the most recently touched few.</summary>
public sealed record ProfileInventory(int Total, IReadOnlyList<ProfileItemRow> Latest);
