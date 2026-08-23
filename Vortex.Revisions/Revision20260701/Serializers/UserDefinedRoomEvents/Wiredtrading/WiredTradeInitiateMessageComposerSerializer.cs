using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// The requirement reads itself first and its length varies with its type, so the three
/// presentation fields can only follow it — the client's parser has the same ordering constraint
/// and says so.
/// </summary>
internal class WiredTradeInitiateMessageComposerSerializer(int header)
    : AbstractSerializer<WiredTradeInitiateMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredTradeInitiateMessageComposer message
    )
    {
        packet
            .WriteInteger(message.RequirementType)
            .WriteString(message.YouGetText)
            // The client concatenates this straight into an asset name
            // ("wired_chests_images_<layoutType>_payments"), so an empty string is not a neutral
            // default -- it asks the resource manager for an image that does not exist and the
            // trade screen opens with a blank panel. "generic" is the client's own fallback: it is
            // LAYOUT_TYPES[0] in PaymentContract, which is what an out-of-range index clamps to.
            .WriteString(
                string.IsNullOrEmpty(message.LayoutType) ? DefaultLayoutType : message.LayoutType
            );

        // Only a custom contract carries rules, and the client reads them off the same test — send
        // one under any other type and everything after it is read at the wrong offset.
        if (message.RequirementType == CustomRequirementType && message.Contract is not null)
        {
            TradeContract contract = message.Contract;

            packet.WriteDefinition(contract.YouGiveRules, contract.YouGetRule);

            packet.WriteInteger(contract.Mode);

            // The mode decides which single trailing int follows it, or none at all.
            if (contract.Mode == 1)
            {
                packet.WriteInteger(contract.Multiplier);
            }
            else if (contract.Mode == 2)
            {
                packet.WriteInteger(contract.AutoMultiplierMax);
            }
        }

        packet
            .WriteBoolean(message.ShowRequirementsImmediate)
            .WriteBoolean(message.OverridePreviousTrade)
            .WriteInteger(message.TimeoutSeconds);
    }

    /// <summary>The one requirement type whose payload is followed by a rules block.</summary>
    private const int CustomRequirementType = 4;

    /// <summary>The layout a screen wears when the contract behind it names none.</summary>
    private const string DefaultLayoutType = "generic";
}
