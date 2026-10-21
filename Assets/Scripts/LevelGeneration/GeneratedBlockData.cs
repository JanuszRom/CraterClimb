using System;

/// <summary>
/// A single placed object in the generated layout. (x, y) is the bottom-left logical cell
/// of its bounding box. Standard and variant blocks are square (width == height);
/// structures can be any rectangle.
/// </summary>
[Serializable]
public class GeneratedBlockData
{
    public int id;
    public BlockCategory category;
    public string prefabId;   // structure/variant id; empty for standard blocks
    public int x;             // bottom-left cell X
    public int y;             // bottom-left cell Y
    public int width;
    public int height;
    public bool mirrored;     // structures only: flipped along the wall's horizontal axis
    public int size;

    /// <summary>Exclusive right edge (x + width).</summary>
    public int Right => x + width;

    /// <summary>Exclusive top edge / standing surface height (y + height).</summary>
    public int Top => y + height;

    public GeneratedBlockData() { }

    /// <summary>Convenience constructor for a standard square block.</summary>
    public GeneratedBlockData(int id, int size, int x, int y)
        : this(id, BlockCategory.Standard, "", x, y, size, size, false, size) { }

    public GeneratedBlockData(int id, BlockCategory category, string prefabId,
                              int x, int y, int width, int height, bool mirrored, int size)
    {
        this.id = id;
        this.category = category;
        this.prefabId = prefabId;
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
        this.mirrored = mirrored;
        this.size = size;
    }
}
