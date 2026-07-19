using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Preferences;

public record SetNewNavigatorWindowPreferencesMessage : IMessageEvent
{
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public bool OpenSavedSearches { get; init; }
    public NavigatorViewModeType ResultsMode { get; init; }
}
