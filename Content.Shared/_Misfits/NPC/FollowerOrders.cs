using Robust.Shared.Serialization;

namespace Content.Shared._Misfits.NPC;

[Serializable, NetSerializable]
public enum FollowerOrderType : byte
{
    Follow,
    Passive,
    HoldPosition,
    Neutral,
}

[Serializable, NetSerializable]
public sealed class IssueFollowerOrderMessage(FollowerOrderType order) : EntityEventArgs
{
    public readonly FollowerOrderType Order = order;
}
