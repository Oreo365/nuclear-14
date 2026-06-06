namespace Content.Shared._Misfits.Traits;

[RegisterComponent]
public sealed partial class GibModifierComponent : Component
{

    [DataField]
    [ViewVariables]
    public float GibThresholdMultiplier = 1f;

    [DataField]
    [ViewVariables]
    public float SeverThresholdMultiplier = 1f;

}
