using HarmonyLib;
using SR2MP.Packets.TreasurePod;

namespace SR2MP.Patches.TreasurePod;

[HarmonyPatch(typeof(Il2Cpp.TreasurePod), nameof(Il2Cpp.TreasurePod.Activate))]
internal static class OnTreasurePodOpen
{
    public static void Postfix(Il2Cpp.TreasurePod __instance)
    {
        if (HandlingPacket)
            return;

        if (!int.TryParse(__instance._id.Replace("pod", string.Empty), out var podId))
        {
            SrLogger.LogWarning($"Could not parse treasure pod id '{__instance._id}'");
            return;
        }

        Main.SendToAllOrServer(new TreasurePodPacket { ID = podId });
    }
}