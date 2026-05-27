using Robust.Shared.Prototypes;

namespace Content.Server.NPC.HTN;

/// <summary>
/// Represents a network of multiple tasks. This gets expanded out to its relevant nodes.
/// </summary>
[Prototype("htnCompound")]
public sealed partial class HTNCompoundPrototype : IPrototype
{
    [IdDataField] public string ID { get; set; } = string.Empty;

    [DataField("branches", required: true)]
    public List<HTNBranch> Branches = new();

    /// <summary>
    /// If set, marks this as the base of a follower-capable compound family and names the Follow-order variant.
    /// Passive and Neutral default to this value if not specified.
    /// </summary>
    [DataField]
    public string? FollowerFollow;

    /// <summary>Compound to use for Passive order. Defaults to <see cref="FollowerFollow"/> if unset.</summary>
    [DataField]
    public string? FollowerPassive;

    /// <summary>Compound to use for Neutral order. Defaults to <see cref="FollowerFollow"/> if unset.</summary>
    [DataField]
    public string? FollowerNeutral;
}
