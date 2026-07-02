using JetBrains.Annotations;
using SR2MP.Packets.World;
using SR2MP.Shared.Managers;
using Starlight.Storage;

namespace SR2MP.Components.World;

[InjectIntoIL]
internal sealed class NetworkTornado : MonoBehaviour
{
    private const float SendInterval = 0.35f;
    private const float StaleTimeout = 30f;

    public int TornadoId;
    public bool IsAuthority;

    private float sendTimer = SendInterval;
    private float staleTimer;
    private Vector3 targetPosition;
    private bool hasTarget;

    [UsedImplicitly]
    public void Update()
    {
        if (IsAuthority)
        {
            if (!Main.Server.IsRunning)
                return;

            sendTimer -= UnityEngine.Time.deltaTime;
            if (sendTimer > 0f)
                return;

            sendTimer = SendInterval;
            Main.Server.SendToAll(new TornadoUpdatePacket
            {
                TornadoId = TornadoId,
                Position = transform.position
            });
        }
        else
        {
            // The client clone keeps its own random pathing; without updates
            // for too long, assume the host tornado is gone.
            staleTimer += UnityEngine.Time.deltaTime;
            if (staleTimer > StaleTimeout)
            {
                NetworkTornadoManager.Unregister(TornadoId);
                Destroy(gameObject);
            }
        }
    }

    [UsedImplicitly]
    public void LateUpdate()
    {
        if (IsAuthority || !hasTarget)
            return;

        // Written in LateUpdate so it wins over the tornado's own movement.
        transform.position = Vector3.Lerp(transform.position, targetPosition,
            UnityEngine.Time.deltaTime * 4f);
    }

    public void ReceivePosition(Vector3 position)
    {
        targetPosition = position;
        hasTarget = true;
        staleTimer = 0f;
    }

    [UsedImplicitly]
    public void OnDestroy()
    {
        NetworkTornadoManager.Unregister(TornadoId);

        if (IsAuthority && Main.Server.IsRunning)
            Main.Server.SendToAll(new TornadoDespawnPacket { TornadoId = TornadoId });
    }
}
