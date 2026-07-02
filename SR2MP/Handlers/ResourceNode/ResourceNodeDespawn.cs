using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.ResourceNode;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.ResourceNode;

[PacketHandler((byte)PacketType.ResourceNodeDespawn, HandlerType.Client)]
internal sealed class ResourceNodeDespawnHandler : BasePacketHandler<ResourceNodeDespawnPacket>
{
    protected override bool Handle(ResourceNodeDespawnPacket packet, IPEndPoint? _)
    {
        HandlingPacket = true;

        try
        {
            var spawner = NetworkResourceNodeManager.GetSpawner(packet.SpawnerId);

            if (spawner != null && spawner._attachedResourceNode)
                spawner.DespawnNode();
            else
                NetworkResourceNodeManager.ApplyNodeCleared(packet.SpawnerId);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to apply resource node despawn for '{packet.SpawnerId}': {ex}");
        }
        finally
        {
            HandlingPacket = false;
        }

        NetworkResourceNodeManager.ClearRemoteHarvest(packet.SpawnerId);

        return false;
    }
}
