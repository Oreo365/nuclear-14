using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Shared._Misfits.Weapons.Ranged.Prediction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PredictedProjectileServerComponent : Component
{
    public ICommonSession Shooter = default!;

    [DataField, AutoNetworkedField]
    public int ClientId;

    [DataField, AutoNetworkedField]
    public EntityUid? ClientEnt;

    [DataField]
    public bool Hit;
}
