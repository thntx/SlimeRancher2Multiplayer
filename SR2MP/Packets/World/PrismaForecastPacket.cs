using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

/// <summary>
/// Snapshot of the Gray Labyrinth disruption forecast. The host rolls the
/// forecast; clients replay it so both sides disrupt the same areas at the
/// same world times.
/// </summary>
internal sealed class PrismaForecastPacket : IPacket
{
    internal sealed class PatternData : INetObject
    {
        public double EndTime;
        public List<double> StartTimes;
        public List<byte> Levels;

        public void Serialise(PacketWriter writer)
        {
            writer.WriteDouble(EndTime);
            writer.WriteList(StartTimes, PacketWriterDels.Double);
            writer.WriteList(Levels, PacketWriterDels.Byte);
        }

        public void Deserialise(PacketReader reader)
        {
            EndTime = reader.ReadDouble();
            StartTimes = reader.ReadList(PacketReaderDels.Double)!;
            Levels = reader.ReadList(PacketReaderDels.Byte)!;
        }
    }

    internal sealed class AreaData : INetObject
    {
        public ushort AreaNameHash;
        public bool ForecastUnlocked;
        public List<PatternData> Patterns;

        public void Serialise(PacketWriter writer)
        {
            writer.WriteUShort(AreaNameHash);
            writer.WritePackedBool(ForecastUnlocked);
            writer.WriteList(Patterns, PacketWriterDels.NetObject<PatternData>.Writer);
        }

        public void Deserialise(PacketReader reader)
        {
            AreaNameHash = reader.ReadUShort();
            ForecastUnlocked = reader.ReadPackedBool();
            Patterns = reader.ReadList(PacketReaderDels.NetObject<PatternData>.Reader)!;
        }
    }

    public List<AreaData> Areas;

    public PacketType Type => PacketType.PrismaForecast;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer) => writer.WriteList(Areas, PacketWriterDels.NetObject<AreaData>.Writer);

    public void Deserialise(PacketReader reader) => Areas = reader.ReadList(PacketReaderDels.NetObject<AreaData>.Reader)!;
}
