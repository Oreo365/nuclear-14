using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Players.JobWhitelist;

namespace Content.Server._Misfits.Ghost;

public sealed class MisfitsGhostRoleJobWhitelistRequirementSystem : EntitySystem
{
    [Dependency] private readonly JobWhitelistManager _jobWhitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MisfitsGhostRoleJobWhitelistRequirementComponent, TakeGhostRoleEvent>(
            OnTakeGhostRole,
            before: [typeof(GhostRoleSystem)]);
    }

    private void OnTakeGhostRole(Entity<MisfitsGhostRoleJobWhitelistRequirementComponent> ent, ref TakeGhostRoleEvent args)
    {
        if (ent.Comp.Jobs.Count == 0)
            return;

        foreach (var job in ent.Comp.Jobs)
        {
            if (_jobWhitelist.IsAllowed(args.Player, job))
                return;
        }

        args.Cancelled = true;
        args.TookRole = false;
    }
}

