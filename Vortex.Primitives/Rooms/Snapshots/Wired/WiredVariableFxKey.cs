namespace Vortex.Primitives.Rooms.Snapshots.Wired;

/// <summary>
/// The composite key a Variable FX status is addressed by.
/// </summary>
/// <remarks>
/// Written in one place because the client parses it by hand — <c>indexOf('|')</c> for the config
/// id, the last <c>|</c> for the entity id, the one before it for the user/furni letter — so a key
/// built any other way is read as a different display, silently, and the bar stops updating rather
/// than erroring.
/// </remarks>
public static class WiredVariableFxKey
{
    public static string Build(int configId, string variableId, bool isUserEntity, int entityId) =>
        $"{configId}|{variableId}|{(isUserEntity ? "u" : "f")}|{entityId}";
}
