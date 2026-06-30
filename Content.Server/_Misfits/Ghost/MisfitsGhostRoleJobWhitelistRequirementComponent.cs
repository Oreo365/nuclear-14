using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._Misfits.Ghost;

/// <summary>
/// Blocks ghost-role takeover unless the requester has at least one listed job whitelist.
/// </summary>
[RegisterComponent]
public sealed partial class MisfitsGhostRoleJobWhitelistRequirementComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<JobPrototype>> Jobs = new();
}

