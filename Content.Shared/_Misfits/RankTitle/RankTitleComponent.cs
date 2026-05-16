namespace Content.Shared._Misfits.RankTitle;

/// <summary>
/// When equipped by a humanoid, overrides the job title shown in examine text with this rank display string.
/// </summary>
[RegisterComponent]
public sealed partial class RankTitleComponent : Component
{
    [DataField(required: true)]
    public string RankTitle = string.Empty;
}
