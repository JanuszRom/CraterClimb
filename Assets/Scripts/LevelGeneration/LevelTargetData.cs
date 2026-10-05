using System;

/// <summary>
/// Links a named target (e.g. "Shop", "Finish") to the block it was placed on.
/// </summary>
[Serializable]
public class LevelTargetData
{
    public string targetId;
    public int blockId;

    public LevelTargetData() { }

    public LevelTargetData(string targetId, int blockId)
    {
        this.targetId = targetId;
        this.blockId = blockId;
    }
}
