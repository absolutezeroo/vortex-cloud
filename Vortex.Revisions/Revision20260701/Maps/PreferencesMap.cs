using Vortex.Primitives.Messages.Outgoing.Preferences;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Parsers.Preferences;
using Vortex.Revisions.Revision20260701.Serializers.Preferences;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class PreferencesMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(
            MessageEvent.SetChatPreferencesMessageEvent,
            new SetChatPreferencesMessageParser()
        );
        builder.MapParser(
            MessageEvent.SetChatStylePreferenceEvent,
            new SetChatStylePreferenceMessageParser()
        );
        builder.MapParser(
            MessageEvent.SetIgnoreRoomInvitesMessageEvent,
            new SetIgnoreRoomInvitesMessageParser()
        );
        builder.MapParser(
            MessageEvent.SetNewNavigatorWindowPreferencesMessageEvent,
            new SetNewNavigatorWindowPreferencesMessageParser()
        );
        builder.MapParser(
            MessageEvent.SetRoomCameraPreferencesMessageEvent,
            new SetRoomCameraPreferencesMessageParser()
        );
        builder.MapParser(MessageEvent.SetSoundSettingsEvent, new SetSoundSettingsMessageParser());
        builder.MapParser(MessageEvent.SetUIFlagsMessageEvent, new SetUIFlagsMessageParser());

        // The personal word filter. Distinct from the ROOM filter in RoomSettingsMap: this one is
        // per player, and its three headers had placeholder values with nothing behind them.
        builder.MapParser(
            MessageEvent.GetCustomFilterMessageEvent,
            new GetCustomFilterMessageParser()
        );
        builder.MapParser(
            MessageEvent.AddToCustomFilterMessageEvent,
            new AddToCustomFilterMessageParser()
        );
        builder.MapParser(
            MessageEvent.RemoveFromCustomFilterMessageEvent,
            new RemoveFromCustomFilterMessageParser()
        );

        builder.MapSerializer(
            typeof(AccountPreferencesEventMessageComposer),
            new AccountPreferencesEventMessageComposerSerializer(
                MessageComposer.AccountPreferencesComposer
            )
        );

        builder.MapSerializer(
            typeof(GetCustomFilterResultMessageComposer),
            new GetCustomFilterResultMessageComposerSerializer(
                MessageComposer.GetCustomFilterResultMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(ModifyCustomFilterResultMessageComposer),
            new ModifyCustomFilterResultMessageComposerSerializer(
                MessageComposer.ModifyCustomFilterResultMessageComposer
            )
        );
    }
}
