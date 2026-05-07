using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.DeviceNetwork;

/// <summary>
/// Allows this device to have a custom frequency that can be edited with a UI interaction.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DeviceCustomFrequencyComponent : Component
{
    /// <summary>
    /// Should Frequency be editable
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool FrequencyChange;

    [DataField, AutoNetworkedField]
    public uint MinFrequency = 1000;

    [DataField, AutoNetworkedField]
    public uint MaxFrequency = 9999;
}
