using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;

/// <summary>
/// The shared half of the six Variable FX add-ons: a display drawn from a variable someone else
/// owns.
/// </summary>
/// <remarks>
/// Exactly the shape the client uses — one base carrying the whole form, and six subclasses that
/// differ only by their code and their <see cref="CategoryId"/>. Health points is category 0 and
/// the rest follow in the order the boxes were released.
/// <para>
/// The form is twenty-one int params. Not all of them have a home in the config message: the client
/// reads a style and a visibility out of the form that its own config record has no field for, so
/// they are kept out of the wire rather than written into a field that happens to be free.
/// </para>
/// <para>
/// Two of the message's three nameless fields are pinned by identity rather than by guesswork: the
/// form and the config record carry the same obfuscated identifiers, <c>_SafeStr_5289</c> at form
/// param 3 and wire field 4, <c>_SafeStr_5624</c> at form param 8 and wire field 10. The third,
/// <c>_SafeStr_5449</c> at wire field 8, appears nowhere in the form and is left at zero — there is
/// nothing to derive it from, and inventing a source for it would be worse than sending nothing.
/// </para>
/// </remarks>
public abstract class FurnitureWiredVariableFxAddonLogic(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredAddonLogic(grainFactory, stuffDataFactory, ctx), IWiredVariableFxSource
{
    /// <summary>Which of the six displays this is, as the client's own renderer switches on it.</summary>
    protected abstract int CategoryId { get; }

    /// <summary>The form's source-type selector: the display hangs off users rather than furni.</summary>
    private const int SourceType = 0;

    private const int ShowMode = 2;

    private const int Field4 = 3;

    private const int ShowOnMouseHover = 4;

    private const int ShowDuration = 5;

    private const int ColorId = 7;

    private const int Field10 = 8;

    private const int RendererId = 9;

    private const int DefaultMinValue = 11;

    private const int DefaultMaxValue = 13;

    /// <summary>The client's own picker id for "this hangs off users".</summary>
    private const int UserSource = 1;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(0, int.MaxValue, 0), // 0 source type
            new WiredRangeParamRule(0, int.MaxValue, 0), // 1 visibility
            new WiredRangeParamRule(0, int.MaxValue, 0), // 2 show mode
            new WiredRangeParamRule(0, int.MaxValue, 0), // 3 _SafeStr_5289
            new WiredRangeParamRule(0, 1, 0), // 4 show on mouse hover
            new WiredRangeParamRule(0, int.MaxValue, 0), // 5 show duration
            new WiredRangeParamRule(0, int.MaxValue, 0), // 6 style id
            new WiredRangeParamRule(0, int.MaxValue, 0), // 7 colour id
            new WiredRangeParamRule(0, int.MaxValue, 0), // 8 _SafeStr_5624
            new WiredRangeParamRule(0, int.MaxValue, 0), // 9 renderer id
            new WiredRangeParamRule(0, int.MaxValue, 0), // 10 min value source
            new WiredRangeParamRule(int.MinValue, int.MaxValue, 0), // 11 default minimum
            new WiredRangeParamRule(0, int.MaxValue, 0), // 12 max value source
            new WiredRangeParamRule(int.MinValue, int.MaxValue, 0), // 13 default maximum
            new WiredRangeParamRule(0, 1, 0), // 14 override minimum enabled
            new WiredRangeParamRule(0, 1, 0), // 15 override maximum enabled
            new WiredRangeParamRule(0, int.MaxValue, 0), // 16 override minimum target
            new WiredRangeParamRule(0, int.MaxValue, 0), // 17 override maximum target
            new WiredRangeParamRule(0, int.MaxValue, 0), // 18 audience source
            new WiredRangeParamRule(0, int.MaxValue, 0), // 19 audience variable value
            new WiredRangeParamRule(0, int.MaxValue, 0), // 20 segments
        ];

    /// <summary>
    /// The display's identity, and the add-on's own object id is the only stable candidate.
    /// </summary>
    /// <remarks>
    /// It has to survive for as long as the display does, because every status update addresses the
    /// display through it and the client keeps its config table keyed on it. The add-on is the one
    /// thing that exists exactly as long as the display it declares.
    /// </remarks>
    public int ConfigId => _ctx.ObjectId.Value;

    public WiredVariableFxConfigSnapshot? CreateFxConfig(IWiredVariable parent)
    {
        if (_wiredData.IntParams.Count <= DefaultMaxValue)
        {
            return null;
        }

        return new WiredVariableFxConfigSnapshot
        {
            ConfigId = ConfigId,
            IsUserFx = Param(SourceType) == UserSource,
            ShowMode = Param(ShowMode),
            Field4 = Param(Field4),
            ShowOnMouseHover = Param(ShowOnMouseHover) != 0,
            ShowDuration = Param(ShowDuration),
            CategoryId = CategoryId,
            ColorId = Param(ColorId),
            Field10 = Param(Field10),
            RendererId = Param(RendererId),
            DefaultMinValue = Param(DefaultMinValue),
            DefaultMaxValue = Param(DefaultMaxValue),
        };
    }

    private int Param(int index) => _wiredData.GetIntParam<int>(index);
}
