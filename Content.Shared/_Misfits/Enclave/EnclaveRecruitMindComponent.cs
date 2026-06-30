// #Misfits Add - Marks a mind as being per-round recruited by the Enclave.
// When present on a mind entity, it injects the EnclaveRecruit playtime
// tracker via MindGetAllRolesEvent so the player accumulates Enclave
// department time. Removed on death or round restart.

using Content.Shared.Roles;
using Content.Shared.Players.PlayTimeTracking;

namespace Content.Shared._Misfits.Enclave;

/// <summary>
/// Added to a mind entity when a player is per-round recruited into the
/// Enclave. The EnclaveRecruitSystem handles lifecycle (add on recruit
/// verb, remove on death/round restart). While present, the mind reports
/// an EnclaveRecruit playtime tracker so that all accumulated time counts
/// toward the Enclave department's role timers.
/// </summary>
[RegisterComponent]
public sealed partial class EnclaveRecruitMindComponent : Component;
