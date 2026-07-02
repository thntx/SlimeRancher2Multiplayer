using HarmonyLib;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.ResourceNode;

// Only the machine whose player started the harvest spawns the resources;
// everyone else receives them through the actor sync. Without this, every
// peer would pop its own copy of the loot out of the node.
[HarmonyPatch(typeof(Il2Cpp.ResourceNode), nameof(Il2Cpp.ResourceNode.SpawnResources))]
internal static class SuppressRemoteHarvestLoot
{
    public static bool Prefix(Il2Cpp.ResourceNode __instance)
        => !NetworkResourceNodeManager.IsRemoteHarvest(NetworkResourceNodeManager.GetSpawnerId(__instance));
}

[HarmonyPatch(typeof(Il2Cpp.ResourceNode), nameof(Il2Cpp.ResourceNode.SpawnSingleResource))]
internal static class SuppressRemoteHarvestSingleLoot
{
    public static bool Prefix(Il2Cpp.ResourceNode __instance)
        => !NetworkResourceNodeManager.IsRemoteHarvest(NetworkResourceNodeManager.GetSpawnerId(__instance));
}
