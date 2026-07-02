using Il2CppMonomiPark.SlimeRancher.Weather.Activity;
using SR2MP.Components.World;
using SR2MP.Packets.World;
using SR2MP.Shared.Utils;

namespace SR2MP.Shared.Managers;

/// <summary>
/// Tracks weather tornados so the host's tornados are mirrored on clients.
/// Clients never spawn their own (SpawnActivity is suppressed for them).
/// </summary>
internal static class NetworkTornadoManager
{
    private static readonly Dictionary<int, NetworkTornado> Tornados = new();

    private static int nextTornadoId;

    public static int RegisterHostTornado(GameObject tornado)
    {
        var id = ++nextTornadoId;
        var network = tornado.AddComponent<NetworkTornado>();
        network.TornadoId = id;
        network.IsAuthority = true;
        Tornados[id] = network;
        return id;
    }

    public static void RegisterClientTornado(int id, GameObject tornado)
    {
        var network = tornado.AddComponent<NetworkTornado>();
        network.TornadoId = id;
        network.IsAuthority = false;
        Tornados[id] = network;
    }

    public static void Unregister(int id) => Tornados.Remove(id);

    public static NetworkTornado? Get(int id)
        => Tornados.TryGetValue(id, out var tornado) && tornado ? tornado : null;

    public static void Despawn(int id)
    {
        var tornado = Get(id);
        Tornados.Remove(id);

        if (tornado)
            Object.Destroy(tornado!.gameObject);
    }

    public static SpawnTornadoActivity? FindActivity(ushort nameHash)
    {
        foreach (var activity in Resources.FindObjectsOfTypeAll<SpawnTornadoActivity>())
        {
            if (activity.name.Hash16() == nameHash)
                return activity;
        }

        return null;
    }
}
