using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Drone;
using SR2MP.Components.Actor;

namespace SR2MP.Patches.Drone;

// Ranch drones are simulated by the host only. Each machine's drone AI would
// otherwise consume its own copies of items and double-deposit the results
// (money, refinery counts, feeder ammo) through the already-synced systems.
[HarmonyPatch(typeof(RanchDrone), nameof(RanchDrone.RegistryFixedUpdate))]
internal static class DisableClientRanchDroneBrain
{
    public static bool Prefix() => !Main.Client.IsConnected || Main.Server.IsRunning;
}

// The fast forwarder replays the drone work that happened "while away";
// on clients that work already happened on the host and synced over.
[HarmonyPatch(typeof(Il2Cpp.DroneFastForwarder), nameof(Il2Cpp.DroneFastForwarder.FastForward),
    typeof(RanchDrone), typeof(double), typeof(double))]
internal static class DisableClientDroneFastForward
{
    public static bool Prefix() => !Main.Client.IsConnected || Main.Server.IsRunning;
}

[HarmonyPatch(typeof(RanchDrone), nameof(RanchDrone.InitStart))]
internal static class OnRanchDroneInitStart
{
    public static void Postfix(RanchDrone __instance)
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected)
            return;

        if (!__instance.GetComponent<NetworkRanchDrone>())
            __instance.gameObject.AddComponent<NetworkRanchDrone>();
    }
}
