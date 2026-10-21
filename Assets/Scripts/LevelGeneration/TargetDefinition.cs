using System;
using UnityEngine;

/// <summary>
/// A requested target for the generator to build a guaranteed path to.
/// The target is placed (reserving its space) before any path or fill blocks.
/// </summary>
[Serializable]
public class TargetDefinition
{
    public string targetId = "Target";

    [Tooltip("Id of a structure in the palette (e.g. an exit doorway). Leave empty to use a plain square block instead.")]
    public string structureId = "";

    [Tooltip("Size of the plain block used when no structure is set.")]
    public int fallbackBlockSize = 2;

    [Tooltip("Desired bottom-left cell of the target's bounding box.")]
    public Vector2Int desiredPosition = new Vector2Int(0, 10);

    [Tooltip("How far (in cells) the target may be shifted from the desired position to find free space.")]
    public int positionTolerance = 6;

    [Tooltip("If true, and the generator's 'Allow Shared Trunk' is enabled, this target's path continues from the previous target instead of starting over from Start.")]
    public bool allowSharedTrunk = true;
}
