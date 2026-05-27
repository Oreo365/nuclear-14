using Content.Shared._Misfits.NPC;

namespace Content.Server.NPC.Components;

[RegisterComponent]
public sealed partial class RecruitedFollowerComponent : Component
{
    [DataField]
    public EntityUid Commander;

    [DataField]
    public string OriginalRootTask = string.Empty;

    [DataField]
    public FollowerOrderType Order = FollowerOrderType.Follow;

    public float NoPathAccumulator;

    [DataField]
    public float NoPathTimeoutSeconds = 15f;

    public bool WasAutoHeld;
}
