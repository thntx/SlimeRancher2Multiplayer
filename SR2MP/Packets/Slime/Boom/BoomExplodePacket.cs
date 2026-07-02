using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Slime.Boom;

internal struct BoomExplodePacket : IPacket
{
    public ActorId ActorId;

    public readonly PacketType Type => PacketType.BoomSlimeExplode;
    public readonly PacketReliability Reliability => PacketReliability.Reliable;
    public readonly NetworkChannel Channel => NetworkChannel.ActorCritical;

    public readonly void Serialise(PacketWriter writer) => writer.WritePackedLong(ActorId.Value);

    public void Deserialise(PacketReader reader) => ActorId = new ActorId(reader.ReadPackedLong());
}
