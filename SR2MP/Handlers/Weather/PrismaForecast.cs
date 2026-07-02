using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Weather;

[PacketHandler((byte)PacketType.PrismaForecast, HandlerType.Client)]
internal sealed class PrismaForecastHandler : BasePacketHandler<PrismaForecastPacket>
{
    protected override bool Handle(PrismaForecastPacket packet, IPEndPoint? _)
    {
        HandlingPacket = true;

        try
        {
            NetworkPrismaManager.Apply(packet);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to apply prisma forecast: {ex}");
        }
        finally
        {
            HandlingPacket = false;
        }

        return false;
    }
}
