namespace Content.Shared._Misfits.C27;

// #Misfits Add - Marker placed on every C-27 outer-armor / helmet entity. Presence of this
// component enforces the rule "only C-27 robots may wear this clothing". The inverse direction
// (a C-27 wearing non-C-27 outerwear) is enforced by the `c27` inventory template's tag
// whitelist (`C27Wearable`) defined in Resources/Prototypes/_Misfits/InventoryTemplates/.
// Not networked: the marker + RequiresC27Species field never change at runtime, and the
// equip-gate runs in a shared system so client + server agree without state replication.
[RegisterComponent]
public sealed partial class MisfitsC27ArmorComponent : Component
{
    /// <summary>
    ///     If false, the species check is skipped (e.g. salvaged trophy variants kept for
    ///     decorative/admin use). Defaults to true so newly-defined armors are restricted by
    ///     default unless an author opts out.
    /// </summary>
    [DataField]
    public bool RequiresC27Species = true;

    /// <summary>
    ///     Optional species whitelist for chassis equipment. If unset, any C-27 chassis
    ///     variant may equip the item.
    /// </summary>
    [DataField]
    public List<string>? AllowedSpecies;
}
