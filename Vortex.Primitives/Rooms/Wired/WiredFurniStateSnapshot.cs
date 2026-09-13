namespace Vortex.Primitives.Rooms.Wired;

/// <summary>
/// Where a furni stood and how it looked when a wired box was last saved.
/// </summary>
/// <remarks>
/// Taken at configuration time, not at run time. That is what the client's <c>hasStateSnapshot</c>
/// flag means and what the box built on it does: "match to snapshot" returns the furni to the
/// conditions they were in when the builder set it up, which is why arranging the room and then
/// saving the box is the whole interaction.
/// <para>
/// Durable player configuration, so it rides along in the wired data and through the furni's
/// <c>extra_data</c> "wired" section. A snapshot rebuilt on room load would be a snapshot of whatever
/// the last player left behind.
/// </para>
/// </remarks>
public sealed class WiredFurniStateSnapshot
{
    public int FurniId { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    /// <summary>Altitude in the integer form the wire and <c>Altitude</c> both use.</summary>
    public int Z { get; set; }

    public int Rotation { get; set; }

    public int State { get; set; }
}
