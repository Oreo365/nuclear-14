using Content.Server.Preferences.Managers;
using Content.Shared._Misfits.Special;
using Content.Shared._Misfits.Special.Components;
using Content.Shared._Misfits.SpecialStats;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Preferences;
using Robust.Shared.Player;

namespace Content.Server._Misfits.Special;

/// <summary>
/// Keeps Z.A.X silicon SPECIAL behavior consistent across NPC, ghost-role, and admin-spawned player chassis.
/// </summary>
public sealed class ZaxSpecialSystem : EntitySystem
{
    [Dependency] private readonly IServerPreferencesManager _preferences = default!;
    [Dependency] private readonly SharedSpecialSystem _special = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ZaxNeutralSpecialComponent, ComponentStartup>(OnNeutralStartup);
        SubscribeLocalEvent<ZaxNeutralSpecialComponent, MindAddedMessage>(OnNeutralMindAdded);
        SubscribeLocalEvent<ZaxNeutralSpecialComponent, PlayerAttachedEvent>(OnNeutralPlayerAttached);

        SubscribeLocalEvent<ZaxPlayerSpecialComponent, ComponentStartup>(OnPlayerStartup);
        SubscribeLocalEvent<ZaxPlayerSpecialComponent, PlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnNeutralStartup(Entity<ZaxNeutralSpecialComponent> ent, ref ComponentStartup args)
    {
        ApplyNeutral(ent.Owner);
    }

    private void OnNeutralMindAdded(Entity<ZaxNeutralSpecialComponent> ent, ref MindAddedMessage args)
    {
        ApplyNeutral(ent.Owner);
    }

    private void OnNeutralPlayerAttached(Entity<ZaxNeutralSpecialComponent> ent, ref PlayerAttachedEvent args)
    {
        ApplyNeutral(ent.Owner);
    }

    private void OnPlayerStartup(Entity<ZaxPlayerSpecialComponent> ent, ref ComponentStartup args)
    {
        ApplyNeutral(ent.Owner);
    }

    private void OnPlayerAttached(Entity<ZaxPlayerSpecialComponent> ent, ref PlayerAttachedEvent args)
    {
        if (HasComp<ZaxNeutralSpecialComponent>(ent.Owner))
        {
            ApplyNeutral(ent.Owner);
            return;
        }

        ApplySelectedProfile(ent.Owner, args.Player);
    }

    private void ApplyNeutral(EntityUid uid)
    {
        var special = EnsureComp<SpecialComponent>(uid);
        _special.TrySetBaseValues(uid, SpecialProfile.Default(), special);
        RaiseStatsReady(uid);
    }

    private void ApplySelectedProfile(EntityUid uid, ICommonSession player)
    {
        var special = EnsureComp<SpecialComponent>(uid);

        if (_preferences.GetPreferencesOrNull(player.UserId)?.SelectedCharacter is HumanoidCharacterProfile profile)
            _special.TryApplyProfileBaseValues(uid, profile, special);
        else
            _special.TrySetBaseValues(uid, SpecialProfile.Default(), special);

        RaiseStatsReady(uid);
    }

    private void RaiseStatsReady(EntityUid uid)
    {
        var ev = new SpecialStatsReadyEvent(uid);
        RaiseLocalEvent(uid, ref ev, true);
    }
}
