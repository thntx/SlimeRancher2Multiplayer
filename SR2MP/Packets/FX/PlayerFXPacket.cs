using SR2MP.Packets.Utils;

namespace SR2MP.Packets.FX;

internal sealed class PlayerFXPacket : IPacket
{
    internal enum PlayerFXType : byte
    {
        None,
        VacReject,
        VacHold,
        VacAccept,
        VacShoot,
        VacShootEmpty,
        VacSlotChange,
        VacRunning,
        VacRunningStart,
        VacRunningEnd,
        VacShootSound,
        WaterSplash
    }

    public PlayerFXType FX;
    public Vector3 Position;
    public string Player;

    /// <summary>
    /// Name of the FX prefab to replay. Only used for <see cref="PlayerFXType.WaterSplash"/>,
    /// whose particle differs per water volume.
    /// </summary>
    public string FXName;

    public PacketType Type => PacketType.PlayerFX;
    public PacketReliability Reliability => PacketReliability.Unreliable;
    public NetworkChannel Channel => NetworkChannel.FX;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteEnum(FX);

        if (FX == PlayerFXType.WaterSplash)
        {
            writer.WriteVector3(Position);
            writer.WriteString(FXName);
        }
        else if (!IsPlayerSoundDictionary[FX])
        {
            writer.WriteVector3(Position);
        }
        else
        {
            writer.WriteStringWithoutSize(Player);
        }
    }

    public void Deserialise(PacketReader reader)
    {
        FX = reader.ReadEnum<PlayerFXType>();

        if (FX == PlayerFXType.WaterSplash)
        {
            Position = reader.ReadVector3();
            FXName = reader.ReadPooledString()!;
        }
        else if (!IsPlayerSoundDictionary[FX])
        {
            Position = reader.ReadVector3();
        }
        else
        {
            Player = reader.ReadPooledStringOfSize(16)!;
        }
    }
}