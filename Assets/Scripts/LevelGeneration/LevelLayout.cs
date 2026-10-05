using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure saved/generated level data. Contains everything needed for
/// LevelBuilder to reconstruct the exact same 2D arrangement without regenerating it.
/// Serialization-friendly so it can later be exported/imported as JSON.
/// </summary>
[CreateAssetMenu(fileName = "NewLevelLayout", menuName = "Climbing Level/Level Layout")]
public class LevelLayout : ScriptableObject
{
    public int width;
    public int height;
    public int seed;

    public List<GeneratedBlockData> blocks = new List<GeneratedBlockData>();
    public int startBlockId = -1;
    public List<LevelTargetData> targets = new List<LevelTargetData>();
    public List<PathEdgeData> pathEdges = new List<PathEdgeData>();

    public GeneratedBlockData GetBlock(int id)
    {
        for (int i = 0; i < blocks.Count; i++)
        {
            if (blocks[i].id == id) return blocks[i];
        }
        return null;
    }
}
