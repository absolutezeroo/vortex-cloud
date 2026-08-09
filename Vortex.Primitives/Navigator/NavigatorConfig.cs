namespace Vortex.Primitives.Navigator;

/// <summary>
/// Config keys and defaults for navigator search, served live from <c>IServerConfigGrain</c>.
/// The default is the fallback when a key has no admin override stored in the DB.
/// </summary>
/// <remarks>
/// Every navigator query used to be unbounded: a search returned one <c>RoomInfoSnapshot</c> per
/// matching row in <c>rooms</c>, which is fine on a dev hotel and a full table scan serialized to a
/// client on a real one. The caps live here rather than as constants in the provider so an operator
/// can tune them without a redeploy.
/// </remarks>
public static class NavigatorConfig
{
    /// <summary>Rooms returned by a single navigator search block.</summary>
    public const string SearchResultLimitKey = "navigator.search_result_limit";
    public const int SearchResultLimitDefault = 50;

    /// <summary>Rooms returned per category block on the "categories" view, which renders many
    /// blocks at once and so needs a tighter cap than a single search.</summary>
    public const string CategoryResultLimitKey = "navigator.category_result_limit";
    public const int CategoryResultLimitDefault = 20;

    /// <summary>Distinct rooms kept in the "recently visited" / "most visited" lists.</summary>
    public const string HistoryLimitKey = "navigator.history_limit";
    public const int HistoryLimitDefault = 25;
}
