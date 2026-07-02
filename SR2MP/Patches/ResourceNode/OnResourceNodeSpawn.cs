using HarmonyLib;
using SR2MP.Packets.ResourceNode;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.ResourceNode;

[HarmonyPatch(typeof(ResourceNodeSpawner), nameof(ResourceNodeSpawner.SpawnNode))]
internal static class OnResourceNodeSpawn
{
    public static void Postfix(ResourceNodeSpawner __instance)
    {
        if (HandlingPacket || !Main.Server.IsRunning)
            return;

        var model = __instance._model;
        if (model == null)
            return;

        var resources = new List<int>();
        if (model.resourcesToSpawn != null)
        {
            foreach (var resource in model.resourcesToSpawn)
                resources.Add(NetworkActorManager.GetPersistentID(resource));
        }

        var packet = new ResourceNodeSpawnPacket
        {
            SpawnerId = __instance.Id,
            DefinitionIndex = NetworkResourceNodeManager.GetDefinitionIndex(model.resourceNodeDefinition),
            VariantIndex = model.resourceNodeVariantIndex,
            DespawnAtWorldTime = model.despawnAtWorldTime,
            ResourcesToSpawn = resources
        };

        Main.Server.SendToAll(packet);
    }
}

[HarmonyPatch(typeof(ResourceNodeSpawner), nameof(ResourceNodeSpawner.DespawnNode))]
internal static class OnResourceNodeDespawn
{
    public static void Postfix(ResourceNodeSpawner __instance)
    {
        if (HandlingPacket || !Main.Server.IsRunning)
            return;

        NetworkResourceNodeManager.ClearRemoteHarvest(__instance.Id);

        Main.Server.SendToAll(new ResourceNodeDespawnPacket { SpawnerId = __instance.Id });
    }
}
