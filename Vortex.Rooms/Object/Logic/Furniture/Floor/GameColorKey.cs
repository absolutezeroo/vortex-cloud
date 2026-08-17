using Vortex.Primitives.Rooms.Enums.Games;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

/// <summary>
/// Resolves a game furni's <see cref="GameTeamColor"/> from the suffix of its bound logic key
/// (<c>freeze_gate_red</c>, <c>battlebanzai_gate_yellow</c>) or its classname
/// (<c>bb_score_r</c>, <c>fball_goal_g</c>). This is what lets one logic class claim all four
/// colour keys with four <c>[RoomObjectLogic]</c> attributes instead of a shell subclass per colour
/// — the instance reads <c>ctx.Definition.LogicName</c> (or <c>Name</c>) at construction.
/// <para>
/// An unrecognised suffix resolves to <see cref="GameTeamColor.None"/>, never a throw: this runs in
/// logic constructors, where a throw would fail the item attach and take the furni out of the room.
/// A None-coloured gate joins nobody and a None-coloured board displays nothing, which is the
/// graceful shape of "misbound definition".
/// </para>
/// </summary>
public static class GameColorKey
{
    public static GameTeamColor FromKeySuffix(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return GameTeamColor.None;
        }

        if (key.EndsWith("_red") || key.EndsWith("_r"))
        {
            return GameTeamColor.Red;
        }

        if (key.EndsWith("_green") || key.EndsWith("_g"))
        {
            return GameTeamColor.Green;
        }

        if (key.EndsWith("_blue") || key.EndsWith("_b"))
        {
            return GameTeamColor.Blue;
        }

        if (key.EndsWith("_yellow") || key.EndsWith("_y"))
        {
            return GameTeamColor.Yellow;
        }

        return GameTeamColor.None;
    }
}
