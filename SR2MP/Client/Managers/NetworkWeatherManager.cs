using System.Collections;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Weather;
using Il2CppMonomiPark.SlimeRancher.World;
using SR2MP.Packets.World;
using SR2MP.Server.Managers;

namespace SR2MP.Client.Managers;

internal static class NetworkWeatherManager
{
    public static WeatherRegistry Registry => SceneContext.Instance.WeatherRegistry;

    public static WeatherDirector Director
    {
        get
        {
            if (!director)
            {
                director = Resources.FindObjectsOfTypeAll<WeatherDirector>().FirstOrDefault()!;
            }

            return director;
        }
    }

    public static LightningStrike Lightning
    {
        get
        {
            if (!lightning)
            {
                lightning = Resources.FindObjectsOfTypeAll<LightningStrike>().First(x => x.BlastPower < 2749f);
            }

            return lightning;
        }
    }

    private static LightningStrike lightning;
    private static WeatherDirector director;

    public static readonly Dictionary<int, WeatherStateDefinition> WeatherStates = new();

    internal static void Initialize()
    {
        var refer = GameContext.Instance.AutoSaveDirector._saveReferenceTranslation;
        foreach (var state in refer._weatherStateTranslation.RawLookupDictionary)
        {
            WeatherStates.Add(refer.GetPersistenceId(state.value), state.value.TryCast<WeatherStateDefinition>()!);
        }
    }

    public static void CheckInitialized()
    {
        if (WeatherStates.Count == 0)
            Initialize();
    }

    public static int GetPersistentID(WeatherStateDefinition state)
        => GameContext.Instance.AutoSaveDirector._saveReferenceTranslation
            .GetPersistenceId(state.Cast<IWeatherState>());

    /// <summary>
    /// True while a weather snapshot is being applied. Weather patches treat
    /// this like <see cref="GlobalVariables.HandlingPacket"/>, but scoped so a
    /// multi-frame apply cannot mute every other system in the mod.
    /// </summary>
    private static bool applyingWeatherFlag;
    private static float applyingWeatherSince;

    /// <summary>
    /// Self-clears after 30s as a watchdog: if the apply coroutine is
    /// abandoned by the runner mid-exception, a stuck flag would otherwise
    /// permanently stop weather from applying.
    /// </summary>
    internal static bool ApplyingWeather
    {
        get => applyingWeatherFlag && UnityEngine.Time.unscaledTime - applyingWeatherSince < 30f;
        private set
        {
            applyingWeatherFlag = value;
            applyingWeatherSince = UnityEngine.Time.unscaledTime;
        }
    }

    internal static IEnumerator Apply(WeatherPacket packet, bool immediate)
    {
        yield return new WaitFrames(3);
        ApplyingWeather = true;

        try
        {

        var registry = Registry;
        var localDirector = Director;

        // Snapshot the zone keys in one frame; iterating the live dictionary
        // across yields dies when the registry mutates it.
        var zoneKeys = new List<ZoneDefinition>();
        foreach (var zone in registry._zones)
            zoneKeys.Add(zone.Key);

        byte zoneId = 0;
        foreach (var zoneKey in zoneKeys)
        {
            if (!packet.Zones.TryGetValue(zoneId, out var data))
                continue;

            var zone = registry._zones[zoneKey];

            var forecastCopy = new List<WeatherModel.ForecastEntry>();
            foreach (var forecast in zone.Forecast)
                forecastCopy.Add(forecast);

            foreach (var forecast in forecastCopy)
            {
                yield return null;

                var state = forecast.State?.TryCast<IWeatherState>();
                if (state == null)
                    continue;

                var patternInstance = registry.GetWeatherPatternInstance(
                    zoneKey,
                    forecast.Pattern
                );

                if (patternInstance == null)
                {
                    localDirector.StopState(state, zone.Parameters);
                }
                else
                {
                    registry.StopPatternState(
                        zoneKey,
                        patternInstance,
                        forecast.State
                    );
                }

                yield return new WaitFrames(2);
            }

            zone.Forecast.Clear();
            zone.Parameters.WindDirection = data.WindSpeed;

            foreach (var forecast in data.WeatherForecasts)
            {
                var state = forecast.State?.TryCast<IWeatherState>();
                if (state == null)
                    continue;

                var pattern = WeatherUpdateHelper.GetPatternForZoneAndState(zoneKey, forecast.State!.name);
                yield return null;

                zone.Forecast.Add(new WeatherModel.ForecastEntry
                {
                    State = state,
                    Pattern = pattern,
                    Started = forecast.WeatherStarted,
                    StartTime = forecast.StartTime,
                    EndTime = forecast.EndTime
                });

                yield return new WaitFrames(2);
            }

            yield return null;
            zoneId++;
            yield return new WaitFrames(2);
        }

        if (!registry._zones.TryGetValue(localDirector.Zone, out var activeZone))
            yield break;

        // Copied in one frame: iterating the live forecast list across
        // yields dies when the game updates it mid-apply.
        var activeCopy = new List<WeatherModel.ForecastEntry>();
        foreach (var activeForecast in activeZone.Forecast)
            activeCopy.Add(activeForecast);

        yield return null;

        foreach (var forecast in activeCopy)
        {
            yield return null;

            var state = forecast.State?.TryCast<IWeatherState>();
            if (state == null)
                continue;

            var patternInstance = registry.GetWeatherPatternInstance(
                localDirector.Zone,
                forecast.Pattern
            );

            yield return null;
            if (patternInstance == null)
            {
                localDirector.RunState(state, activeZone.Parameters, immediate);
            }
            else
            {
                registry.RunPatternState(
                    localDirector.Zone,
                    patternInstance,
                    forecast.State,
                    immediate
                );
            }

            yield return new WaitFrames(3);
        }

        }
        finally
        {
            ApplyingWeather = false;
        }
    }
}