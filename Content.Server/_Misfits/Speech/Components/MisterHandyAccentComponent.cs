using Content.Server._Misfits.Speech.EntitySystems;

namespace Content.Server._Misfits.Speech.Components;

/// <summary>
///     Polite pre-war robotic butler speech, sir. Word swaps plus occasional courtesies.
/// </summary>
[RegisterComponent]
[Access(typeof(MisterHandyAccentSystem))]
public sealed partial class MisterHandyAccentComponent : Component
{
}
