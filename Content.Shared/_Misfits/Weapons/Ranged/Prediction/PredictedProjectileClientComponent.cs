using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._Misfits.Weapons.Ranged.Prediction;

[RegisterComponent]
public sealed partial class PredictedProjectileClientComponent : Component
{
    [DataField]
    public bool Hit;

    [DataField]
    public EntityCoordinates? Coordinates;
}
