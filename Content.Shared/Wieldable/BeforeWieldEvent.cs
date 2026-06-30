namespace Content.Shared.Wieldable;

public sealed class BeforeWieldEvent : CancellableEntityEventArgs
{
    public EntityUid User { get; }

    public BeforeWieldEvent(EntityUid user)
    {
        User = user;
    }
}
