using System.Collections;
using Il2CppMonomiPark.SlimeRancher.Labyrinth;
using SR2MP.Packets.World;
using SR2MP.Shared.Utils;

namespace SR2MP.Shared.Managers;

/// <summary>
/// Builds and applies Gray Labyrinth disruption forecast snapshots. The
/// disruption director derives area states deterministically from the model
/// and world time, so mirroring the model is enough to keep peers in step.
/// </summary>
internal static class NetworkPrismaManager
{
    private static bool sendPending;

    /// <summary>
    /// Debounced host-side broadcast; forecast generation touches several
    /// groups in a row and one snapshot at the end covers all of them.
    /// </summary>
    public static void QueueBroadcast()
    {
        if (sendPending || !Main.Server.IsRunning)
            return;

        sendPending = true;
        StartCoroutine(BroadcastRoutine());
    }

    private static IEnumerator BroadcastRoutine()
    {
        yield return new WaitFrames(5);
        sendPending = false;

        if (!Main.Server.IsRunning)
            yield break;

        var packet = BuildPacket();
        if (packet != null)
            Main.Server.SendToAll(packet);
    }

    public static PrismaForecastPacket? BuildPacket()
    {
        try
        {
            var model = GameState.prismaDisruptionModel;
            if (model?._areaDatas == null)
                return null;

            var areas = new List<PrismaForecastPacket.AreaData>();

            foreach (var areaData in model._areaDatas)
            {
                var definition = areaData.Key;
                var data = areaData.Value;

                if (!definition || data == null)
                    continue;

                var patterns = new List<PrismaForecastPacket.PatternData>();

                if (data.Forecast != null)
                {
                    foreach (var pattern in data.Forecast)
                    {
                        if (pattern?.Entries == null)
                            continue;

                        var patternData = new PrismaForecastPacket.PatternData
                        {
                            EndTime = pattern.EndTime,
                            StartTimes = new List<double>(),
                            Levels = new List<byte>()
                        };

                        foreach (var entry in pattern.Entries)
                        {
                            patternData.StartTimes.Add(entry.StartTime);
                            patternData.Levels.Add((byte)entry.Level);
                        }

                        patterns.Add(patternData);
                    }
                }

                areas.Add(new PrismaForecastPacket.AreaData
                {
                    AreaNameHash = definition.name.Hash16(),
                    ForecastUnlocked = data.ForecastUnlocked,
                    Patterns = patterns
                });
            }

            return new PrismaForecastPacket { Areas = areas };
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to build prisma forecast packet: {ex}");
            return null;
        }
    }

    public static void Apply(PrismaForecastPacket packet)
    {
        var model = GameState.prismaDisruptionModel;
        if (model?._areaDatas == null)
            return;

        var worldTime = SceneContext.Instance.TimeDirector._worldModel.worldTime;
        var applied = false;

        foreach (var areaData in model._areaDatas)
        {
            var definition = areaData.Key;
            var data = areaData.Value;

            if (!definition || data == null)
                continue;

            var hash = definition.name.Hash16();
            var incoming = packet.Areas.Find(area => area.AreaNameHash == hash);
            if (incoming == null)
                continue;

            data.Forecast?.Clear();

            foreach (var patternData in incoming.Patterns)
            {
                var startTimes = new CppCollections.List<double>();
                var levels = new CppCollections.List<DisruptionLevel>();

                for (var i = 0; i < patternData.StartTimes.Count && i < patternData.Levels.Count; i++)
                {
                    startTimes.Add(patternData.StartTimes[i]);
                    levels.Add((DisruptionLevel)patternData.Levels[i]);
                }

                var pattern = DisruptionPattern.CreateTimedPattern(startTimes, levels, patternData.EndTime);
                data.Forecast?.Add(pattern);
            }

            data.ForecastUnlocked = incoming.ForecastUnlocked;
            data.CacheChangeTime(worldTime);
            applied = true;
        }

        if (!applied)
            return;

        model._areaRefreshNeeded = true;
        model.NotifyParticipants();
    }
}
