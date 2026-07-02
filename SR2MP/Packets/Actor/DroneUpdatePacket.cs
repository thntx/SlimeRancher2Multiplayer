using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Actor;

/// <summary>
/// Position of a ranch drone, keyed by its station gadget's actor id since
/// the drone unit itself is spawned locally by each machine's station.
/// </summary>
internal struct DroneUpdatePacket : IPacket
{
    public long StationId;
    public Vector3 Position;
    public float Yaw;

    public readonly PacketType Type => PacketType.DroneUpdate;
    public readonly PacketReliability Reliability => PacketReliability.Unreliable;
    public readonly NetworkChannel Channel => NetworkChannel.ActorUpdate;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WritePackedLong(StationId);
        writer.WriteVector3(Position);
        writer.WriteFloat(Yaw);
    }

    public void Deserialise(PacketReader reader)
    {
        StationId = reader.ReadPackedLong();
        Position = reader.ReadVector3();
        Yaw = reader.ReadFloat();
    }
}
