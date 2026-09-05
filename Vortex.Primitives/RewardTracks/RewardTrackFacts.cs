namespace Vortex.Primitives.RewardTracks;

/// <summary>
/// The named facts a progress signal can carry about what just happened.
/// </summary>
/// <remarks>
/// <para>
/// A signal used to be one opaque string, which is why a task could say "a sofa" but never "the
/// sofa you just placed". Facts are what make a sequence composable: a step filters on them, and a
/// later step can point back at what an earlier one matched.
/// </para>
/// <para>
/// The vocabulary is closed on purpose. An operator picking a fact an action never emits would be
/// writing a filter that silently never matches, so the dashboard offers only the facts the chosen
/// action actually produces — see <c>RewardTrackActionFacts</c>, which is the same list read the
/// other way round.
/// </para>
/// </remarks>
public static class RewardTrackFacts
{
    /// <summary>
    /// What the signal was mainly about. Every signal that carries anything carries this, and it is
    /// what a task's <c>Parameter</c> is matched against — which is why the parameter keeps working
    /// unchanged.
    /// </summary>
    public const string Target = "target";

    /// <summary>The room object id of a piece of furniture: the identity that survives a move.</summary>
    public const string Item = "item";

    /// <summary>A furniture definition id — the kind of thing, not the thing.</summary>
    public const string Definition = "def";

    /// <summary>
    /// <c>floor</c> or <c>wall</c>. The coarse type an operator means by "un mobilier de type sol",
    /// which no definition id can express on its own.
    /// </summary>
    public const string Placement = "kind";

    /// <summary>A room id.</summary>
    public const string Room = "room";

    /// <summary>
    /// Who owns the room. Reserved, and NOT emitted by anything yet: it would make "join their
    /// flat" expressible without a follow event, but <c>PlayerEnteredRoomEvent</c> does not carry
    /// the owner and reading it per entry would be a grain call on an arrival path that has been
    /// slow before. Left declared so the shape is obvious to whoever adds it, and left out of
    /// <see cref="RewardTrackActionFacts"/> so nobody can pick it in the editor meanwhile.
    /// </summary>
    public const string RoomOwner = "room_owner";

    /// <summary>Another player's id: the one respected, befriended, traded with.</summary>
    public const string Player = "player";

    /// <summary>A catalogue offer id.</summary>
    public const string Offer = "offer";

    /// <summary>A Habbicon id.</summary>
    public const string Habbicon = "habbicon";

    /// <summary>A Habbicon collection code.</summary>
    public const string Collection = "collection";

    /// <summary>A pet id.</summary>
    public const string Pet = "pet";

    /// <summary>A badge code.</summary>
    public const string Badge = "badge";

    /// <summary>Value of <see cref="Placement"/> for a floor item.</summary>
    public const string PlacementFloor = "floor";

    /// <summary>Value of <see cref="Placement"/> for a wall item.</summary>
    public const string PlacementWall = "wall";
}
