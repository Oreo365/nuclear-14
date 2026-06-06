// #Misfits Change /Add/ - Private do-style hunger and thirst ambience for player characters.
using Content.Server.Chat.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Content.Shared.Stunnable;
using Robust.Shared.Timing;

namespace Content.Server._Misfits.Nutrition;

/// <summary>
/// Sends first-person, private do-style flavor text to player-controlled entities
/// when hunger and thirst cross into specific thresholds.
/// </summary>
public sealed class NeedFlavorTextSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<EntityUid, HungerThreshold> _lastHungerThresholds = new();
    private readonly Dictionary<EntityUid, ThirstThreshold> _lastThirstThresholds = new();
    private readonly Dictionary<EntityUid, TimeSpan> _nextCollapseAttemptAt = new();

    // Misfits Tweak - Cooldown per entity to reduce flavortext spam when hovering near a threshold boundary.
    private readonly Dictionary<EntityUid, TimeSpan> _nextHungerFlavorAt = new();
    private readonly Dictionary<EntityUid, TimeSpan> _nextThirstFlavorAt = new();
    private static readonly TimeSpan FlavorTextCooldown = TimeSpan.FromMinutes(5);

    // #Misfits Fix: At 70+ players two EntityQueryEnumerator passes every tick add up to ~2800
    // component lookups/s. Gate the scan to 2 s — hunger/thirst thresholds change on a many-second
    // cadence so we will never miss a transition at this resolution.
    private float _scanTimer;
    private const float ScanInterval = 2f;

    private static readonly string[] HungerPeckishMessages =
    {
        "need-flavor-hunger-peckish-1",
        "need-flavor-hunger-peckish-2",
        "need-flavor-hunger-peckish-3",
        "need-flavor-hunger-peckish-4",
    };

    private static readonly string[] HungerStarvingMessages =
    {
        "need-flavor-hunger-starving-1",
        "need-flavor-hunger-starving-2",
        "need-flavor-hunger-starving-3",
        "need-flavor-hunger-starving-4",
    };

    private static readonly string[] ThirstThirstyMessages =
    {
        "need-flavor-thirst-thirsty-1",
        "need-flavor-thirst-thirsty-2",
        "need-flavor-thirst-thirsty-3",
        "need-flavor-thirst-thirsty-4",
    };

    private static readonly string[] ThirstParchedMessages =
    {
        "need-flavor-thirst-parched-1",
        "need-flavor-thirst-parched-2",
        "need-flavor-thirst-parched-3",
        "need-flavor-thirst-parched-4",
    };

    private static readonly string[] HungerDeadMessages =
    {
        "need-flavor-hunger-dead-1",
        "need-flavor-hunger-dead-2",
        "need-flavor-hunger-dead-3",
        "need-flavor-hunger-dead-4",
    };

    private static readonly string[] ThirstDeadMessages =
    {
        "need-flavor-thirst-dead-1",
        "need-flavor-thirst-dead-2",
        "need-flavor-thirst-dead-3",
        "need-flavor-thirst-dead-4",
    };

    public override void Initialize()
    {
        base.Initialize();

        // #Misfits Fix: System defunct — removed for 70+ player performance. Re-enable by
        // un-commenting all subscriptions here and the Update body below.
        // SubscribeLocalEvent<HungerComponent, ComponentRemove>(OnHungerShutdown);
        // SubscribeLocalEvent<ThirstComponent, ComponentShutdown>(OnThirstShutdown);
    }

    private void OnHungerShutdown(EntityUid uid, HungerComponent component, ComponentRemove args)
    {
        _lastHungerThresholds.Remove(uid);
        _nextCollapseAttemptAt.Remove(uid);
        _nextHungerFlavorAt.Remove(uid);
    }

    private void OnThirstShutdown(EntityUid uid, ThirstComponent component, ComponentShutdown args)
    {
        _lastThirstThresholds.Remove(uid);
        _nextCollapseAttemptAt.Remove(uid);
        _nextThirstFlavorAt.Remove(uid);
    }

    public override void Update(float frameTime)
    {
        // #Misfits Fix: Defunct at 70+ players — two per-player entity-query passes every scan
        // contributed significant tick overhead. Hunger/thirst flavor text is cosmetic only.
        // Restore by un-commenting the body below and the Initialize subscriptions above.
        
         base.Update(frameTime);
         _scanTimer += frameTime;
         if (_scanTimer < ScanInterval)
             return;
         _scanTimer -= ScanInterval;
         var hungerQuery = EntityQueryEnumerator<ActorComponent, HungerComponent>();
         while (hungerQuery.MoveNext(out var uid, out var actor, out var hunger))
         {
             if (ShouldSkip(uid, actor.PlayerSession))
                 continue;
             ProcessHunger(uid, actor.PlayerSession, hunger);
         }
         var thirstQuery = EntityQueryEnumerator<ActorComponent, ThirstComponent>();
         while (thirstQuery.MoveNext(out var uid, out var actor, out var thirst))
         {
             if (ShouldSkip(uid, actor.PlayerSession))
                 continue;
             ProcessThirst(uid, actor.PlayerSession, thirst);
         }
    }

    private bool ShouldSkip(EntityUid uid, ICommonSession session)
    {
        if (session.AttachedEntity != uid)
            return true;

        return TryComp<MobStateComponent>(uid, out var mobState) && _mobState.IsDead(uid, mobState);
    }

    private void ProcessHunger(EntityUid uid, ICommonSession session, HungerComponent hunger)
    {
        var threshold = hunger.CurrentThreshold;
        if (_lastHungerThresholds.TryAdd(uid, threshold))
        {
            if (threshold == HungerThreshold.Dead)
                TryCauseCollapse(uid);

            return;
        }

        var previous = _lastHungerThresholds[uid];
        if (previous != threshold)
        {
            _lastHungerThresholds[uid] = threshold;

            if (threshold == HungerThreshold.Dead)
            {
                TryCauseCollapse(uid);
                // Misfits Add - Fixed near-death self-emote fires once when hunger enters the Dead threshold.
                SendNearDeathMessage(session);
            }

            if (IsInterestingHungerThreshold(threshold))
                TrySendAmbientNeedMessage(uid, session, GetHungerMessages(threshold), _nextHungerFlavorAt);

            return;
        }

        if (threshold == HungerThreshold.Dead)
            TryCauseCollapse(uid);
    }

    private void ProcessThirst(EntityUid uid, ICommonSession session, ThirstComponent thirst)
    {
        var threshold = thirst.CurrentThirstThreshold;
        if (_lastThirstThresholds.TryAdd(uid, threshold))
        {
            if (threshold == ThirstThreshold.Dead)
                TryCauseCollapse(uid);

            return;
        }

        var previous = _lastThirstThresholds[uid];
        if (previous != threshold)
        {
            _lastThirstThresholds[uid] = threshold;

            if (threshold == ThirstThreshold.Dead)
            {
                TryCauseCollapse(uid);
                // Misfits Add - Fixed near-death self-emote fires once when thirst enters the Dead threshold.
                SendNearDeathMessage(session);
            }

            if (IsInterestingThirstThreshold(threshold))
                TrySendAmbientNeedMessage(uid, session, GetThirstMessages(threshold), _nextThirstFlavorAt);

            return;
        }

        if (threshold == ThirstThreshold.Dead)
            TryCauseCollapse(uid);
    }

    private void TryCauseCollapse(EntityUid uid)
    {
        if (_nextCollapseAttemptAt.TryGetValue(uid, out var nextAt) && _timing.CurTime < nextAt)
            return;

        _nextCollapseAttemptAt[uid] = _timing.CurTime + TimeSpan.FromSeconds(18);

        if (_random.Prob(0.18f))
            _stun.TryKnockdown(uid, TimeSpan.FromSeconds(2.5f), true);
    }

    // Misfits Tweak - Only send if the per-entity cooldown has elapsed; update the cooldown on success.
    private void TrySendAmbientNeedMessage(EntityUid uid, ICommonSession session, string[] messageKeys, Dictionary<EntityUid, TimeSpan> cooldownMap)
    {
        if (cooldownMap.TryGetValue(uid, out var nextAt) && _timing.CurTime < nextAt)
            return;

        cooldownMap[uid] = _timing.CurTime + FlavorTextCooldown;
        var text = Loc.GetString(_random.Pick(messageKeys));
        _chat.SendPrivateDoMessage(session, text);
    }

    /// <summary>
    /// Misfits Add - Sends the fixed near-death self-emote to the player only.
    /// Fires once when hunger or thirst first crosses into the Dead threshold.
    /// </summary>
    private void SendNearDeathMessage(ICommonSession session)
    {
        var text = Loc.GetString("need-flavor-near-death");
        _chat.SendPrivateDoMessage(session, text);
    }

    private static bool IsInterestingHungerThreshold(HungerThreshold threshold)
    {
        return threshold is HungerThreshold.Peckish or HungerThreshold.Starving or HungerThreshold.Dead;
    }

    private static bool IsInterestingThirstThreshold(ThirstThreshold threshold)
    {
        return threshold is ThirstThreshold.Thirsty or ThirstThreshold.Parched or ThirstThreshold.Dead;
    }

    private static string[] GetHungerMessages(HungerThreshold threshold)
    {
        return threshold switch
        {
            HungerThreshold.Dead => HungerDeadMessages,
            HungerThreshold.Starving => HungerStarvingMessages,
            HungerThreshold.Peckish => HungerPeckishMessages,
            _ => Array.Empty<string>(),
        };
    }

    private static string[] GetThirstMessages(ThirstThreshold threshold)
    {
        return threshold switch
        {
            ThirstThreshold.Dead => ThirstDeadMessages,
            ThirstThreshold.Parched => ThirstParchedMessages,
            ThirstThreshold.Thirsty => ThirstThirstyMessages,
            _ => Array.Empty<string>(),
        };
    }

}