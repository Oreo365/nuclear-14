using Robust.Shared.GameStates;

namespace Content.Shared._Misfits.NPC.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FollowerCommanderComponent : Component
{
    [DataField, AutoNetworkedField]
    public int FollowerCount;

    public EntityUid? LastKnownGrid;

    public float GridTeleportAccumulator;

    public const float GridTeleportDelaySeconds = 3f;
}
