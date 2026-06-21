using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Timing; // #Misfits Add

namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised on the client to indicate it'd like to shoot.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestShootEvent : EntityEventArgs
{
    public NetEntity Gun;
    public NetCoordinates Coordinates;
    public NetEntity? Target;
    public List<int>? Shot;

    // #Misfits Add — last confirmed client tick at the time of the shot; used by
    // ServerMisfitsLagCompensationSystem to apply a range-tolerance margin on the server.
    public GameTick? LastRealTick;
}
