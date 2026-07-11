namespace Content.Shared._Misfits.Silicon;

/// <summary>
/// [Changed by MisfitsCrew/Operator] Marks NPC silicon units that may receive Station AI field commands.
/// </summary>
[RegisterComponent]
public sealed partial class ZaxUnitComponent : Component;

/// <summary>
/// [Changed by MisfitsCrew/Operator] Marks any Z.A.X-linked chassis that should appear in
/// the Z.A.X Core linked-unit directory.
/// </summary>
[RegisterComponent]
public sealed partial class ZaxLinkedUnitComponent : Component;
