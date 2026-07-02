using HarmonyLib;
using SR2MP.Components.Actor;
using SR2MP.Packets.Slime.Crystal;

namespace SR2MP.Patches.Slime.Crystal;

// Crystal slimes roll their spike positions on their own timers; only the
// owner spawns spikes and announces each one.
[HarmonyPatch(typeof(CrystalSlimeLaunch), nameof(CrystalSlimeLaunch.CreateSpikes),
    typeof(GameObject), typeof(Vector3))]
internal static class OnCrystalSpikesCreated
{
    public static bool Prefix(CrystalSlimeLaunch __instance, ref bool __result)
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected)
            return true;

        if (HandlingPacket)
            return true;

        var networkActor = __instance.GetComponent<NetworkActor>();
        if (networkActor && !networkActor.LocallyOwned)
        {
            __result = false;
            return false;
        }

        return true;
    }

    public static void Postfix(CrystalSlimeLaunch __instance, bool __result, GameObject prefab, Vector3 position)
    {
        if (HandlingPacket || !__result)
            return;

        if (!Main.Server.IsRunning && !Main.Client.IsConnected)
            return;

        var networkActor = __instance.GetComponent<NetworkActor>();
        if (!networkActor || !networkActor.LocallyOwned)
            return;

        var actorId = networkActor.ActorId;
        if (actorId.Value == 0)
            return;

        Main.SendToAllOrServer(new CrystalSpikesPacket
        {
            ActorId = actorId,
            Large = prefab == __instance._launchSpawnLarge,
            Position = position
        });
    }
}
