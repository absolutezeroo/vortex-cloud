using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Vortex.Primitives.Rooms.Object.Avatars;

namespace Vortex.Rooms.Grains.Systems;

public sealed class RoomPathingSystem(RoomGrain roomGrain)
{
    private static readonly int CARDINAL_COST = 10;
    private static readonly int DIAGONAL_COST = 14;

    private static readonly (int dx, int dy, int cost)[] DIRECTIONS =
    {
        (0, -1, 10), // N
        (1, -1, 14), // NE
        (1, 0, 10), // E
        (1, 1, 14), // SE
        (0, 1, 10), // S
        (-1, 1, 14), // SW
        (-1, 0, 10), // W
        (-1, -1, 14), // NW
    };

    private readonly RoomGrain _roomGrain = roomGrain;

    public IReadOnlyList<(int X, int Y)> FindPath(
        IRoomAvatar avatar,
        (int X, int Y) start,
        (int X, int Y) goal
    )
    {
        return FindPath(
            start,
            goal,
            tileIdx => _roomGrain.MapModule.CanAvatarWalk(avatar, tileIdx),
            (currentTileId, nextTileId, isGoal) =>
                _roomGrain.MapModule.CanAvatarWalkBetween(avatar, currentTileId, nextTileId, isGoal)
        );
    }

    public IReadOnlyList<(int X, int Y)> FindPath(
        (int X, int Y) start,
        (int X, int Y) goal,
        Func<int, bool> canOccupyTile,
        Func<int, int, bool, bool> canMoveBetween
    )
    {
        try
        {
            (int startX, int startY) = start;
            (int goalX, int goalY) = goal;
            int currentTileId = _roomGrain.MapModule.ToIdx(start.X, start.Y);
            int goalTileId = _roomGrain.MapModule.ToIdx(goal.X, goal.Y);

            if (
                currentTileId == goalTileId
                || !canOccupyTile(currentTileId)
                || !canOccupyTile(goalTileId)
            )
            {
                return [];
            }

            PriorityQueue<Node, int> open = new();
            Dictionary<(int, int), Node> allNodes = new(256);

            Node GetOrCreateNode(int x, int y)
            {
                (int x, int y) key = (x, y);

                if (allNodes.TryGetValue(key, out Node? n))
                {
                    return n;
                }

                n = new Node { X = x, Y = y };
                allNodes[key] = n;

                return n;
            }

            Node startNode = GetOrCreateNode(startX, startY);

            startNode.G = 0;
            startNode.H = Heuristic(startX, startY, goalX, goalY);
            startNode.Parent = null;

            open.Enqueue(startNode, startNode.F);

            HashSet<(int, int)> closed = new();

            while (open.Count > 0 && allNodes.Count <= _roomGrain._roomConfig.MaxPathNodes)
            {
                try
                {
                    Node current = open.Dequeue();
                    (int X, int Y) cKey = (current.X, current.Y);
                    int cTileId = _roomGrain.MapModule.ToIdx(current.X, current.Y);

                    if (!closed.Add(cKey))
                    {
                        continue;
                    }

                    if (current.X == goalX && current.Y == goalY)
                    {
                        return ReconstructPath(current);
                    }

                    for (int i = 0; i < DIRECTIONS.Length; i++)
                    {
                        try
                        {
                            (int dx, int dy, int moveCost) = DIRECTIONS[i];
                            int nx = current.X + dx;
                            int ny = current.Y + dy;

                            if (
                                nx < 0
                                || ny < 0
                                || nx >= _roomGrain.MapModule.Width
                                || ny >= _roomGrain.MapModule.Height
                            )
                            {
                                continue;
                            }

                            if (closed.Contains((nx, ny)))
                            {
                                continue;
                            }

                            int nTileId = _roomGrain.MapModule.ToIdx(nx, ny);

                            if (!canMoveBetween(cTileId, nTileId, nx == goalX && ny == goalY))
                            {
                                continue;
                            }

                            int tentativeG = current.G + moveCost;
                            Node neighbor = GetOrCreateNode(nx, ny);

                            if (neighbor.Parent == null && !(nx == startX && ny == startY))
                            {
                                neighbor.Parent = current;
                                neighbor.G = tentativeG;
                                neighbor.H = Heuristic(nx, ny, goalX, goalY);
                                open.Enqueue(neighbor, neighbor.F);
                            }
                            else if (tentativeG < neighbor.G)
                            {
                                neighbor.Parent = current;
                                neighbor.G = tentativeG;
                                open.Enqueue(neighbor, neighbor.F);
                            }
                        }
                        catch (Exception ex)
                        {
                            _roomGrain._logger.LogWarning(
                                ex,
                                "Failed to evaluate pathfinding neighbor from ({X},{Y}) direction {DirectionIndex} in room {RoomId}.",
                                current.X,
                                current.Y,
                                i,
                                _roomGrain.RoomId
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    _roomGrain._logger.LogWarning(
                        ex,
                        "Failed to expand pathfinding node while searching from ({StartX},{StartY}) to ({GoalX},{GoalY}) in room {RoomId}.",
                        start.X,
                        start.Y,
                        goal.X,
                        goal.Y,
                        _roomGrain.RoomId
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to find path from ({StartX},{StartY}) to ({GoalX},{GoalY}) in room {RoomId}.",
                start.X,
                start.Y,
                goal.X,
                goal.Y,
                _roomGrain.RoomId
            );
        }

        return [];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Heuristic(int x, int y, int goalX, int goalY)
    {
        int dx = Math.Abs(x - goalX);
        int dy = Math.Abs(y - goalY);

        return dx < dy
            ? DIAGONAL_COST * dx + CARDINAL_COST * (dy - dx)
            : DIAGONAL_COST * dy + CARDINAL_COST * (dx - dy);
    }

    private static List<(int X, int Y)> ReconstructPath(Node goalNode)
    {
        List<(int, int)> list = new();
        Node? current = goalNode;

        while (current != null)
        {
            list.Add((current.X, current.Y));
            current = current.Parent!;
        }

        list.Reverse();

        return list;
    }

    internal sealed class Node
    {
        public int G; // Cost from start
        public int H; // Heuristic cost to goal
        public Node? Parent;
        public int X;
        public int Y;
        public int F => G + H;
    }
}
