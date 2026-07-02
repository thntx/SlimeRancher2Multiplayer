using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.ResourceNode;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.ResourceNode;

[PacketHandler((byte)PacketType.ResourceNodeState)]
internal sealed class ResourceNodeStateHandler : BasePacketHandler<ResourceNodeStatePacket>
{
    protected override bool Handle(ResourceNodeStatePacket packet, IPEndPoint? _)
    {
        HandlingPacket = true;

        try
        {
            NetworkResourceNodeManager.ApplyNodeState(
                packet.SpawnerId,
                (Il2Cpp.ResourceNode.NodeState)packet.State);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to apply resource node state for '{packet.SpawnerId}': {ex}");
        }
        finally
        {
            HandlingPacket = false;
        }

        return true;
    }
}
