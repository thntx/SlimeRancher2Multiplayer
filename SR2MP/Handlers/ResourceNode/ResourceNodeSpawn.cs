using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.ResourceNode;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.ResourceNode;

[PacketHandler((byte)PacketType.ResourceNodeSpawn, HandlerType.Client)]
internal sealed class ResourceNodeSpawnHandler : BasePacketHandler<ResourceNodeSpawnPacket>
{
    protected override bool Handle(ResourceNodeSpawnPacket packet, IPEndPoint? _)
    {
        HandlingPacket = true;

        try
        {
            NetworkResourceNodeManager.ApplyNodeData(
                packet.SpawnerId,
                packet.DefinitionIndex,
                packet.VariantIndex,
                packet.DespawnAtWorldTime,
                packet.ResourcesToSpawn,
                Il2Cpp.ResourceNode.NodeState.READY);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to apply resource node spawn for '{packet.SpawnerId}': {ex}");
        }
        finally
        {
            HandlingPacket = false;
        }

        return false;
    }
}
