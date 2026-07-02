using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Slime.Boom;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Slime.Boom;

[PacketHandler((byte)PacketType.BoomSlimeExplode)]
internal sealed class BoomExplodeHandler : BasePacketHandler<BoomExplodePacket>
{
    protected override bool Handle(BoomExplodePacket packet, IPEndPoint? _)
    {
        if (!ActorManager.Actors.TryGetValue(packet.ActorId.Value, out var model))
            return true;

        if (!model.TryGetNetworkComponent(out var networkComponent))
            return true;

        var boom = networkComponent.GetComponent<BoomSlimeExplode>();
        if (!boom)
            return true;

        HandlingPacket = true;

        try
        {
            boom.Explode();
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"Failed to replay boom explosion: {ex.Message}");
        }
        finally
        {
            HandlingPacket = false;
        }

        return true;
    }
}
