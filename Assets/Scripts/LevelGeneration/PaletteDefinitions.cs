using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>A standard square block size and the prefab used to build it.</summary>
[Serializable]
public class StandardBlockEntry
{
    public int size = 1;
    public GameObject prefab;
}

/// <summary>
/// A shaped prefab placed before any other block (Start, exit doorway, shop...).
/// The generator only needs its 2D footprint; LevelBuilder instantiates the whole prefab.
///
/// All cell coordinates are relative to the bottom-left cell of the bounding box (0,0),
/// in the NON-mirrored orientation. The prefab's pivot must be that bottom-left corner.
///
/// Example - exit doorway, 3 wide x 4 tall, 1x2 opening in the middle:
///   width 3, height 4
///   freeCells  : (0,0) (2,0) (0,3) (2,3)   (the four corners)
///   entryCells : (1,0)                     (the block under the doorway)
/// The opening itself (1,1) and (1,2) is NOT listed as free, so it stays reserved and
/// nothing is ever generated inside it.
/// </summary>
[Serializable]
public class StructureDefinition
{
    public string id = "Structure";
    public GameObject prefab;

    [Min(1)] public int width = 3;
    [Min(1)] public int height = 4;

    [Tooltip("Cells inside the bounding box that are NOT part of the structure. The generator treats them as normal free space and fills them like any other.")]
    public List<Vector2Int> freeCells = new List<Vector2Int>();

    [Tooltip("Cells the player must land on. Each is treated as a 1x1 platform whose top surface is the landing spot.")]
    public List<Vector2Int> entryCells = new List<Vector2Int>();

    [Tooltip("Allow the generator to flip this structure horizontally when that gives a simpler approach.")]
    public bool allowMirror = true;

    /// <summary>Cells reserved by the structure (bounding box minus free cells), in placed orientation.</summary>
    public IEnumerable<Vector2Int> GetReservedCells(bool mirrored)
    {
        for (int cy = 0; cy < height; cy++)
        {
            for (int cx = 0; cx < width; cx++)
            {
                int defX = mirrored ? width - 1 - cx : cx;
                if (freeCells.Contains(new Vector2Int(defX, cy))) continue;
                yield return new Vector2Int(cx, cy);
            }
        }
    }

    /// <summary>Entry cells in placed orientation.</summary>
    public IEnumerable<Vector2Int> GetEntryCells(bool mirrored)
    {
        foreach (Vector2Int e in entryCells)
        {
            yield return new Vector2Int(mirrored ? width - 1 - e.x : e.x, e.y);
        }
    }

    public bool Validate(out string error)
    {
        if (string.IsNullOrEmpty(id)) { error = "A structure has no id."; return false; }
        if (width < 1 || height < 1) { error = $"Structure '{id}' has an invalid size."; return false; }
        if (entryCells == null || entryCells.Count == 0) { error = $"Structure '{id}' needs at least one entry cell."; return false; }

        foreach (Vector2Int e in entryCells)
        {
            if (e.x < 0 || e.y < 0 || e.x >= width || e.y >= height)
            {
                error = $"Structure '{id}': entry cell {e} is outside its {width}x{height} bounding box.";
                return false;
            }
            if (freeCells != null && freeCells.Contains(e))
            {
                error = $"Structure '{id}': entry cell {e} is also listed as a free cell.";
                return false;
            }
        }

        if (freeCells != null)
        {
            foreach (Vector2Int f in freeCells)
            {
                if (f.x < 0 || f.y < 0 || f.x >= width || f.y >= height)
                {
                    error = $"Structure '{id}': free cell {f} is outside its {width}x{height} bounding box.";
                    return false;
                }
            }
        }

        error = "";
        return true;
    }
}

/// <summary>
/// A special prefab that can replace a standard block of the same size
/// (e.g. an unmovable block). Counts are per whole level.
/// The replacement keeps the footprint, so the top surface and reachability are unchanged.
/// </summary>
[Serializable]
public class VariantDefinition
{
    public string id = "Variant";
    public GameObject prefab;

    [Tooltip("Footprint of the standard blocks this variant can replace (1, 2, 4...).")]
    public int size = 2;

    [Min(0)] public int minPerLevel = 0;
    [Min(0)] public int maxPerLevel = 3;

    [Tooltip("If false, the variant is never placed on blocks that belong to a guaranteed path.")]
    public bool allowedOnPaths = false;
}
