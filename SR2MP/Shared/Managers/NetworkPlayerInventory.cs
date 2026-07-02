using System.Collections;
using Il2CppMonomiPark.SlimeRancher.Player;
using SR2MP.Packets.Player;

namespace SR2MP.Shared.Managers;

/// <summary>
/// Captures and applies the local player's vac inventory for persistence on
/// the host. Only clients report their inventory; the host's own inventory
/// already lives in its save file.
/// </summary>
internal static class NetworkPlayerInventory
{
    private const float AutosaveInterval = 60f;
    private static float autosaveTimer = AutosaveInterval;

    /// <summary>
    /// Ticked from the local player's update loop; periodically reports the
    /// inventory so a crash only loses the last interval.
    /// </summary>
    public static void TickAutosave(float deltaTime)
    {
        if (!Main.Client.IsConnected)
            return;

        autosaveTimer -= deltaTime;
        if (autosaveTimer > 0f)
            return;

        autosaveTimer = AutosaveInterval;
        SendLocalInventory();
    }

    public static void SendLocalInventory()
    {
        if (!Main.Client.IsConnected)
            return;

        var slots = CaptureLocalSlots();
        if (slots == null)
            return;

        Main.Client.SendPacket(new PlayerInventoryPacket
        {
            PlayerId = Main.Client.PlayerId,
            Slots = slots
        });
    }

    private static List<PlayerInventoryPacket.InventorySlot>? CaptureLocalSlots()
    {
        try
        {
            var ammo = SceneContext.Instance?.PlayerState?.Ammo;
            if (ammo?._ammoModel?.Slots == null)
                return null;

            var slots = new List<PlayerInventoryPacket.InventorySlot>();

            for (var i = 0; i < ammo._ammoModel.Slots.Count; i++)
            {
                var slot = ammo.Slots[i];
                var ident = slot?._id;

                slots.Add(new PlayerInventoryPacket.InventorySlot
                {
                    Identifiable = ident ? NetworkActorManager.GetPersistentID(ident!) : -1,
                    Count = slot?._count ?? 0
                });
            }

            return slots;
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"Could not capture local inventory: {ex.Message}");
            return null;
        }
    }

    public static void ApplySlots(List<PlayerInventoryPacket.InventorySlot> slots)
        => StartCoroutine(ApplySlotsRoutine(slots));

    private static IEnumerator ApplySlotsRoutine(List<PlayerInventoryPacket.InventorySlot> slots)
    {
        // Give the join synchronisation (upgrades in particular) time to land
        // so locked slots are already unlocked before items are inserted.
        yield return new WaitFrames(15);

        var ammo = SceneContext.Instance?.PlayerState?.Ammo;
        if (ammo == null)
            yield break;

        HandlingPacket = true;

        try
        {
            ammo.Clear();

            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];

                if (slot.Identifiable < 0 || slot.Count <= 0)
                    continue;

                if (!ActorManager.ActorTypes.TryGetValue(slot.Identifiable, out var ident) || !ident)
                    continue;

                ammo.MaybeAddToSpecificSlot(new AmmoSlot.AmmoMetadata(ident), i, slot.Count, false);
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to apply networked inventory: {ex}");
        }
        finally
        {
            HandlingPacket = false;
        }

        SrLogger.LogMessage("Restored vac inventory from the server.");
    }
}
