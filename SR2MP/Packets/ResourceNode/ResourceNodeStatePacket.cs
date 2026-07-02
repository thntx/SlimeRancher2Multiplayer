using SR2MP.Packets.Utils;

namespace SR2MP.Packets.ResourceNode;

internal sealed class ResourceNodeStatePacket : IPacket
{
    public string SpawnerId;
    public byte State;

    public PacketType Type => PacketType.ResourceNodeState;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(SpawnerId);
        writer.WriteByte(State);
    }

    public void Deserialise(PacketReader reader)
    {
        SpawnerId = reader.ReadPooledString()!;
        State = reader.ReadByte();
    }
}
