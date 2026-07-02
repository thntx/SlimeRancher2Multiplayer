using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Player;

/// <summary>
/// Carries a player's vac inventory. Clients report theirs to the server for
/// persistence; the server sends the stored inventory back on join.
/// </summary>
internal sealed class PlayerInventoryPacket : IPacket
{
    internal struct InventorySlot : INetObject
    {
        /// <summary>Persistent id of the stored type, or -1 for an empty slot.</summary>
        public int Identifiable;
        public int Count;

        public readonly void Serialise(PacketWriter writer)
        {
            writer.WritePackedInt(Identifiable);
            writer.WritePackedInt(Count);
        }

        public void Deserialise(PacketReader reader)
        {
            Identifiable = reader.ReadPackedInt();
            Count = reader.ReadPackedInt();
        }
    }

    public string PlayerId;
    public List<InventorySlot> Slots;

    public PacketType Type => PacketType.PlayerInventory;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteStringWithoutSize(PlayerId);
        writer.WriteList(Slots, PacketWriterDels.NetObject<InventorySlot>.Writer);
    }

    public void Deserialise(PacketReader reader)
    {
        PlayerId = reader.ReadPooledStringOfSize(16)!;
        Slots = reader.ReadList(PacketReaderDels.NetObject<InventorySlot>.Reader)!;
    }
}
