namespace Vortex.Primitives.Rooms.Enums.Games;

/// <summary>The four Habbo game teams (plus "none"). The numeric values match the wired setup team
/// selector ids (team_1..team_4 radios) so a raw wired int param casts straight to this enum. The
/// in-room team aura effect id is <c>32 + (int)color</c> (Red=33, Green=34, Blue=35, Yellow=36), the
/// standard Habbo team-effect mapping.</summary>
public enum GameTeamColor
{
    None = 0,
    Red = 1,
    Green = 2,
    Blue = 3,
    Yellow = 4,
}
