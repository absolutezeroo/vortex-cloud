using Vortex.Primitives.Networking.Revisions;
using Vortex.Protocol.Messages.Outgoing.Preferences;
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

        // Discord Rich Presence. The client asks for these at init on every login, so an unmapped
        // 2883 was one "Incoming Unknown" per session and a settings dialog that refused to open.
        builder.MapParser(
            MessageEvent.GetDiscordPreferencesMessageEvent,
            new GetDiscordPreferencesMessageParser()
        );
        builder.MapParser(
            MessageEvent.SetDiscordPreferencesMessageEvent,
            new SetDiscordPreferencesMessageParser()
        );

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
            typeof(DiscordPreferencesEventMessageComposer),
            new DiscordPreferencesEventMessageComposerSerializer(
                MessageComposer.DiscordPreferencesComposer
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
