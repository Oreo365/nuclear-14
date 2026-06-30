namespace Content.Shared._Misfits.C27;

// #Misfits Add - Marker placed on every C-27 humanoid robot mob entity. Carries the per-species
// stat tunables (EMP damage, EMP stun duration) consumed by MisfitsC27EmpSystem on the server.
// Not networked — purely server-side EMP handling and config; everything visible to the client
// (movement speed, melee, immunities) is set by sibling components on the same entity.
[RegisterComponent]
public sealed partial class MisfitsC27Component : Component
{
    /// <summary>
    ///     Shock damage dealt to the chassis on each EMP pulse. Represents the posibrain /
    ///     internal-electronics shock the spec asks for. Scales linearly with the pulse's
    ///     energy consumption so larger EMP charges hurt more.
    /// </summary>
    [DataField]
    public float EmpShockDamage = 25f;

    /// <summary>
    ///     Per-1000-J multiplier applied on top of <see cref="EmpShockDamage"/>. A standard
    ///     1000 J pulse adds the flat damage; a 4000 J grenade adds 4× extra on top.
    /// </summary>
    [DataField]
    public float EmpDamagePerKiloJoule = 5f;

    /// <summary>
    ///     Multiplier applied to external silicon repair do-afters, such as welder repairs
    ///     and cable-coil wire repairs.
    /// </summary>
    [DataField]
    public float SiliconRepairDelayMultiplier = 1f;

    /// <summary>
    ///     If true, the mob is also added to the EmpDisabled stun pool (forced to drop, can't
    ///     interact for the pulse duration). Spec calls for "possible PA-style stun" — opt-in
    ///     via this flag in case it proves too punishing in playtest.
    /// </summary>
    [DataField]
    public bool ApplyEmpStun = true;
}
