using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

internal sealed class InitialResourceNodesPacket : IPacket
{
    internal sealed class ResourceNodeEntry : INetObject
    {
        public string SpawnerId;
        public byte State;
        public int DefinitionIndex;
        public int VariantIndex;
        public double DespawnAtWorldTime;
        public List<int> ResourcesToSpawn;

        public void Serialise(PacketWriter writer)
        {
            writer.WriteString(SpawnerId);
            writer.WriteByte(State);
            writer.WritePackedInt(DefinitionIndex);
            writer.WritePackedInt(VariantIndex);
            writer.WriteDouble(DespawnAtWorldTime);
            writer.WriteList(ResourcesToSpawn, PacketWriterDels.PackedInt);
        }

        public void Deserialise(PacketReader reader)
        {
            SpawnerId = reader.ReadPooledString()!;
            State = reader.ReadByte();
            DefinitionIndex = reader.ReadPackedInt();
            VariantIndex = reader.ReadPackedInt();
            DespawnAtWorldTime = reader.ReadDouble();
            ResourcesToSpawn = reader.ReadList(PacketReaderDels.PackedInt)!;
        }
    }

    public List<ResourceNodeEntry> Nodes;

    public PacketType Type => PacketType.InitialResourceNodes;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer) => writer.WriteList(Nodes, PacketWriterDels.NetObject<ResourceNodeEntry>.Writer);

    public void Deserialise(PacketReader reader) => Nodes = reader.ReadList(PacketReaderDels.NetObject<ResourceNodeEntry>.Reader)!;
}
