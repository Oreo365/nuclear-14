using Content.Server.Chat.Systems;
using Content.Server.CombatMode.Disarm;
using Content.Server._Misfits.Movement;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Actions.Events;
using Content.Shared.Administration.Components;
using Content.Shared.CombatMode;
using Content.Shared.Contests;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Effects;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Speech.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Linq;
using System.Numerics;
using Content.Shared.Chat;

namespace Content.Server.Weapons.Melee;

public sealed class MeleeWeaponSystem : SharedMeleeWeaponSystem
{
    // #Misfits Change /Fix/: give empty-hand shoves a short, visible displacement instead of only stamina feedback.
    private const float ShoveImpulse = 6f;

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DamageExamineSystem _damageExamine = default!;
    [Dependency] private readonly ServerMisfitsLagCompensationSystem _lag = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly ContestsSystem _contests = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MeleeSpeechComponent, MeleeHitEvent>(OnSpeechHit);
        SubscribeLocalEvent<MeleeWeaponComponent, DamageExamineEvent>(OnMeleeExamineDamage, after: [typeof(GunSystem)]);
    }

    private void OnMeleeExamineDamage(EntityUid uid, MeleeWeaponComponent component, ref DamageExamineEvent args)
    {
        if (component.Hidden)
            return;

        var damageSpec = GetDamage(uid, args.User, component);
        if (damageSpec.Empty)
            return;

        if (!component.DisableClick)
            _damageExamine.AddDamageExamine(args.Message, damageSpec, Loc.GetString("damage-melee"));

        if (!component.DisableHeavy)
        {
            if (damageSpec * component.HeavyDamageBaseModifier != damageSpec)
                _damageExamine.AddDamageExamine(args.Message, damageSpec * component.HeavyDamageBaseModifier, Loc.GetString("damage-melee-heavy"));

            if (component.HeavyStaminaCost != 0)
            {
                var staminaCostMarkup = FormattedMessage.FromMarkupOrThrow(
                    Loc.GetString("damage-stamina-cost",
                    ("type", Loc.GetString("damage-melee-heavy")), ("cost", Math.Round(component.HeavyStaminaCost, 2).ToString("0.##"))));
                args.Message.PushNewline();
                args.Message.AddMessage(staminaCostMarkup);
            }
        }
    }

    protected override bool ArcRaySuccessful(EntityUid targetUid, Vector2 position, Angle angle, Angle arcWidth, float range, MapId mapId,
        EntityUid ignore, ICommonSession? session)
    {
        // Originally the client didn't predict damage effects so you'd intuit some level of how far
        // in the future you'd need to predict, but then there was a lot of complaining like "why would you add artifical delay" as if ping is a choice.
        // Now damage effects are predicted but for wide attacks it differs significantly from client and server so your game could be lying to you on hits.
        // This isn't fair in the slightest because it makes ping a huge advantage and this would be a hidden system.
        // Now the client tells us what they hit and we validate if it's plausible.

        // Even if the client is sending entities they shouldn't be able to hit:
        // A) Wide-damage is split anyway
        // B) We run the same validation we do for click attacks.

        EntityCoordinates targetCoords;
        Angle targetLocalAngle;

        if (session != null)
        {
            (targetCoords, targetLocalAngle) = _lag.GetCoordinatesAngle(targetUid, session);
            if (!Interaction.InRangeUnobstructed(ignore, targetUid, targetCoords, targetLocalAngle, range + 0.1f))
                return false;
        }
        else
        {
            var xform = Transform(targetUid);
            targetCoords = xform.Coordinates;
            targetLocalAngle = xform.LocalRotation;
            if (!Interaction.InRangeUnobstructed(ignore, targetUid, range + 0.1f))
                return false;
        }

        var targetMapPos = TransformSystem.ToMapCoordinates(targetCoords);
        if (targetMapPos.MapId == mapId)
        {
            var toTarget = targetMapPos.Position - position;
            if (toTarget.LengthSquared() > 0.001f)
            {
                var diff = Angle.ShortestDistance(angle, toTarget.ToWorldAngle());
                if (Math.Abs((double) diff) > (double) arcWidth / 2.0 + 0.1)
                    return false;
            }
        }

        return true;
    }

    protected override bool DoDisarm(EntityUid user, DisarmAttackEvent ev, EntityUid meleeUid, MeleeWeaponComponent component, ICommonSession? session)
    {
        if (!base.DoDisarm(user, ev, meleeUid, component, session))
            return false;

        if (!TryComp<CombatModeComponent>(user, out var combatMode) ||
            combatMode.CanDisarm != true)
        {
            return false;
        }

        var target = GetEntity(ev.Target!.Value);

        if (_mobState.IsIncapacitated(target))
        {
            return false;
        }

        if (!TryComp<HandsComponent>(target, out var targetHandsComponent))
        {
            if (!TryComp<StatusEffectsComponent>(target, out var status) || !status.AllowedEffects.Contains("KnockedDown"))
                return false;
        }

        if (!InRange(user, target, component.Range, session))
        {
            return false;
        }

        EntityUid? inTargetHand = null;

        if (targetHandsComponent?.ActiveHand is { IsEmpty: false })
        {
            inTargetHand = targetHandsComponent.ActiveHand.HeldEntity!.Value;
        }

        Interaction.DoContactInteraction(user, target);

        var attemptEvent = new DisarmAttemptEvent(target, user, inTargetHand);

        if (inTargetHand != null)
        {
            RaiseLocalEvent(inTargetHand.Value, attemptEvent);
        }

        RaiseLocalEvent(target, attemptEvent);

        if (attemptEvent.Cancelled)
            return false;

        var chance = CalculateDisarmChance(user, target, inTargetHand, combatMode);
        if (!_random.Prob(chance))
        {
            // Don't play a sound as the swing is already predicted.
            // Also don't play popups because most disarms will miss.
            return false;
        }

        var staminaDamage = (TryComp<ShovingComponent>(user, out var shoving) ? shoving.StaminaDamage : ShovingComponent.DefaultStaminaDamage)
            * Math.Clamp(chance, 0f, 1f);

        var eventArgs = new DisarmedEvent { Target = target, Source = user, PushProbability = chance, StaminaDamage = staminaDamage };
        RaiseLocalEvent(target, eventArgs);

        // #Misfits Change /Fix/: if nothing else handled an empty-hand disarm, convert it into an actual shove impulse.
        if (!eventArgs.Handled && inTargetHand == null)
            eventArgs.Handled = TryShoveTarget(user, target);

        if (!eventArgs.Handled)
            return false;

        var emoteKey = inTargetHand == null ? "disarm-action-shove-emote" : "disarm-action-emote";
        var emoteMessage = Loc.GetString(emoteKey, ("targetName", Identity.Entity(target, EntityManager)));
        _chat.TrySendInGameICMessage(user, emoteMessage, InGameICChatType.Emote, ChatTransmitRange.Normal, ignoreActionBlocker: true);

        _audio.PlayPvs(combatMode.DisarmSuccessSound, user, AudioParams.Default.WithVariation(0.025f).WithVolume(5f));
        AdminLogger.Add(LogType.DisarmedAction, $"{ToPrettyString(user):user} used disarm on {ToPrettyString(target):target}");

        return true;
    }

    protected override bool InRange(EntityUid user, EntityUid target, float range, ICommonSession? session)
    {
        EntityCoordinates targetCoordinates;
        Angle targetLocalAngle;

        if (session is { } pSession)
        {
            (targetCoordinates, targetLocalAngle) = _lag.GetCoordinatesAngle(target, pSession);
            return Interaction.InRangeUnobstructed(user, target, targetCoordinates, targetLocalAngle, range);
        }

        return Interaction.InRangeUnobstructed(user, target, range);
    }

    protected override void DoDamageEffect(List<EntityUid> targets, EntityUid? user, TransformComponent targetXform)
    {
        var filter = Filter.Pvs(targetXform.Coordinates, entityMan: EntityManager).RemoveWhereAttachedEntity(o => o == user);
        _color.RaiseEffect(Color.Red, targets, filter);
    }

    private bool TryShoveTarget(EntityUid user, EntityUid target)
    {
        if (!TryComp<PhysicsComponent>(target, out var targetPhysics))
            return false;

        if ((targetPhysics.BodyType & (BodyType.Dynamic | BodyType.KinematicController)) == 0)
            return false;

        var shoveDirection = _transform.GetWorldPosition(target) - _transform.GetWorldPosition(user);

        if (shoveDirection.LengthSquared() <= 0.001f)
            shoveDirection = Transform(user).LocalRotation.ToWorldVec();

        // #Misfits Change /Fix/: scale impulse by target mass so heavy mobs still move perceptibly on a valid shove.
        _physics.ApplyLinearImpulse(target, shoveDirection.Normalized() * (ShoveImpulse * targetPhysics.Mass), body: targetPhysics);
        return true;
    }

    private float CalculateDisarmChance(EntityUid disarmer, EntityUid disarmed, EntityUid? inTargetHand, CombatModeComponent disarmerComp)
    {
        if (HasComp<DisarmProneComponent>(disarmer))
            return 1.0f;

        if (HasComp<DisarmProneComponent>(disarmed))
            return 0.0f;

        var chance = 1 - disarmerComp.BaseDisarmFailChance;

        if (inTargetHand != null && TryComp<DisarmMalusComponent>(inTargetHand, out var malus))
            chance -= malus.Malus;

        if (TryComp<ShovingComponent>(disarmer, out var shoving))
            chance += shoving.DisarmBonus;

        return Math.Clamp(chance
                        * _contests.MassContest(disarmer, disarmed, false, 2f)
                        * _contests.StaminaContest(disarmer, disarmed, false, 0.5f)
                        * _contests.HealthContest(disarmer, disarmed, false, 1f),
                        0f, 1f);
    }

    public override void DoLunge(EntityUid user, EntityUid weapon, Angle angle, Vector2 localPos, string? animation, bool predicted = true)
    {
        Filter filter;

        if (predicted)
        {
            filter = Filter.PvsExcept(user, entityManager: EntityManager);
        }
        else
        {
            filter = Filter.Pvs(user, entityManager: EntityManager);
        }

        RaiseNetworkEvent(new MeleeLungeEvent(GetNetEntity(user), GetNetEntity(weapon), angle, localPos, animation), filter);
    }

    private void OnSpeechHit(EntityUid owner, MeleeSpeechComponent comp, MeleeHitEvent args)
    {
        if (!args.IsHit ||
        !args.HitEntities.Any())
        {
            return;
        }

        if (comp.Battlecry != null)//If the battlecry is set to empty, doesn't speak
        {
            _chat.TrySendInGameICMessage(args.User, comp.Battlecry, InGameICChatType.Speak, true, true, checkRadioPrefix: false);  //Speech that isn't sent to chat or adminlogs
        }

    }
}
