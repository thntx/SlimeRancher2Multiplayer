using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Weather;
using Il2CppMonomiPark.SlimeRancher.World;

namespace SR2MP.Patches.Weather;

// Clients never run WeatherRegistry.Update (see WeatherRegistryPatches), so the
// registry's own map calculation finds no running weather and the map shows no
// weather icons. Rebuild the icon list from the synced forecast model instead.
[HarmonyPatch(typeof(WeatherRegistry), nameof(WeatherRegistry.CalculateZoneMapData))]
internal static class MapWeatherIcons
{
    public static bool Prefix(WeatherRegistry __instance, ZoneMapData zoneMapData, CppCollections.List<ZoneWeatherMapData> mapData)
    {
        if (!Main.Client.IsConnected)
            return true;

        try
        {
            mapData.Clear();

            var zone = zoneMapData._weatherZone ? zoneMapData._weatherZone : zoneMapData._primaryZone;
            if (!zone || __instance._model == null)
                return false;

            if (!__instance._model._zoneDatas.TryGetValue(zone, out var zoneData))
                return false;

            var worldTime = SceneContext.Instance.TimeDirector._worldModel.worldTime;

            foreach (var forecast in zoneData.Forecast)
            {
                // The server only syncs forecasts that have started; the end time
                // guard covers entries that lapse between weather updates.
                if (!forecast.Started || forecast.EndTime <= worldTime)
                    continue;

                var pattern = forecast.Pattern;
                if (!pattern || !pattern.Metadata)
                    continue;

                if (zoneMapData._excludedWeatherPatterns != null && zoneMapData._excludedWeatherPatterns.Contains(pattern))
                    continue;

                var state = forecast.State?.TryCast<WeatherStateDefinition>();

                mapData.Add(new ZoneWeatherMapData
                {
                    Metadata = pattern.Metadata,
                    MapTier = state ? state!.MapTier : 0
                });
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to calculate networked zone map data: {ex}");
        }

        return false;
    }
}
