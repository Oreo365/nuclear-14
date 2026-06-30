// #Misfits Add - Per-round Enclave recruitment system.
// Enclave members get a right-click "Recruit" verb on player entities.
// Recruited players gain EnclaveRecruit playtime so their time counts
// toward Enclave department role timers. Resets on death or round restart.

using System.Linq;
using Content.Server.Administration;
using Content.Server.Mind;
using Content.Shared._Misfits.Enclave;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Verbs;
using Robust.Server.Player;
using Content.Shared.IdentityManagement;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Misfits.Enclave;

public sealed class EnclaveRecruitSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly SharedMindSystem _minds = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;

    /// <summary>Enclave department ID from the department prototype.</summary>
    private const string EnclaveDepartmentId = "Enclave";

    /// <summary>Tracker ID injected on recruited players.</summary>
    private const string EnclaveRecruitTrackerId = "EnclaveRecruit";

    public override void Initialize()
    {
        base.Initialize();

        // Show "Recruit" verb on living player entities for Enclave members
        SubscribeLocalEvent<MindContainerComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);

        // Inject EnclaveRecruit tracker for recruited minds
        SubscribeLocalEvent<EnclaveRecruitMindComponent, MindGetAllRolesEvent>(OnMindGetAllRoles);

        // Remove recruitment on death
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);

        // Clean up all recruitments on round restart
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    /// <summary>
    /// Add "Recruit" verb for Enclave members on player entities.
    /// </summary>
    private void OnGetInteractionVerbs(
        EntityUid target,
        MindContainerComponent targetMind,
        GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;

        // User must be a living player with an Enclave job
        if (!IsEnclaveMember(user))
            return;

        // Target must be a living player with a mind
        if (!targetMind.HasMind)
            return;

        // Target must not already be recruited
        if (HasComp<EnclaveRecruitMindComponent>(targetMind.Mind))
            return;

        // Target must be alive (not dead/ghost)
        if (!TryComp<MobStateComponent>(target, out var mobState)
            || mobState.CurrentState != MobState.Alive)
            return;

        // Don't show verb on self
        if (user == target)
            return;

        args.Verbs.Add(new InteractionVerb
        {
            Text = "Recruit",
            Category = VerbCategory.Interaction,
            Act = () => RecruitPlayer(target, targetMind, user),
        });
    }

    /// <summary>
    /// Inject EnclaveRecruit tracker into the playtime system for recruited minds.
    /// </summary>
    private void OnMindGetAllRoles(
        EntityUid mindId,
        EnclaveRecruitMindComponent component,
        ref MindGetAllRolesEvent args)
    {
        args.Roles.Add(new RoleInfo(
            component,
            Loc.GetString("job-name-enclave-recruit"),
            false,
            EnclaveRecruitTrackerId,
            "EnclaveRecruit"));
    }

    /// <summary>
    /// Remove recruitment when the player dies.
    /// </summary>
    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (ev.NewMobState != MobState.Dead)
            return;

        RemoveRecruitment(ev.Target);
    }

    /// <summary>
    /// Clean up all EnclaveRecruitMindComponents on round restart.
    /// </summary>
    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        var query = EntityQueryEnumerator<EnclaveRecruitMindComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            RemComp<EnclaveRecruitMindComponent>(uid);
        }
    }

    /// <summary>
    /// Show a confirmation dialog to the target. Only recruits if they accept.
    /// </summary>
    private void RecruitPlayer(EntityUid target, MindContainerComponent targetMind, EntityUid user)
    {
        if (!targetMind.HasMind)
            return;

        var mindId = targetMind.Mind.Value;

        // Already recruited
        if (HasComp<EnclaveRecruitMindComponent>(mindId))
            return;

        // Get the target's player session for the dialog
        if (!_minds.TryGetSession(mindId, out var targetSession))
            return;

        var userName = Identity.Name(user, EntityManager);
        var targetName = Identity.Name(target, EntityManager);

        _quickDialog.OpenConfirmationDialog(
            targetSession,
            Loc.GetString("enclave-recruit-dialog-title"),
            Loc.GetString("enclave-recruit-dialog-accept"),
            Loc.GetString("enclave-recruit-dialog-decline"),
            // Accept
            () =>
            {
                // Double-check they're still not recruited (may have been recruited while dialog was open)
                if (!targetMind.HasMind || HasComp<EnclaveRecruitMindComponent>(targetMind.Mind!.Value))
                    return;

                ApplyRecruitment(target, targetMind, user, userName, targetName);
            },
            // Decline
            () =>
            {
                _popup.PopupEntity(
                    Loc.GetString("enclave-recruit-declined", ("target", (object)targetName)),
                    user,
                    user,
                    PopupType.MediumCaution);
            });
    }

    /// <summary>
    /// Actually apply the recruitment (called after target confirms).
    /// </summary>
    private void ApplyRecruitment(EntityUid target, MindContainerComponent targetMind, EntityUid user,
        string userName, string targetName)
    {
        if (!targetMind.HasMind)
            return;

        var mindId = targetMind.Mind!.Value;

        if (HasComp<EnclaveRecruitMindComponent>(mindId))
            return;

        AddComp<EnclaveRecruitMindComponent>(mindId);

        _popup.PopupEntity(
            Loc.GetString("enclave-recruit-popup-target", ("user", userName)),
            target,
            target,
            PopupType.Medium);

        _popup.PopupEntity(
            Loc.GetString("enclave-recruit-popup-user", ("target", targetName)),
            user,
            user,
            PopupType.Medium);
    }

    /// <summary>
    /// Remove EnclaveRecruitMindComponent from a player's mind on death.
    /// </summary>
    private void RemoveRecruitment(EntityUid body)
    {
        if (!TryComp<MindContainerComponent>(body, out var mindContainer) || !mindContainer.HasMind)
            return;

        var mindId = mindContainer.Mind.Value;

        if (!HasComp<EnclaveRecruitMindComponent>(mindId))
            return;

        RemComp<EnclaveRecruitMindComponent>(mindId);

        // Notify the player they are no longer recruited
        if (_minds.TryGetSession(mindId, out var session))
        {
            _popup.PopupEntity(
                Loc.GetString("enclave-recruit-lost"),
                body,
                body,
                PopupType.MediumCaution);
        }
    }

    /// <summary>
    /// Check if a user entity is an Enclave member (has an Enclave department job).
    /// </summary>
    private bool IsEnclaveMember(EntityUid uid)
    {
        // Get the mind ID from the user's entity
        if (!TryComp<MindContainerComponent>(uid, out var mindContainer) || !mindContainer.HasMind)
            return false;

        var mindId = mindContainer.Mind.Value;

        // Check if the user has a job component on their mind
        if (!_jobs.MindTryGetJob(mindId, out _, out var jobProto))
            return false;

        // Check if the job belongs to the Enclave department
        var department = _prototypes.Index<DepartmentPrototype>(EnclaveDepartmentId);
        return department.Roles.Contains(jobProto.ID);
    }
}
