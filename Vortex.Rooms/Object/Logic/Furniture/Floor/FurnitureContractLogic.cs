using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

/// <summary>
/// A wired trading contract: payment, reward or trade.
/// </summary>
/// <remarks>
/// It behaves exactly like plain floor furni and inherits that whole; the point of naming it is
/// identity, not behaviour. The wired boxes that offer and cancel transactions have to find the
/// contract among the furni they were pointed at, and the only reliable way to ask what a furni is
/// is its logic — a classname is not a key in this database, which ships thousands of duplicates.
/// <para>
/// These three shipped bound to <c>furniture_basic</c>, which is this same behaviour under a name
/// shared with roughly 5 800 other definitions and therefore useless to match on.
/// <c>scripts/sql/wired_contract_logic_binding.sql</c> points them at these names instead.
/// </para>
/// </remarks>
[RoomObjectLogic("wf_contract_payment")]
[RoomObjectLogic("wf_contract_reward")]
[RoomObjectLogic("wf_contract_trade")]
public class FurnitureContractLogic(IStuffDataFactory stuffDataFactory, IRoomFloorItemContext ctx)
    : FurnitureFloorLogic(stuffDataFactory, ctx);
