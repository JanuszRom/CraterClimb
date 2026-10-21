using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Takes a LevelLayout (pure data) and instantiates the real prefabs from the shared palette.
/// Structures and variants are instantiated as whole prefabs; the builder never assembles them.
///
/// Mapping: logical x -> world Z, logical y -> world Y, world X is the fixed wall depth.
/// Every prefab's pivot is assumed to be the bottom-left corner of its bounding box.
/// </summary>
public class LevelBuilder : MonoBehaviour
{
    [Header("Source")]
    public LevelLayout layout;
    [Tooltip("The same palette asset assigned to LevelGenerator.")]
    public LevelPalette palette;

    [Header("Prefab Mapping")]
    public List<BlockSizeOption> blockPrefabs = new List<BlockSizeOption>();

    [Header("World Placement")]
    public float cellSize = 1f;
    [Tooltip("Fixed world X offset of the whole wall.")]
    public float wallDepth = 0f;
    public Transform container;
    private Dictionary<int, GameObject> prefabBySize;

    [ContextMenu("Build")]
    public void Build()
    {
        if (layout == null)
        {
            Debug.LogError("[LevelBuilder] No LevelLayout assigned.");
            return;
        }
        if (palette == null)
        {
            Debug.LogError("[LevelBuilder] No LevelPalette assigned.");
            return;
        }

        EnsureContainer();
        ClearBuilt();

        int built = 0;
        foreach (GeneratedBlockData block in layout.blocks)
        {
            
            GameObject prefab = ResolvePrefab(block);
            if (prefab == null)
            {
                Debug.LogWarning($"[LevelBuilder] No prefab found for block {block.id} ({block.category} '{block.prefabId}', size {block.width}x{block.height}). Skipping.");
                continue;
            }

            // A mirrored structure is flipped along Z around its pivot, so it extends
            // toward -Z; shift the pivot to the far edge so it still fills its cells.
            float worldZ = block.mirrored
                ? (block.x + block.width) * cellSize
                : block.x * cellSize;

            Vector3 worldPos = container.position + new Vector3(
                0f,
                /*(*/block.y/* + block.size * 0.5f)*/ * cellSize,
                (block.x + block.size * 0.5f) * cellSize
                );

            GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity, container);

            if (block.mirrored)
            {
                Vector3 s = instance.transform.localScale;
                s.z = -s.z;
                instance.transform.localScale = s;
            }

            instance.name = block.category == BlockCategory.Standard
                ? $"Block_{block.width}x{block.height}_{block.id}"
                : $"{block.prefabId}_{block.id}";

            built++;
        }

        Debug.Log($"[LevelBuilder] Built {built}/{layout.blocks.Count} objects from layout '{layout.name}'.");
    }

    [ContextMenu("Clear Built Level")]
    public void ClearBuilt()
    {
        if (container == null) return;

        for (int i = container.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(container.GetChild(i).gameObject);
                continue;
            }
#endif
            Destroy(container.GetChild(i).gameObject);
        }
    }

    private GameObject ResolvePrefab(GeneratedBlockData block)
    {
        switch (block.category)
        {
            case BlockCategory.Structure:
                return palette.GetStructure(block.prefabId)?.prefab;
            case BlockCategory.Variant:
                return palette.GetVariant(block.prefabId)?.prefab;
            default:
                return palette.GetStandardPrefab(block.width);
        }
    }

    private void EnsureContainer()
    {
        if (container != null) return;

        var go = new GameObject("GeneratedLevel");
        go.transform.SetParent(transform, false);
        container = go.transform;
    }
    private void BuildPrefabLookup()
    {
        prefabBySize = new Dictionary<int, GameObject>();
        foreach (var option in blockPrefabs)
        {
            prefabBySize[option.size] = option.prefab;
        }
    }
}
