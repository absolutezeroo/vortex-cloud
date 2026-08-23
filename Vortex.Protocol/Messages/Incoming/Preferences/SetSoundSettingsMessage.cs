using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Preferences;

public record SetSoundSettingsMessage : IMessageEvent
{
    // HabboSoundManagerFlash10::storeVolumeSetting() sends (trax, furni, generic) — header 3662.
    public required int Trax { get; init; }
    public required int Furni { get; init; }
    public required int Generic { get; init; }
}
