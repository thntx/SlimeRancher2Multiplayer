using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.LandPlots;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.LandPlots;

[PacketHandler((byte)PacketType.AutoFeederDispense)]
internal sealed class AutoFeederDispenseHandler : BasePacketHandler<AutoFeederDispensePacket>
{
    protected override bool Handle(AutoFeederDispensePacket packet, IPEndPoint? _)
    {
        // The plot may not be loaded on this side (e.g. the host is away from
        // the ranch); still relay the packet so other clients receive it.
        if (!GameState.landPlots.TryGetValue(packet.ID, out var model) || !model.gameObj)
            return true;

        var feeder = model.gameObj.GetComponentInChildren<SlimeFeeder>();
        if (feeder != null)
            feeder._nextEject = packet.NextTime;

        return true;
    }
}