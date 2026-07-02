using System.Text;
using MelonLoader.Utils;
using SR2MP.Packets.Player;

namespace SR2MP.Server.Managers;

/// <summary>
/// File-backed store of client vac inventories, keyed by host save name and
/// player id, so returning players get their items back.
/// </summary>
internal static class PlayerInventoryStore
{
    private static Dictionary<(string Save, string Player), List<PlayerInventoryPacket.InventorySlot>>? cache;

    private static string FilePath
        => Path.Combine(MelonEnvironment.UserDataDirectory, "SR2MP", "player_inventories.txt");

    public static void Set(string saveName, string playerId, List<PlayerInventoryPacket.InventorySlot> slots)
    {
        try
        {
            EnsureLoaded();
            cache![(saveName, playerId)] = slots;
            WriteFile();
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to store inventory for {playerId}: {ex}");
        }
    }

    public static List<PlayerInventoryPacket.InventorySlot>? Get(string saveName, string playerId)
    {
        try
        {
            EnsureLoaded();
            return cache!.GetValueOrDefault((saveName, playerId));
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to load inventory for {playerId}: {ex}");
            return null;
        }
    }

    private static void EnsureLoaded()
    {
        if (cache != null)
            return;

        cache = new Dictionary<(string, string), List<PlayerInventoryPacket.InventorySlot>>();

        if (!File.Exists(FilePath))
            return;

        foreach (var line in File.ReadAllLines(FilePath))
        {
            var parts = line.Split('\t');
            if (parts.Length != 3)
                continue;

            var slots = new List<PlayerInventoryPacket.InventorySlot>();
            foreach (var slotData in parts[2].Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = slotData.Split(':');
                if (pair.Length == 2 && int.TryParse(pair[0], out var id) && int.TryParse(pair[1], out var count))
                    slots.Add(new PlayerInventoryPacket.InventorySlot { Identifiable = id, Count = count });
            }

            cache[(Decode(parts[0]), parts[1])] = slots;
        }
    }

    private static void WriteFile()
    {
        var directory = Path.GetDirectoryName(FilePath)!;
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var builder = new StringBuilder();
        foreach (var ((save, player), slots) in cache!)
        {
            builder.Append(Encode(save)).Append('\t').Append(player).Append('\t');
            builder.AppendJoin(';', slots.Select(slot => $"{slot.Identifiable}:{slot.Count}"));
            builder.AppendLine();
        }

        File.WriteAllText(FilePath, builder.ToString());
    }

    // Save names are user text; base64 keeps the line format unambiguous.
    private static string Encode(string value)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    private static string Decode(string value)
    {
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
        catch (FormatException)
        {
            return value;
        }
    }
}
