using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Player.CharacterController;
using SR2MP.Packets.FX;

namespace SR2MP.Patches.FX;

// Splashes from synced actors already play on every machine through their own
// water volumes; only the local player's splash needs to be networked, since
// remote player objects carry no rigidbody and never trip the trigger.
[HarmonyPatch(typeof(SplashOnTrigger), nameof(SplashOnTrigger.SpawnAndPlayFX))]
internal static class OnWaterSplash
{
    public static void Postfix(SplashOnTrigger __instance, GameObject prefab, Collider collider)
    {
        if (HandlingPacket)
            return;

        if (!Main.Server.IsRunning && !Main.Client.IsConnected)
            return;

        if (!prefab || !collider)
            return;

        if (!collider.GetComponentInParent<SRCharacterController>(true))
            return;

        var position = collider.bounds.center;

        // Splash at the water surface when the volume exposes it.
        var splashColliders = __instance.splashColliders;
        if (splashColliders != null && splashColliders.Length > 0 && splashColliders[0])
            position.y = splashColliders[0]!.bounds.max.y;

        var packet = new PlayerFXPacket
        {
            FX = PlayerFXType.WaterSplash,
            Position = position,
            FXName = prefab.name.Replace(' ', '_')
        };

        Main.SendToAllOrServer(packet);
    }
}
