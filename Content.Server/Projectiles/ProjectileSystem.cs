using Content.Server.Damage.Systems;
using Content.Shared.Damage.Events;
using Content.Shared.Projectiles;
using Robust.Shared.Physics.Events;
using Robust.Shared.Utility;

namespace Content.Server.Projectiles;

public sealed class ProjectileSystem : SharedProjectileSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProjectileComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<EmbeddableProjectileComponent, DamageExamineEvent>(OnDamageExamine, after: [typeof(DamageOtherOnHitSystem)]);
    }

    private void OnStartCollide(EntityUid uid, ProjectileComponent component, ref StartCollideEvent args)
    {
        // This is so entities that shouldn't get a collision are ignored.
        if (args.OurFixtureId != ProjectileFixture || !args.OtherFixture.Hard
            || component.DamagedEntity || component is { Weapon: null, OnlyCollideWhenShot: true, })
            return;

        ProjectileCollide((uid, component, args.OurBody), args.OtherEntity);
    }

    private void OnDamageExamine(EntityUid uid, EmbeddableProjectileComponent component, ref DamageExamineEvent args)
    {
        if (!component.EmbedOnThrow)
            return;

        if (!args.Message.IsEmpty)
            args.Message.PushNewline();

        var isHarmful = TryComp<EmbedPassiveDamageComponent>(uid, out var passiveDamage) && passiveDamage.Damage.AnyPositive();
        var loc = isHarmful
            ? "damage-examine-embeddable-harmful"
            : "damage-examine-embeddable";

        var staminaCostMarkup = FormattedMessage.FromMarkupOrThrow(Loc.GetString(loc));
        args.Message.AddMessage(staminaCostMarkup);
    }
}
