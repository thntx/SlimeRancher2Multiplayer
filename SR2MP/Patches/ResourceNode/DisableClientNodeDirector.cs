using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.World.ResourceNode;

namespace SR2MP.Patches.ResourceNode;

// Node spawning is randomized per machine, so clients must not run their own
// director; the host decides which node appears at which spawner.
[HarmonyPatch(typeof(ResourceNodeDirector), nameof(ResourceNodeDirector.Update))]
internal static class DisableClientNodeDirector
{
    public static bool Prefix() => !Main.Client.IsConnected;
}

[HarmonyPatch(typeof(ResourceNodeDirector), nameof(ResourceNodeDirector.SpawnNode))]
internal static class DisableClientNodeDirectorSpawn
{
    public static bool Prefix() => !Main.Client.IsConnected || HandlingPacket;
}
