using Content.Server._Misfits.Speech.Components;
using Content.Server.Speech;
using Content.Server.Speech.EntitySystems;
using Robust.Shared.Random;

namespace Content.Server._Misfits.Speech.EntitySystems;

public sealed class MisterHandyAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MisterHandyAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    public string Accentuate(string message)
    {
        var result = _replacement.ApplyReplacements(message, "n14MisterHandy");

        // Occasional butler courtesy appended to the end of the sentence.
        var roll = _random.NextDouble();

        if (roll < 0.04)
            result += ", sir";
        else if (roll < 0.08)
            result += ", mum";
        else if (roll < 0.11)
            result += ", quite right";
        else if (roll < 0.13)
            result += ", pip pip";

        return result;
    }

    private void OnAccentGet(EntityUid uid, MisterHandyAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }
}
