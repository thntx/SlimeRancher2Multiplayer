using System.Net;
using SR2MP.Components.Player;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.FX;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.FX;

[PacketHandler((byte)PacketType.PlayerFX)]
internal sealed class PlayerFXHandler : BasePacketHandler<PlayerFXPacket>
{
    protected override bool Handle(PlayerFXPacket packet, IPEndPoint? _)
    {
        HandlingPacket = true;

        try
        {
            if (packet.FX == PlayerFXType.WaterSplash)
            {
                var splashPrefab = FXManager.GetSplashFX(packet.FXName);
                if (splashPrefab)
                    FXHelpers.SpawnAndPlayFX(splashPrefab, packet.Position, Quaternion.identity);
            }
            else if (!IsPlayerSoundDictionary[packet.FX])
            {
                var fxPrefab = FXManager.PlayerFXMap[packet.FX];
                FXHelpers.SpawnAndPlayFX(fxPrefab, packet.Position, Quaternion.identity);
            }
            else
            {
                SetRemoteVacTrail(packet);
                var cue = FXManager.PlayerAudioCueMap[packet.FX];

                if (ShouldPlayerSoundBeTransientDictionary[packet.FX])
                {
                    RemoteFXManager.PlayTransientAudio(cue, PlayerObjects[packet.Player].transform.position,
                        PlayerSoundVolumeDictionary[packet.FX]);
                }
                else
                {
                    var playerAudio = PlayerObjects[packet.Player].GetComponent<SECTR_PointSource>();
                    playerAudio.Cue = cue;
                    playerAudio.Loop = DoesPlayerSoundLoopDictionary[packet.FX];
                    playerAudio.instance.Volume = PlayerSoundVolumeDictionary[packet.FX];
                    playerAudio.Play();
                }
            }
        }
        catch { /* Errors here are typically non-serious related to scene loading */ }
        finally
        {
            HandlingPacket = false;
        }

        return true;
    }

    private static void SetRemoteVacTrail(PlayerFXPacket packet)
    {
        if (packet.FX is not (PlayerFXType.VacRunningStart or PlayerFXType.VacRunningEnd))
            return;

        if (!PlayerObjects.TryGetValue(packet.Player, out var playerObject) || !playerObject)
            return;

        playerObject.GetComponent<NetworkPlayer>()?
            .SetVacTrailActive(packet.FX == PlayerFXType.VacRunningStart);
    }
}