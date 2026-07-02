using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Labyrinth;

// Disruption forecasts are random rolls; only the host may generate them.
// Clients receive the host's forecast and derive area states from it.
[HarmonyPatch(typeof(PrismaDisruptionModel), nameof(PrismaDisruptionModel.PopulateTimedForecasts))]
internal static class DisableClientPrismaTimedForecasts
{
    public static bool Prefix() => !Main.Client.IsConnected;

    public static void Postfix() => NetworkPrismaManager.QueueBroadcast();
}

[HarmonyPatch(typeof(PrismaDisruptionModel), nameof(PrismaDisruptionModel.PopulateInitialForecasts))]
internal static class DisableClientPrismaInitialForecasts
{
    public static bool Prefix() => !Main.Client.IsConnected;

    public static void Postfix() => NetworkPrismaManager.QueueBroadcast();
}

[HarmonyPatch(typeof(PrismaDisruptionModel), nameof(PrismaDisruptionModel.ForecastAll))]
internal static class DisableClientPrismaForecastAll
{
    public static bool Prefix() => !Main.Client.IsConnected;

    public static void Postfix() => NetworkPrismaManager.QueueBroadcast();
}

[HarmonyPatch(typeof(PrismaDisruptionModel), nameof(PrismaDisruptionModel.ForecastGroup))]
internal static class DisableClientPrismaForecastGroup
{
    public static bool Prefix() => !Main.Client.IsConnected;

    public static void Postfix() => NetworkPrismaManager.QueueBroadcast();
}

[HarmonyPatch(typeof(PrismaDisruptionModel), nameof(PrismaDisruptionModel.ForecastGroups))]
internal static class DisableClientPrismaForecastGroups
{
    public static bool Prefix() => !Main.Client.IsConnected;

    public static void Postfix() => NetworkPrismaManager.QueueBroadcast();
}

[HarmonyPatch(typeof(PrismaDisruptionModel), nameof(PrismaDisruptionModel.SetForecastUnlocked))]
internal static class OnPrismaForecastUnlocked
{
    public static void Postfix() => NetworkPrismaManager.QueueBroadcast();
}
