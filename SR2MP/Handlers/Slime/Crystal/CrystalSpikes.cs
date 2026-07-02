using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Slime.Crystal;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Slime.Crystal;

[PacketHandler((byte)PacketType.CrystalSpikes)]
internal sealed class CrystalSpikesHandler : BasePacketHandler<CrystalSpikesPacket>
{
    protected override bool Handle(CrystalSpikesPacket packet, IPEndPoint? _)
    {
        if (!ActorManager.Actors.TryGetValue(packet.ActorId.Value, out var model))
            return true;

        if (!model.TryGetNetworkComponent(out var networkComponent))
            return true;

        var launch = networkComponent.GetComponent<CrystalSlimeLaunch>();
        if (!launch)
            return true;

        var prefab = packet.Large ? launch._launchSpawnLarge : launch._launchSpawnSmall;
        if (!prefab)
            return true;

        HandlingPacket = true;

        try
        {
            launch.CreateSpikes(prefab, packet.Position);
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"Failed to replay crystal spikes: {ex.Message}");
        }
        finally
        {
            HandlingPacket = false;
        }

        return true;
    }
}
