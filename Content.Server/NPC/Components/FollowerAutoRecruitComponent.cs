namespace Content.Server.NPC.Components;

[RegisterComponent]
public sealed partial class FollowerAutoRecruitComponent : Component
{
    [DataField]
    public EntityUid Commander;
}
