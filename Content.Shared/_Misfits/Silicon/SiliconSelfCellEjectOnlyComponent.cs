namespace Content.Shared._Misfits.Silicon;

[RegisterComponent]
public sealed partial class SiliconSelfCellEjectOnlyComponent : Component
{
    [DataField]
    public string SlotId = "cell_slot";
}
