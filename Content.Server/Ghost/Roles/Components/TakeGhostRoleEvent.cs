using Robust.Shared.Player;

namespace Content.Server.Ghost.Roles.Components;

[ByRefEvent]
public record struct TakeGhostRoleEvent(ICommonSession Player)
{
    public bool Cancelled { get; set; }
    public bool TookRole { get; set; }
}
