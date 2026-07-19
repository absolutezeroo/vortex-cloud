using System.Collections.Generic;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.NewNavigator;

public sealed record NavigatorCollapsedCategoriesMessage : IComposer
{
    public required List<string> CollapsedCategoryIds { get; init; }
}
