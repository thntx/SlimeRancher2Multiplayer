using SR2MP.Packets.Utils;

namespace SR2MP.Packets.ResourceNode;

internal sealed class ResourceNodeSpawnPacket : IPacket
{
    public string SpawnerId;
    public int DefinitionIndex;
    public int VariantIndex;
    public double DespawnAtWorldTime;
    public List<int> ResourcesToSpawn;

    public PacketType Type => PacketType.ResourceNodeSpawn;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(SpawnerId);
        writer.WritePackedInt(DefinitionIndex);
        writer.WritePackedInt(VariantIndex);
        writer.WriteDouble(DespawnAtWorldTime);
        writer.WriteList(ResourcesToSpawn, PacketWriterDels.PackedInt);
    }

    public void Deserialise(PacketReader reader)
    {
        SpawnerId = reader.ReadPooledString()!;
        DefinitionIndex = reader.ReadPackedInt();
        VariantIndex = reader.ReadPackedInt();
        DespawnAtWorldTime = reader.ReadDouble();
        ResourcesToSpawn = reader.ReadList(PacketReaderDels.PackedInt)!;
    }
}
