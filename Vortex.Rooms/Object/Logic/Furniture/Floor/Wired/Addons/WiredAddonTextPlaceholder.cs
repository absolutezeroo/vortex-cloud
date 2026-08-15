using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;

/// <summary>
/// What the three "text add-on" boxes share: a named placeholder that any wired text in the same
/// pile can carry, written <c>$(name)</c>.
/// </summary>
/// <remarks>
/// Int param [0] is the single/multiple choice, and the string param holds the name — plus, when
/// set to multiple, the delimiter after a tab. Resolving the values is the only thing the three
/// boxes differ by.
/// </remarks>
public abstract class WiredAddonTextPlaceholder(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredAddonLogic(grainFactory, stuffDataFactory, ctx)
{
    public override List<IWiredParamRule> GetIntParamRules() => [new WiredBoolParamRule(false)];

    /// <summary>Whether the box was set to show every match joined by its delimiter, rather than
    /// just the first.</summary>
    protected bool ShowsMultiple =>
        _wiredData.IntParams.Count > 0 && _wiredData.GetIntParam<bool>(0);

    public override async Task<string> ApplyToTextAsync(
        string text,
        IWiredExecutionContext ctx,
        CancellationToken ct
    )
    {
        (string name, string delimiter) = WiredPlaceholder.ParseConfiguration(
            _wiredData.StringParam
        );

        string token = WiredPlaceholder.BuildToken(name);

        // Resolving costs a selection walk, so an unnamed box, or a text that does not mention this
        // placeholder, stops here.
        if (token.Length == 0 || !text.Contains(token, System.StringComparison.Ordinal))
        {
            return text;
        }

        IReadOnlyList<string> values = await ResolveValuesAsync(ctx, ct);

        return WiredPlaceholder.Substitute(text, token, values, ShowsMultiple, delimiter);
    }

    /// <summary>What this placeholder stands for, in the order the pile resolved its targets.</summary>
    protected abstract Task<IReadOnlyList<string>> ResolveValuesAsync(
        IWiredExecutionContext ctx,
        CancellationToken ct
    );
}
