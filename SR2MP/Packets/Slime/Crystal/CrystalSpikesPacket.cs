using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Slime.Crystal;

internal struct CrystalSpikesPacket : IPacket
{
    public ActorId ActorId;
    public bool Large;
    public Vector3 Position;

    public readonly PacketType Type => PacketType.CrystalSpikes;
    public readonly PacketReliability Reliability => PacketReliability.Reliable;
    public readonly NetworkChannel Channel => NetworkChannel.ActorCritical;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WritePackedLong(ActorId.Value);
        writer.WritePackedBool(Large);
        writer.WriteVector3(Position);
    }

    public void Deserialise(PacketReader reader)
    {
        ActorId = new ActorId(reader.ReadPackedLong());
        Large = reader.ReadPackedBool();
        Position = reader.ReadVector3();
    }
}
