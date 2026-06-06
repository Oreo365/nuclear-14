// #Misfits Change
using Content.Server._Misfits.GhoulReversal;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Ghoul;
using Content.Server.Humanoid;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Server.Player;
using Robust.Shared.Enums;

namespace Content.Server._Misfits.GhoulReversal;

/// <summary>
/// When a humanoid with GhoulifyOnRadiationDeathComponent dies to radiation damage,
/// they transform into the Ghoul player species and are revived at low health,
/// instead of fully dying. The Promethine chemistry reagent can reverse this
/// within the first 12 real hours.
/// </summary>
public sealed class GhoulifyOnRadiationDeathSystem : EntitySystem
{
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhoulifyOnRadiationDeathComponent, MobStateChangedEvent>(OnMobStateDeath);
    }

    private void OnMobStateDeath(EntityUid uid, GhoulifyOnRadiationDeathComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        // Must be a humanoid player character
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var appearance))
            return;

        // Don't re-ghoulify someone already a ghoul or super mutant
        if (appearance.Species == "Ghoul" || appearance.Species == "GhoulGlowing" || appearance.Species == "SuperMutant")
            return;

        // Check that a meaningful amount of radiation damage was accumulated
        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return;

        var radDamage = 0f;
        if (damageable.Damage.DamageDict.TryGetValue("Radiation", out var rad))
            radDamage = rad.Float();

        if (radDamage < component.MinimumRadiationDamage)
            return;

        // --- Transform into Ghoul player species ---
        _humanoid.SetSpecies(uid, component.GhoulSpecies);

        // #Misfits Fix - Revive into soft crit instead of full heal.
        // Heal all damage first to leave Dead state, then re-apply enough damage
        // to land exactly at the Critical threshold (soft crit).
        if (TryComp<MobThresholdsComponent>(uid, out var thresholds))
        {
            _mobThreshold.SetAllowRevives(uid, true, thresholds);
            _damageable.SetAllDamage(uid, damageable, FixedPoint2.Zero);

            // Read the crit threshold; fall back to 100 if not defined
            if (!_mobThreshold.TryGetThresholdForState(uid, MobState.Critical, out var critThreshold, thresholds))
                critThreshold = FixedPoint2.New(100);

            // Apply airloss damage equal to crit threshold so the player wakes in soft crit
            // Changed from blunt to prevent Bloody Mess characters from getting all their limbs gibbed
            var critDamage = new DamageSpecifier();
            critDamage.DamageDict["Asphyxiation"] = critThreshold.Value;
            _damageable.TryChangeDamage(uid, critDamage, ignoreResistances: true);

            _mobThreshold.SetAllowRevives(uid, false, thresholds);
        }
        else
        {
            // No thresholds component — heal and apply a default crit amount
            _damageable.SetAllDamage(uid, damageable, FixedPoint2.Zero);
            var critDamage = new DamageSpecifier();
            critDamage.DamageDict["Blunt"] = FixedPoint2.New(100);
            _damageable.TryChangeDamage(uid, critDamage, ignoreResistances: true);
        }

        // Stamp the time component so Promethine chemistry can gatekeep reversal
        EnsureComp<GhoulificationTimeComponent>(uid);

        // Add feral tracker — further radiation can still push them to feral
        EnsureComp<FeralGhoulifyComponent>(uid);

        // Private message to the transforming player only.
        if (_playerManager.TryGetSessionByEntity(uid, out var session)
            && session.Status == SessionStatus.InGame)
        {
            var selfMsg = Loc.GetString("ghoulify-on-death-self");
            _chatManager.ChatMessageToOne(ChatChannel.Local, selfMsg, selfMsg,
                EntityUid.Invalid, false, session.Channel);
        }

        // Emote broadcast to nearby bystanders — emote system prefixes the entity name.
        _chat.TrySendInGameICMessage(uid,
            Loc.GetString("ghoulify-on-death-others"),
            InGameICChatType.Emote, ChatTransmitRange.Normal, ignoreActionBlocker: true);
    }
}
