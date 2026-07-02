using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Player;

[PacketHandler((byte)PacketType.PlayerInventory)]
internal sealed class PlayerInventoryHandler : BasePacketHandler<PlayerInventoryPacket>
{
    protected override bool Handle(PlayerInventoryPacket packet, IPEndPoint? _)
    {
        if (IsServerSide)
        {
            // A client reported its inventory; persist it for the next join.
            PlayerInventoryStore.Set(CurrentSaveName(), packet.PlayerId, packet.Slots);
        }
        else if (packet.PlayerId == Main.Client.PlayerId)
        {
            // The server restored this client's stored inventory.
            NetworkPlayerInventory.ApplySlots(packet.Slots);
        }

        return false;
    }

    internal static string CurrentSaveName()
    {
        try
        {
            return GameContext.Instance?.AutoSaveDirector?.CurrentSaveGameName() ?? "unknown";
        }
        catch
        {
            return "unknown";
        }
    }
}
