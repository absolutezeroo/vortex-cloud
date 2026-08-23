using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.NewNavigator;

public record NavigatorSetSearchCodeViewModeMessage : IMessageEvent
{
    public string? CategoryName { get; init; }
    public NavigatorViewModeType ViewMode { get; init; }
}
