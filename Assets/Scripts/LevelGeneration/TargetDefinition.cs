using System;
using UnityEngine;

/// <summary>
/// A requested target for the generator to build a guaranteed path to.
/// The actual placed block may drift from desiredPosition by up to positionTolerance,
/// since the path generator needs room to land a valid final jump onto it.
/// </summary>
[Serializable]
public class TargetDefinition
{
    public string targetId = "Target";

    [Tooltip("Approximate desired position for this target, in grid cells (bottom-left of its eventual block).")]
    public Vector2Int desiredPosition = new Vector2Int(0, 10);

    [Tooltip("How far (in cells) the actual placed target may drift from the desired position while still counting as valid.")]
    public int positionTolerance = 6;

    [Tooltip("If true, and the generator's 'Allow Shared Trunk' option is enabled, this target's path may reuse platforms already placed for an earlier target before diverging.")]
    public bool allowSharedTrunk = true;
}
