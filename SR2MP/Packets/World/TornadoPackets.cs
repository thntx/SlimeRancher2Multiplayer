using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

internal sealed class TornadoSpawnPacket : IPacket
{
    public int TornadoId;
    public ushort ActivityNameHash;
    public Vector3 Position;
    public Quaternion Rotation;

    public PacketType Type => PacketType.TornadoSpawn;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.Weather;

    public void Serialise(PacketWriter writer)
    {
        writer.WritePackedInt(TornadoId);
        writer.WriteUShort(ActivityNameHash);
        writer.WriteVector3(Position);
        writer.WriteQuaternion(Rotation);
    }

    public void Deserialise(PacketReader reader)
    {
        TornadoId = reader.ReadPackedInt();
        ActivityNameHash = reader.ReadUShort();
        Position = reader.ReadVector3();
        Rotation = reader.ReadQuaternion();
    }
}

internal struct TornadoUpdatePacket : IPacket
{
    public int TornadoId;
    public Vector3 Position;

    public readonly PacketType Type => PacketType.TornadoUpdate;
    public readonly PacketReliability Reliability => PacketReliability.Unreliable;
    public readonly NetworkChannel Channel => NetworkChannel.Weather;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WritePackedInt(TornadoId);
        writer.WriteVector3(Position);
    }

    public void Deserialise(PacketReader reader)
    {
        TornadoId = reader.ReadPackedInt();
        Position = reader.ReadVector3();
    }
}

internal struct TornadoDespawnPacket : IPacket
{
    public int TornadoId;

    public readonly PacketType Type => PacketType.TornadoDespawn;
    public readonly PacketReliability Reliability => PacketReliability.Reliable;
    public readonly NetworkChannel Channel => NetworkChannel.Weather;

    public readonly void Serialise(PacketWriter writer) => writer.WritePackedInt(TornadoId);

    public void Deserialise(PacketReader reader) => TornadoId = reader.ReadPackedInt();
}
