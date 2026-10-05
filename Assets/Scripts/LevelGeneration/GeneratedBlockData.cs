using System;

/// <summary>
/// A single placed block in the generated layout.
/// (x, y) is the bottom-left logical cell. The block occupies
/// cells [x, x+size-1] x [y, y+size-1] inclusive.
/// </summary>
[Serializable]
public class GeneratedBlockData
{
    public int id;
    public int size;   // footprint, e.g. 1, 2, 4, 6, 8
    public int x;       // bottom-left cell X
    public int y;       // bottom-left cell Y

    /// <summary>Exclusive right edge (x + size).</summary>
    public int Right => x + size;

    /// <summary>Exclusive top edge / standing surface height (y + size).</summary>
    public int Top => y + size;

    public GeneratedBlockData() { }

    public GeneratedBlockData(int id, int size, int x, int y)
    {
        this.id = id;
        this.size = size;
        this.x = x;
        this.y = y;
    }
}
