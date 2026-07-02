using SR2MP.Packets.Utils;

namespace SR2MP.Packets.ResourceNode;

internal sealed class ResourceNodeDespawnPacket : IPacket
{
    public string SpawnerId;

    public PacketType Type => PacketType.ResourceNodeDespawn;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer) => writer.WriteString(SpawnerId);

    public void Deserialise(PacketReader reader) => SpawnerId = reader.ReadPooledString()!;
}
