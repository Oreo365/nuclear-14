using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.StationRadio.Components;

/// <summary>
/// Relays all packets from the vinyl player and rig to all entities with <see cref="StationRadioReceiverComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StationRadioServerComponent : Component
{
    /// <summary>
    /// VinylPlayer that holds music info
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? VinylPlayer;

    /// <summary>
    /// Signal port that is sending out music data.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> MusicOutputPort = "VinylMusic";
}
