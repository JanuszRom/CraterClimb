using System;
using UnityEngine;

/// <summary>
/// Abstract jump-reach parameters used by the generator's reachability test.
/// All values are in grid cells.
/// </summary>
[Serializable]
public class JumpSettings
{
    [Tooltip("Maximum height the player can jump upward.")]
    public float maxJumpHeight = 2f;

    [Tooltip("Maximum horizontal distance, measured edge-to-edge (closest side of one block to closest side of the other), the player can cross in a single jump. Applies to both upward and downward jumps.")]
    public float maxJumpDistance = 3f;

    [Tooltip("Maximum height the player can safely drop down in a single jump. Keep this small (e.g. 1-2 cells) so the player doesn't lose track of a viable path by dropping too far.")]
    public float maxDropHeight = 2f;
}
