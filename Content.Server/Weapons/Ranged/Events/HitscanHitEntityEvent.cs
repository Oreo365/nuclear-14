using Robust.Shared.GameObjects;

namespace Content.Server.Weapons.Ranged.Events;

[ByRefEvent]
public record struct HitscanHitEntityEvent(EntityUid Target);
