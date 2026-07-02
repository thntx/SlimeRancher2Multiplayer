using HarmonyLib;
using SR2MP.Components.Actor;
using SR2MP.Packets.Slime.Boom;

namespace SR2MP.Patches.Slime.Boom;

// Boom slimes charge on their own timers per machine; only the owner may
// detonate, everyone else replays the owner's explosion.
[HarmonyPatch(typeof(BoomSlimeExplode), nameof(BoomSlimeExplode.Explode))]
internal static class OnBoomSlimeExplode
{
    public static bool Prefix(BoomSlimeExplode __instance)
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected)
            return true;

        if (HandlingPacket)
            return true;

        var networkActor = __instance.GetComponent<NetworkActor>();
        if (networkActor && !networkActor.LocallyOwned)
            return false;

        return true;
    }

    public static void Postfix(BoomSlimeExplode __instance)
    {
        if (HandlingPacket)
            return;

        if (!Main.Server.IsRunning && !Main.Client.IsConnected)
            return;

        var networkActor = __instance.GetComponent<NetworkActor>();
        if (!networkActor || !networkActor.LocallyOwned)
            return;

        var actorId = networkActor.ActorId;
        if (actorId.Value == 0)
            return;

        Main.SendToAllOrServer(new BoomExplodePacket { ActorId = actorId });
    }
}
