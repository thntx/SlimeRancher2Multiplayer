using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.ResourceNode;

[HarmonyPatch(typeof(GameModel), nameof(GameModel.RegisterResourceNodeSpawner))]
internal static class OnResourceNodeSpawnerRegistered
{
    public static void Postfix(ResourceNodeSpawner resourceNodeSpawner)
        => NetworkResourceNodeManager.Register(resourceNodeSpawner);
}
