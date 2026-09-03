using System;
using System.Collections.Generic;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Rooms.Games.Arena;

/// <summary>Where one of a game's components sits, as the partitioner needs to see it.</summary>
public readonly record struct ArenaPlacement(RoomObjectId ObjectId, int X, int Y);

/// <summary>
/// One installation's footprint: the tiles its furniture occupies. What "how far is this timer from
/// that board" is measured against.
/// </summary>
public sealed record ArenaFootprint
{
    public required int Instance { get; init; }

    public required IReadOnlyList<(int X, int Y)> Tiles { get; init; }

    /// <summary>Chebyshev distance from a tile to the nearest tile of this footprint;
    /// <see cref="int.MaxValue"/> for an empty footprint. Chebyshev because a room's furniture is
    /// laid out on a grid a player walks diagonally.</summary>
    public int DistanceTo(int x, int y)
    {
        int best = int.MaxValue;

        foreach ((int tx, int ty) in Tiles)
        {
            int distance = Math.Max(Math.Abs(tx - x), Math.Abs(ty - y));

            if (distance < best)
            {
                best = distance;
            }
        }

        return best;
    }
}

/// <summary>
/// Splits one game's furniture in a room into independent installations, generically and with no
/// per-game code: two components belong to the same arena when they are within
/// <c>GameProfile.ArenaSeparation</c> tiles of each other, transitively. Two Banzai boards at
/// opposite ends of a hall are therefore two arenas; the gates beside one board belong to that board.
/// <para>
/// A separation of 0 or less means "one installation per room", which is what every Habbo game gets:
/// the client has no way to address a second board — one <c>bb_score_r</c> shows one red score, one
/// counter starts one game — so a second installation would be unplayable rather than independent.
/// That case allocates nothing and compares nothing; it is the constant-time
/// <see cref="Single"/> partition.
/// </para>
/// <para>Pure: a function of positions and a radius, so every rule in it is testable with no room.</para>
/// </summary>
public sealed class ArenaPartition
{
    /// <summary>Everything is instance 0. The partition a game gets when it does not separate.</summary>
    public static readonly ArenaPartition Single = new(null, 1);

    private readonly Dictionary<RoomObjectId, int>? _instanceByObject;

    private ArenaPartition(
        Dictionary<RoomObjectId, int>? instanceByObject,
        int instanceCount
    )
    {
        _instanceByObject = instanceByObject;
        InstanceCount = instanceCount;
        Footprints = [];
    }

    /// <summary>How many installations were found. Always at least 1, so an empty room still has an
    /// arena to address, refuse to start, and report a shortfall for.</summary>
    public int InstanceCount { get; private init; }

    public IReadOnlyList<ArenaFootprint> Footprints { get; private init; }

    public static ArenaPartition Build(IReadOnlyList<ArenaPlacement> placements, int separation)
    {
        if (separation <= 0 || placements.Count == 0)
        {
            return Single;
        }

        int[] parent = new int[placements.Count];

        for (int i = 0; i < parent.Length; i++)
        {
            parent[i] = i;
        }

        for (int i = 0; i < placements.Count; i++)
        {
            for (int j = i + 1; j < placements.Count; j++)
            {
                int dx = Math.Abs(placements[i].X - placements[j].X);
                int dy = Math.Abs(placements[i].Y - placements[j].Y);

                if (Math.Max(dx, dy) <= separation)
                {
                    Union(parent, i, j);
                }
            }
        }

        // Number the groups in the order their first member appears, so the instance a board gets is
        // a function of the room's contents and not of dictionary ordering: the same room always
        // partitions the same way, which is what makes an arena addressable across ticks.
        Dictionary<int, int> instanceByRoot = [];
        Dictionary<RoomObjectId, int> instanceByObject = [];
        List<List<(int X, int Y)>> tiles = [];

        for (int i = 0; i < placements.Count; i++)
        {
            int root = Find(parent, i);

            if (!instanceByRoot.TryGetValue(root, out int instance))
            {
                instance = instanceByRoot.Count;
                instanceByRoot[root] = instance;
                tiles.Add([]);
            }

            instanceByObject[placements[i].ObjectId] = instance;
            tiles[instance].Add((placements[i].X, placements[i].Y));
        }

        List<ArenaFootprint> footprints = [];

        for (int instance = 0; instance < tiles.Count; instance++)
        {
            footprints.Add(new ArenaFootprint { Instance = instance, Tiles = tiles[instance] });
        }

        return new ArenaPartition(instanceByObject, Math.Max(1, instanceByRoot.Count))
        {
            Footprints = footprints,
        };
    }

    /// <summary>Which installation that component belongs to. 0 for an unpartitioned game, and 0 for
    /// a component placed since the partition was taken — a brand new tile joins the first arena
    /// rather than vanishing from every one of them.</summary>
    public int InstanceOf(RoomObjectId objectId)
    {
        if (_instanceByObject is null)
        {
            return 0;
        }

        return _instanceByObject.TryGetValue(objectId, out int instance) ? instance : 0;
    }

    private static int Find(int[] parent, int node)
    {
        while (parent[node] != node)
        {
            parent[node] = parent[parent[node]];
            node = parent[node];
        }

        return node;
    }

    private static void Union(int[] parent, int a, int b)
    {
        int rootA = Find(parent, a);
        int rootB = Find(parent, b);

        if (rootA != rootB)
        {
            parent[Math.Max(rootA, rootB)] = Math.Min(rootA, rootB);
        }
    }
}
