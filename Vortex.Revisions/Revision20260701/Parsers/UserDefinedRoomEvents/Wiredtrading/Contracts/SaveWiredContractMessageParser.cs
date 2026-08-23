using System.Collections.Immutable;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading.Contracts;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Wiredtrading.Contracts;

/// <summary>
/// The contract editor's save.
/// </summary>
/// <remarks>
/// The type is a <em>short</em> where the id is an int, and it decides the tail: a payment contract
/// carries three more fields, a reward contract three different ones, a trade contract none. The
/// client writes exactly this and reads exactly this back, so the two have to agree field for
/// field.
/// </remarks>
internal class SaveWiredContractMessageParser : IParser
{
    private const int PaymentContract = 0;

    private const int RewardContract = 2;

    public IMessageEvent Parse(IClientPacket packet)
    {
        int contractId = packet.PopInt();
        int contractType = packet.PopShort();

        (ImmutableArray<TradeContractRule>? give, TradeContractRule? get) =
            TradeContractReader.ReadDefinition(packet);

        WiredContractSnapshot contract = new()
        {
            ContractId = contractId,
            ContractType = contractType,
            YouGiveRules = give,
            YouGetRule = get,
        };

        if (contractType == PaymentContract)
        {
            contract = contract with
            {
                PaymentMode = packet.PopShort(),
                ReceiveText = packet.PopString(),
                LayoutType = packet.PopString(),
            };
        }
        else if (contractType == RewardContract)
        {
            contract = contract with
            {
                RewardCategory = packet.PopShort(),
                ShowDialog = packet.PopBoolean(),
                RewardText = packet.PopString(),
            };
        }

        return new SaveWiredContractMessage { Contract = contract };
    }
}
