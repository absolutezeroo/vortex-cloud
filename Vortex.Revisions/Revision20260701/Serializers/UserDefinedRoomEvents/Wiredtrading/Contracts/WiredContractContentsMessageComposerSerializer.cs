using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading.Contracts;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredtrading.Contracts;

/// <summary>
/// One contract, read back into its editor.
/// </summary>
/// <remarks>
/// The type is a short where the id is an int, and it decides the tail — the same three branches
/// the save arrives under. Sending the wrong one does not throw at the other end; it eats the next
/// message.
/// </remarks>
internal class WiredContractContentsMessageComposerSerializer(int header)
    : AbstractSerializer<WiredContractContentsMessageComposer>(header)
{
    private const int PaymentContract = 0;

    private const int RewardContract = 2;

    protected override void Serialize(
        IServerPacket packet,
        WiredContractContentsMessageComposer message
    )
    {
        WiredContractSnapshot contract = message.Contract;

        packet
            .WriteInteger(contract.ContractId)
            .WriteShort((short)contract.ContractType)
            .WriteDefinition(contract.YouGiveRules, contract.YouGetRule);

        if (contract.ContractType == PaymentContract)
        {
            packet
                .WriteShort((short)contract.PaymentMode)
                .WriteString(contract.ReceiveText)
                .WriteString(contract.LayoutType);
        }
        else if (contract.ContractType == RewardContract)
        {
            packet
                .WriteShort((short)contract.RewardCategory)
                .WriteBoolean(contract.ShowDialog)
                .WriteString(contract.RewardText);
        }
    }
}
