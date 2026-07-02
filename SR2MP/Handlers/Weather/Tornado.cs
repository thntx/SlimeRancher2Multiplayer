using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Weather;

[PacketHandler((byte)PacketType.TornadoSpawn, HandlerType.Client)]
internal sealed class TornadoSpawnHandler : BasePacketHandler<TornadoSpawnPacket>
{
    protected override bool Handle(TornadoSpawnPacket packet, IPEndPoint? _)
    {
        if (NetworkTornadoManager.Get(packet.TornadoId) != null)
            return false;

        var activity = NetworkTornadoManager.FindActivity(packet.ActivityNameHash);
        if (activity == null)
        {
            SrLogger.LogWarning($"No SpawnTornadoActivity found for hash {packet.ActivityNameHash}");
            return false;
        }

        HandlingPacket = true;

        try
        {
            var tornado = activity.Spawn(packet.Position, packet.Rotation);
            if (tornado)
                NetworkTornadoManager.RegisterClientTornado(packet.TornadoId, tornado);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to spawn networked tornado: {ex}");
        }
        finally
        {
            HandlingPacket = false;
        }

        return false;
    }
}

[PacketHandler((byte)PacketType.TornadoUpdate, HandlerType.Client)]
internal sealed class TornadoUpdateHandler : BasePacketHandler<TornadoUpdatePacket>
{
    protected override bool Handle(TornadoUpdatePacket packet, IPEndPoint? _)
    {
        NetworkTornadoManager.Get(packet.TornadoId)?.ReceivePosition(packet.Position);
        return false;
    }
}

[PacketHandler((byte)PacketType.TornadoDespawn, HandlerType.Client)]
internal sealed class TornadoDespawnHandler : BasePacketHandler<TornadoDespawnPacket>
{
    protected override bool Handle(TornadoDespawnPacket packet, IPEndPoint? _)
    {
        NetworkTornadoManager.Despawn(packet.TornadoId);
        return false;
    }
}
