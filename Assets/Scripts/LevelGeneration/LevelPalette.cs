using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// One asset shared by LevelGenerator (which only reads sizes/shapes/counts)
/// and LevelBuilder (which instantiates the prefabs). Layout records refer to
/// entries here by size (standard blocks) or id (structures, variants).
/// </summary>
[CreateAssetMenu(fileName = "LevelPalette", menuName = "Climbing Level/Level Palette")]
public class LevelPalette : ScriptableObject
{
    public List<StandardBlockEntry> standardBlocks = new List<StandardBlockEntry>
    {
        new StandardBlockEntry { size = 1 },
        new StandardBlockEntry { size = 2 },
        new StandardBlockEntry { size = 4 },
    };

    public List<StructureDefinition> structures = new List<StructureDefinition>();
    public List<VariantDefinition> variants = new List<VariantDefinition>();

    public IEnumerable<int> StandardSizes => standardBlocks.Select(b => b.size).Distinct();

    public GameObject GetStandardPrefab(int size)
    {
        foreach (StandardBlockEntry entry in standardBlocks)
        {
            if (entry.size == size) return entry.prefab;
        }
        return null;
    }

    public StructureDefinition GetStructure(string id)
    {
        foreach (StructureDefinition s in structures)
        {
            if (s.id == id) return s;
        }
        return null;
    }

    public VariantDefinition GetVariant(string id)
    {
        foreach (VariantDefinition v in variants)
        {
            if (v.id == id) return v;
        }
        return null;
    }
}
