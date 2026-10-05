using System;

/// <summary>
/// A directed, guaranteed-jumpable connection between two placed blocks.
/// </summary>
[Serializable]
public class PathEdgeData
{
    public int fromBlockId;
    public int toBlockId;

    public PathEdgeData() { }

    public PathEdgeData(int fromBlockId, int toBlockId)
    {
        this.fromBlockId = fromBlockId;
        this.toBlockId = toBlockId;
    }
}
