using HarmonyLib;
using SR2MP.Packets.ResourceNode;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.ResourceNode;

internal static class ResourceNodeStateSender
{
    public static void Send(Il2Cpp.ResourceNode node, Il2Cpp.ResourceNode.NodeState state)
    {
        if (HandlingPacket)
            return;

        if (!Main.Server.IsRunning && !Main.Client.IsConnected)
            return;

        var spawnerId = NetworkResourceNodeManager.GetSpawnerId(node);
        if (spawnerId == null)
            return;

        if (state == Il2Cpp.ResourceNode.NodeState.READY)
        {
            NetworkResourceNodeManager.ClearRemoteHarvest(spawnerId);
        }
        else if (NetworkResourceNodeManager.IsRemoteHarvest(spawnerId))
        {
            // This transition is the local side playing out a harvest that was
            // started remotely; the origin already announced it.
            return;
        }

        Main.SendToAllOrServer(new ResourceNodeStatePacket
        {
            SpawnerId = spawnerId,
            State = (byte)state
        });
    }
}

[HarmonyPatch(typeof(Il2Cpp.ResourceNode), nameof(Il2Cpp.ResourceNode.SetStateReady))]
internal static class OnResourceNodeReady
{
    public static void Postfix(Il2Cpp.ResourceNode __instance)
        => ResourceNodeStateSender.Send(__instance, Il2Cpp.ResourceNode.NodeState.READY);
}

[HarmonyPatch(typeof(Il2Cpp.ResourceNode), nameof(Il2Cpp.ResourceNode.SetStateHarvesting))]
internal static class OnResourceNodeHarvesting
{
    public static void Postfix(Il2Cpp.ResourceNode __instance)
        => ResourceNodeStateSender.Send(__instance, Il2Cpp.ResourceNode.NodeState.HARVESTING);
}

[HarmonyPatch(typeof(Il2Cpp.ResourceNode), nameof(Il2Cpp.ResourceNode.SetStateEmpty))]
internal static class OnResourceNodeEmptied
{
    public static void Postfix(Il2Cpp.ResourceNode __instance)
        => ResourceNodeStateSender.Send(__instance, Il2Cpp.ResourceNode.NodeState.HARVESTED);
}
