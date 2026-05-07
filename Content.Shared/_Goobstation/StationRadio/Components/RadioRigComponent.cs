using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.StationRadio.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RadioRigComponent : Component
{
    /// <summary>
    /// Signal port that is sending out Microphon Data
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> MicrophoneOutputPort = "Microphone";
}
