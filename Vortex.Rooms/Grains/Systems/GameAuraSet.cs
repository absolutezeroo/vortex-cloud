namespace Vortex.Rooms.Grains.Systems;

/// <summary>
/// The two team-aura effect families the client ships, as the base each <c>GameTeamColor</c> is
/// added to. Both are wire-correct and deliberately distinct — they are different effect sets in the
/// client's effect map, not a duplicated constant:
/// <list type="bullet">
/// <item><see cref="Wired"/> — the wired / Battle Banzai auras ("Team red".."Team yellow",
/// effects 33-36). Applied by the wired join-team action and any game whose teams are picked
/// through wired or Banzai-style gates.</item>
/// <item><see cref="Freeze"/> — the Freeze arena auras (ESred..ESyellow, effects 40-43), worn by
/// players who joined through a Freeze gate.</item>
/// </list>
/// </summary>
public enum GameAuraSet
{
    Wired = 32,
    Freeze = 39,
}
