using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.ResourceNode;

[PacketHandler((byte)PacketType.InitialResourceNodes, HandlerType.Client)]
internal sealed class InitialResourceNodesHandler : BasePacketHandler<InitialResourceNodesPacket>
{
    protected override bool Handle(InitialResourceNodesPacket packet, IPEndPoint? _)
    {
        HandlingPacket = true;

        try
        {
            foreach (var node in packet.Nodes)
            {
                try
                {
                    NetworkResourceNodeManager.ApplyNodeData(
                        node.SpawnerId,
                        node.DefinitionIndex,
                        node.VariantIndex,
                        node.DespawnAtWorldTime,
                        node.ResourcesToSpawn,
                        (Il2Cpp.ResourceNode.NodeState)node.State);
                }
                catch (Exception ex)
                {
                    SrLogger.LogError($"Failed to apply initial resource node '{node.SpawnerId}': {ex}");
                }
            }
        }
        finally
        {
            HandlingPacket = false;
        }

        return false;
    }
}
