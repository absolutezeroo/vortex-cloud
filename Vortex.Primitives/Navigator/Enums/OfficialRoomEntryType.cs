namespace Vortex.Primitives.Navigator.Enums;

/// <summary>
/// Discriminator for an official-rooms entry. The values are the client's own constants, and they
/// decide what the client reads next off the wire, so they are not free to renumber.
/// </summary>
public enum OfficialRoomEntryType
{
    /// <summary>Entry points at a tag search; the wire carries a tag string.</summary>
    Tag = 1,

    /// <summary>Entry is a room; the wire carries a full guest-room data block.</summary>
    Room = 2,

    /// <summary>Entry is a folder node; the wire carries its open/closed flag.</summary>
    Folder = 4,
}
