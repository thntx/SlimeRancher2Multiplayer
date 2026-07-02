using Il2CppMonomiPark.SlimeRancher.DataModel;

namespace SR2MP.Shared.Managers;

/// <summary>
/// Tracks resource node spawners by their persistent id and keeps harvest
/// origins so only the machine that initiated a harvest spawns the loot.
/// </summary>
internal static class NetworkResourceNodeManager
{
    private static readonly Dictionary<string, ResourceNodeSpawner> Spawners = new();

    // Spawner ids whose current harvest was applied from the network. Their
    // nodes must not spawn resources locally; the origin syncs the actors.
    private static readonly HashSet<string> RemoteHarvests = new();

    public static void Register(ResourceNodeSpawner spawner)
    {
        var id = spawner.Id;
        if (!string.IsNullOrEmpty(id))
            Spawners[id] = spawner;
    }

    public static void Clear()
    {
        Spawners.Clear();
        RemoteHarvests.Clear();
    }

    public static ResourceNodeSpawner? GetSpawner(string id)
    {
        if (Spawners.TryGetValue(id, out var spawner) && spawner)
            return spawner;

        return null;
    }

    public static ResourceNodeSpawnerModel? GetModel(string id)
        => SceneContext.Instance?.GameModel?.resourceNodeSpawnerModels?.TryGetValue(id, out var model) == true
            ? model
            : null;

    public static void MarkRemoteHarvest(string id) => RemoteHarvests.Add(id);

    public static void ClearRemoteHarvest(string id) => RemoteHarvests.Remove(id);

    public static bool IsRemoteHarvest(string? id) => id != null && RemoteHarvests.Contains(id);

    /// <summary>
    /// Resolves the spawner id a node belongs to, or null when unknown.
    /// </summary>
    public static string? GetSpawnerId(ResourceNode node)
    {
        var spawner = node.GetComponentInParent<ResourceNodeSpawner>(true);
        if (spawner)
            return spawner!.Id;

        var model = node._model;
        if (model == null)
            return null;

        foreach (var entry in GameState.resourceNodeSpawnerModels)
        {
            if (entry.Value?.Pointer == model.Pointer)
                return entry.Key;
        }

        return null;
    }

    public static int GetDefinitionIndex(ResourceNodeDefinition? definition)
        => definition ? GameState.resourceNodeDefinitions.IndexOf(definition) : -1;

    public static ResourceNodeDefinition? GetDefinition(int index)
    {
        var definitions = GameState.resourceNodeDefinitions;
        if (index < 0 || index >= definitions.Count)
            return null;

        return definitions[index];
    }

    /// <summary>
    /// Applies a networked node snapshot to the local spawner and model.
    /// Callers must have <see cref="GlobalVariables.HandlingPacket"/> set.
    /// </summary>
    public static void ApplyNodeData(
        string spawnerId,
        int definitionIndex,
        int variantIndex,
        double despawnAtWorldTime,
        List<int> resourceIds,
        ResourceNode.NodeState state)
    {
        var definition = GetDefinition(definitionIndex);

        if (!definition || state == ResourceNode.NodeState.NONE)
        {
            ApplyNodeCleared(spawnerId);
            return;
        }

        var model = GetModel(spawnerId) ?? GameState.InitializeResourceNodeSpawnerModel(spawnerId);
        if (model == null)
        {
            SrLogger.LogWarning($"Could not resolve resource node spawner model '{spawnerId}'");
            return;
        }

        var spawner = GetSpawner(spawnerId);
        if (spawner != null)
        {
            var currentDefinition = model.resourceNodeDefinition;
            var needsRespawn = !spawner._attachedResourceNode
                               || !currentDefinition
                               || currentDefinition!.Pointer != definition!.Pointer;

            if (needsRespawn)
            {
                if (spawner._attachedResourceNode)
                {
                    Object.Destroy(spawner._attachedResourceNode.gameObject);
                    spawner._attachedResourceNode = null;
                }

                spawner.SpawnNode(definition);
            }
        }

        model.resourceNodeDefinition = definition;
        model.resourceNodeVariantIndex = variantIndex;
        model.despawnAtWorldTime = despawnAtWorldTime;

        if (resourceIds != null)
        {
            var resources = new CppCollections.List<IdentifiableType>();
            foreach (var resourceId in resourceIds)
            {
                if (ActorManager.ActorTypes.TryGetValue(resourceId, out var resourceType) && resourceType)
                    resources.Add(resourceType);
            }

            model.resourcesToSpawn = resources;
        }

        ApplyNodeState(spawnerId, state);
    }

    /// <summary>
    /// Applies a networked node state to the local node instance, or to the
    /// model when the zone is not loaded. Callers must have
    /// <see cref="GlobalVariables.HandlingPacket"/> set.
    /// </summary>
    public static void ApplyNodeState(string spawnerId, ResourceNode.NodeState state)
    {
        var node = GetSpawner(spawnerId)?._attachedResourceNode;
        var model = GetModel(spawnerId);

        if (state == ResourceNode.NodeState.HARVESTING)
        {
            // If this side is already harvesting on its own, keep the local
            // origin; suppressing both sides would spawn no loot at all.
            if (model?.nodeState != ResourceNode.NodeState.HARVESTING)
                MarkRemoteHarvest(spawnerId);
        }
        else if (state == ResourceNode.NodeState.READY)
        {
            ClearRemoteHarvest(spawnerId);
        }

        if (node != null && model?.nodeState != state)
        {
            switch (state)
            {
                case ResourceNode.NodeState.READY:
                    node.SetStateReady();
                    break;
                case ResourceNode.NodeState.HARVESTING:
                    node.SetStateHarvesting();
                    break;
                case ResourceNode.NodeState.HARVESTED:
                    node.SetStateEmpty();
                    break;
            }
        }

        if (model != null)
            model.nodeState = state;
    }

    /// <summary>
    /// Removes the node from a spawner without playing the despawn sequence.
    /// Callers must have <see cref="GlobalVariables.HandlingPacket"/> set.
    /// </summary>
    public static void ApplyNodeCleared(string spawnerId)
    {
        ClearRemoteHarvest(spawnerId);

        var spawner = GetSpawner(spawnerId);
        if (spawner != null && spawner._attachedResourceNode)
        {
            Object.Destroy(spawner._attachedResourceNode.gameObject);
            spawner._attachedResourceNode = null;
        }

        var model = GetModel(spawnerId);
        if (model == null)
            return;

        model.nodeState = ResourceNode.NodeState.NONE;
        model.resourceNodeDefinition = null;
        model.resourcesToSpawn?.Clear();
    }
}
