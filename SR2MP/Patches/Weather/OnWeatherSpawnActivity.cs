using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Weather.Activity;
using SR2MP.Packets.World;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;

namespace SR2MP.Patches.Weather;

// Weather spawn activities (actors, storm resources, tornados, tangle stalks)
// roll their own timing and positions per machine, so clients must not run
// them: the host's spawned actors arrive through the actor sync and tornados
// are mirrored explicitly below.
[HarmonyPatch(typeof(SpawnActivity), nameof(SpawnActivity.OnFixedUpdate))]
internal static class DisableClientWeatherSpawns
{
    public static bool Prefix() => !Main.Client.IsConnected;
}

[HarmonyPatch(typeof(ReleaseResourcesActivity), nameof(ReleaseResourcesActivity.OnReleaseTime))]
internal static class DisableClientWeatherResourceReleases
{
    public static bool Prefix() => !Main.Client.IsConnected;
}

[HarmonyPatch(typeof(SpawnTornadoActivity), nameof(SpawnTornadoActivity.Spawn))]
internal static class OnTornadoSpawn
{
    public static void Postfix(SpawnTornadoActivity __instance, GameObject __result, Vector3 position, Quaternion rotation)
    {
        if (HandlingPacket || !Main.Server.IsRunning || !__result)
            return;

        var id = NetworkTornadoManager.RegisterHostTornado(__result);

        Main.Server.SendToAll(new TornadoSpawnPacket
        {
            TornadoId = id,
            ActivityNameHash = __instance.name.Hash16(),
            Position = position,
            Rotation = rotation
        });
    }
}
