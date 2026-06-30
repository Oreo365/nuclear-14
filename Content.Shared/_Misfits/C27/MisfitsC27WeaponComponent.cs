namespace Content.Shared._Misfits.C27;

// #Misfits Add - Marker for weapons built around C-27 chassis ergonomics.
// The shared C-27 equipment system blocks non-C-27 users from wielding or firing these.
[RegisterComponent]
public sealed partial class MisfitsC27WeaponComponent : Component
{
    /// <summary>
    ///     Optional species whitelist. If unset, any C-27 chassis variant may use the weapon.
    /// </summary>
    [DataField]
    public List<string>? AllowedSpecies;
}
