using Content.Server._Misfits.Pets;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Traits;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

// #Misfits Add - TraitSpawnEntity: spawns a separate entity at the player's position when the trait activates.
// Used for pet companion traits. If the spawner prototype contains a GhostRoleMobSpawner with a valid
// prototype, the companion mob is spawned immediately as an AI NPC (no ghost player required).

namespace Content.Server._Misfits.Traits;

/// <summary>
///     Spawns one or more entities at the player's location when the trait is applied.
///     If a spawned entity has a <c>GhostRoleMobSpawnerComponent</c> with a valid prototype,
///     the mob is spawned immediately as an AI NPC and the spawner is deleted.
/// </summary>
[UsedImplicitly]
public sealed partial class TraitSpawnEntity : TraitFunction
{
    /// <summary>
    ///     Prototype IDs to spawn at the player's coordinates.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Prototypes { get; private set; } = new();

    public override void OnPlayerSpawn(
        EntityUid uid,
        IComponentFactory factory,
        IEntityManager entityManager,
        ISerializationManager serializationManager)
    {
        // Resolve the player's current map coordinates for spawning
        var xform = entityManager.GetComponent<TransformComponent>(uid);
        var coords = xform.Coordinates;

        foreach (var proto in Prototypes)
        {
            var spawner = entityManager.SpawnEntity(proto, coords);

            if (entityManager.TryGetComponent<GhostRoleMobSpawnerComponent>(spawner, out var mobSpawner)
                && mobSpawner.Prototype is { } mobProto)
            {
                // Spawn the companion immediately as an AI NPC; no ghost player required.
                entityManager.SpawnEntity(mobProto, coords);
                entityManager.DeleteEntity(spawner);
            }
            else if (entityManager.HasComponent<GhostRoleMobSpawnerComponent>(spawner))
            {
                // Spawner exists but prototype is unset: fall back to ghost-role flow.
                var ownerComp = entityManager.EnsureComponent<MisfitsPetSpawnerOwnerComponent>(spawner);
                ownerComp.Owner = uid;
            }
        }
    }
}
