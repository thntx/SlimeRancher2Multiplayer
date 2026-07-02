using System.Net;
using SR2MP.Components.Actor;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Actor;

[PacketHandler((byte)PacketType.DroneUpdate, HandlerType.Client)]
internal sealed class DroneUpdateHandler : BasePacketHandler<DroneUpdatePacket>
{
    protected override bool Handle(DroneUpdatePacket packet, IPEndPoint? _)
    {
        NetworkRanchDrone.Get(packet.StationId)?.ReceivePosition(packet.Position, packet.Yaw);
        return false;
    }
}
