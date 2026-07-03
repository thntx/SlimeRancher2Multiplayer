using System.Collections;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Components.Actor;
using SR2MP.Packets.Actor;

namespace SR2MP.Shared.Managers;

internal sealed partial class NetworkActorManager
{
    public readonly Dictionary<long, IdentifiableModel> Actors = new();
    public readonly Dictionary<int, IdentifiableType> ActorTypes = new();

    public static int GetPersistentID(IdentifiableType type)
        => GameContext.Instance.AutoSaveDirector._saveReferenceTranslation.GetPersistenceId(type);

    internal void Initialize(GameContext context)
    {
        ActorTypes.Clear();
        Actors.Clear();

        foreach (var type in context.AutoSaveDirector._saveReferenceTranslation._identifiableTypeLookup)
            ActorTypes.TryAdd(GetPersistentID(type.value), type.value);

        ActorTypes[-1] = null!;

        StartCoroutine(ZoneLoadingLoop());
        StartCoroutine(OwnershipMaintenanceLoop());
    }

    private const float ClaimRadius = 30f;
    private const float OwnerAbandonRadius = 60f;

    /// <summary>
    /// Periodically claims actors near the local player whose owner is gone
    /// or far away. Without this, ownership only moved on zone loads, so a
    /// slime next to a remote player kept "simulating" on a distant machine
    /// where its region was hibernated - no AI, no fleeing, no behaviours.
    /// </summary>
    private IEnumerator OwnershipMaintenanceLoop()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(4f);

            if (!Main.Server.IsRunning && !Main.Client.IsConnected)
                continue;

            var player = SceneContext.Instance?.player;
            if (!player)
                continue;

            var playerPosition = player!.transform.position;
            var snapshot = new List<IdentifiableModel?>(Actors.Values);
            var claims = 0;

            foreach (var actor in snapshot)
            {
                if (actor == null)
                    continue;

                try
                {
                    if (!actor.TryGetNetworkComponent(out var netActor))
                        continue;

                    if (netActor.LocallyOwned || !netActor.IsValid || netActor.IsDestroyed)
                        continue;

                    var actorPosition = netActor.transform.position;

                    if ((actorPosition - playerPosition).sqrMagnitude > ClaimRadius * ClaimRadius)
                        continue;

                    // Hysteresis: never steal from a player who is also close
                    // to the actor, so ownership doesn't ping-pong when two
                    // players stand together.
                    var ownerId = netActor.CurrentOwnerId;
                    if (!string.IsNullOrEmpty(ownerId) && ownerId != LocalID)
                    {
                        var owner = PlayerManager.GetPlayer(ownerId);
                        if (owner != null &&
                            (owner.Position - actorPosition).sqrMagnitude < OwnerAbandonRadius * OwnerAbandonRadius)
                            continue;
                    }

                    var actorId = netActor.ActorId;
                    if (actorId.Value == 0)
                        continue;

                    netActor.LocallyOwned = true;
                    netActor.CurrentOwnerId = LocalID;

                    Main.SendToAllOrServer(new ActorTransferPacket { ActorId = actorId, OwnerId = LocalID });
                    claims++;
                }
                catch (Exception ex)
                {
                    SrLogger.LogWarning($"Proximity ownership claim failed: {ex.Message}");
                }

                if (claims < 12)
                    continue;

                claims = 0;
                yield return null;
            }
        }
    }

    private IEnumerator ZoneLoadingLoop()
    {
        while (true)
        {
            yield return new WaitForSceneGroupLoad(false);
            yield return new WaitForSceneGroupLoad();

            if (!Main.Server.IsRunning && !Main.Client.IsConnected)
                continue;

            if (!SystemContext.Instance.SceneLoader.IsCurrentSceneGroupGameplay())
                continue;

            var gameModel = SceneContext.Instance?.GameModel;
            if (!gameModel)
                continue;

            var scene = SystemContext.Instance.SceneLoader.CurrentSceneGroup;

            // Everything below works on a snapshot and swallows per-actor
            // failures: instantiating actors mutates the identifiables
            // dictionary, and a single exception used to kill this loop for
            // the rest of the session (actors then never reloaded again).
            var snapshot = new List<IdentifiableModel>();

            try
            {
                foreach (var actor in gameModel!.identifiables)
                {
                    if (actor.value != null)
                        snapshot.Add(actor.value);
                }
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"Failed to snapshot identifiables on zone load: {ex}");
                continue;
            }

            foreach (var actor in snapshot)
            {
                try
                {
                    if (actor.ident.IsPlayer)
                        continue;

                    if (actor.TryCast<ActorModel>() == null)
                        continue;

                    var obj = actor.GetGameObject();
                    if (!obj)
                        continue;

                    Object.Destroy(obj);
                    Actors.Remove(actor.actorId.Value);
                }
                catch (Exception ex)
                {
                    SrLogger.LogWarning($"Zone reload: failed to clear an actor: {ex.Message}");
                }
            }

            foreach (var actor2 in snapshot)
            {
                try
                {
                    if (actor2.ident.IsPlayer)
                        continue;

                    var model = actor2.TryCast<ActorModel>();

                    if (model == null)
                        continue;

                    if (!model.ident.prefab)
                        continue;

                    if (actor2.sceneGroup != scene)
                        continue;

                    GameObject? obj;

                    HandlingPacket = true;
                    try
                    {
                        obj = InstantiationHelpers.InstantiateActorFromModel(model);
                    }
                    finally
                    {
                        HandlingPacket = false;
                    }

                    if (!obj)
                        continue;

                    var networkComponent = obj!.AddComponent<NetworkActor>();

                    networkComponent.previousPosition = model.lastPosition;
                    networkComponent.nextPosition     = model.lastPosition;
                    networkComponent.previousRotation = model.lastRotation;
                    networkComponent.nextRotation     = model.lastRotation;

                    Actors[model.actorId.Value] = model;
                }
                catch (Exception ex)
                {
                    SrLogger.LogWarning($"Zone reload: failed to respawn an actor: {ex.Message}");
                }
            }

            yield return TakeOwnershipOfNearby();
        }
    }

    private static bool ActorIDAlreadyInUse(ActorId id)
        => SceneContext.Instance?.GameModel?.TryGetIdentifiableModel(id, out _) ?? false;

    public static long GetHighestActorIdInRange(long min, long max)
    {
        var result = min;
        foreach (var actor in GameState.identifiables)
        {
            var id = actor.value.actorId.Value;
            if (id < min || id >= max)
                continue;
            if (id > result)
                result = id;
        }

        return result;
    }

    internal IEnumerator TakeOwnershipOfNearby(bool onlyUnowned = false)
    {
        const int max = 12;

        var player = SceneContext.Instance.player;
        var bounds = new Bounds(player.transform.position, new Vector3(600, 1250, 600));

        // Snapshot: actor packets mutate the dictionary while this yields.
        var snapshot = new List<IdentifiableModel?>(Actors.Values);

        var i = 0;
        foreach (var actor in snapshot)
        {
            if (actor == null)
                continue;

            try
            {
                if (!bounds.Contains(actor.lastPosition))
                    continue;

                if (!actor.TryGetNetworkComponent(out var netActor))
                    continue;

                // todo: only if you wanna claim actors that are currently unowned,
                // could hook this up somewhere in the future
                if (onlyUnowned)
                {
                    if (!string.IsNullOrEmpty(netActor.CurrentOwnerId))
                        continue;
                }

                netActor.LocallyOwned = true;
                netActor.CurrentOwnerId = LocalID;

                var actorId = netActor.ActorId;
                if (actorId.Value == 0)
                    continue;

                var packet = new ActorTransferPacket { ActorId = actorId, OwnerId = LocalID };
                Main.SendToAllOrServer(packet);
                i++;
            }
            catch (Exception ex)
            {
                SrLogger.LogWarning($"Ownership claim failed for an actor: {ex.Message}");
            }

            if (i <= max)
                continue;

            yield return null;
            i = 0;
        }
    }
}