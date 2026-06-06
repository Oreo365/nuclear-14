using Content.Shared._Misfits.ChronicPain.EntitySystems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;


namespace Content.Shared._Misfits.ChronicPain.ChemicalEffects;
using Content.Shared.EntityEffects;
using Content.Shared._Misfits.ChronicPain.EntitySystems;


public sealed partial class SuppressChronicPainReagent : EntityEffect
{
    /// <summary>
    /// How long the suppression applies (in seconds) when metabolized.
    /// </summary>
    [DataField("SuppressionTime")]
    public float SuppressionTime { get; set; } = 0f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "suppresses chronic pain for a time.";
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        args.EntityManager.System<SharedChronicPainSystem>().TrySuppressChronicPain(args.TargetEntity, TimeSpan.FromSeconds(SuppressionTime));
    }
}
