namespace Vortex.Rooms.Grains;

public sealed class WiredErrorLogCounter
{
    public required string ErrorName { get; init; }
    public required string Category { get; init; }
    public int ThrowCount { get; set; }
    public long LastOccurrenceMs { get; set; }
}
