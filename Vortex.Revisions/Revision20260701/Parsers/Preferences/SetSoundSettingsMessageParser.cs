using Vortex.Protocol.Messages.Incoming.Preferences;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Preferences;

internal class SetSoundSettingsMessageParser : IParser
{
    // Wire order is generic, furni, trax — NOT the (trax, furni, generic) the AS3
    // constructor signature suggests. WIN63's _SafePkg_2091/_SafeCls_2171 takes
    // (param1: trax, param2: furni, param3: generic) and pushes them back to front:
    //
    //     _SafeStr_4642.push(param3);   // generic, first on the wire
    //     _SafeStr_4642.push(param2);   // furni
    //     _SafeStr_4642.push(param1);   // trax, last
    //
    // Reading the signature alone put Trax first here, so the UI and Trax volumes were
    // stored swapped: turning the music down muted the interface instead. Corrected
    // 2026-08-02 against HabboSoundManagerFlash10::storeVolumeSetting()'s call site.
    public IMessageEvent Parse(IClientPacket packet) =>
        new SetSoundSettingsMessage
        {
            Generic = packet.PopInt(),
            Furni = packet.PopInt(),
            Trax = packet.PopInt(),
        };
}
