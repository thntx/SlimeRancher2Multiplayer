using Il2CppMonomiPark.SlimeRancher.Drone;
using JetBrains.Annotations;
using SR2MP.Packets.Actor;
using Starlight.Storage;

namespace SR2MP.Components.Actor;

/// <summary>
/// Mirrors a ranch drone between peers. The host simulates the drone (its
/// brain is disabled on clients) and broadcasts its position; clients smooth
/// towards it.
/// </summary>
[InjectIntoIL]
internal sealed class NetworkRanchDrone : MonoBehaviour
{
    private const float SendInterval = 0.34f;

    private static readonly Dictionary<long, NetworkRanchDrone> Drones = new();

    private RanchDrone? drone;
    private Rigidbody? droneRigidbody;
    private long stationId;
    private float sendTimer = SendInterval;
    private Vector3 targetPosition;
    private float targetYaw;
    private bool hasTarget;
    private bool frozen;

    public static NetworkRanchDrone? Get(long stationId)
        => Drones.TryGetValue(stationId, out var networkDrone) && networkDrone ? networkDrone : null;

    [UsedImplicitly]
    public void Start()
    {
        drone = GetComponent<RanchDrone>();
        droneRigidbody = GetComponent<Rigidbody>();
    }

    [UsedImplicitly]
    public void Update()
    {
        if (!TryResolveStation())
            return;

        if (Main.Server.IsRunning)
        {
            sendTimer -= UnityEngine.Time.deltaTime;
            if (sendTimer > 0f)
                return;

            sendTimer = SendInterval;
            Main.Server.SendToAll(new DroneUpdatePacket
            {
                StationId = stationId,
                Position = transform.position,
                Yaw = transform.eulerAngles.y
            });
        }
        else if (Main.Client.IsConnected && !frozen)
        {
            // The client's own drone brain is disabled; keep physics from
            // fighting the mirrored positions.
            if (droneRigidbody)
                droneRigidbody!.constraints = RigidbodyConstraints.FreezeAll;
            frozen = true;
        }
    }

    [UsedImplicitly]
    public void LateUpdate()
    {
        if (Main.Server.IsRunning || !hasTarget)
            return;

        var blend = UnityEngine.Time.deltaTime * 5f;
        transform.position = Vector3.Lerp(transform.position, targetPosition, blend);

        var euler = transform.eulerAngles;
        euler.y = Mathf.LerpAngle(euler.y, targetYaw, blend);
        transform.eulerAngles = euler;
    }

    public void ReceivePosition(Vector3 position, float yaw)
    {
        targetPosition = position;
        targetYaw = yaw;
        hasTarget = true;
    }

    private bool TryResolveStation()
    {
        if (stationId != 0)
            return true;

        var stationModel = drone?._stationModel;
        if (stationModel == null)
            return false;

        stationId = stationModel.actorId.Value;
        if (stationId == 0)
            return false;

        Drones[stationId] = this;
        return true;
    }

    [UsedImplicitly]
    public void OnDestroy()
    {
        if (stationId != 0 && Drones.TryGetValue(stationId, out var registered) && registered == this)
            Drones.Remove(stationId);
    }
}
