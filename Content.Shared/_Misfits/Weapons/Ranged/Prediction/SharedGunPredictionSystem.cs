using Content.Shared._Misfits.CCVar;
using Robust.Shared.Configuration;

namespace Content.Shared._Misfits.Weapons.Ranged.Prediction;

public abstract class SharedGunPredictionSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;

    public bool GunPrediction { get; private set; }

    public override void Initialize()
    {
        Subs.CVar(_config, PerformanceCVars.GunPrediction, v => GunPrediction = v, true);
    }
}
