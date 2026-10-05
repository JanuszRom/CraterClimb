using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Takes a LevelLayout (pure data) and instantiates your actual 3D block prefabs.
/// Contains no generation/validation logic - it only reads the finished layout.
/// </summary>
public class LevelBuilder : MonoBehaviour
{
    [Header("Source Layout")]
    public LevelLayout layout;

    [Header("Prefab Mapping")]
    public List<BlockSizeOption> blockPrefabs = new List<BlockSizeOption>();

    [Header("World Placement")]
    public float cellSize = 1f;
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

        EnsureContainer();
        ClearBuilt();
        BuildPrefabLookup();

        foreach (var block in layout.blocks)
        {
            if (!prefabBySize.TryGetValue(block.size, out GameObject prefab) || prefab == null)
            {
                Debug.LogWarning($"[LevelBuilder] No prefab assigned for block size {block.size} (block id {block.id}). Skipping.");
                continue;
            }

            Vector3 worldPos = container.position + new Vector3(
                0f,
                /*(*/block.y/* + block.size * 0.5f)*/ * cellSize,
                (block.x + block.size * 0.5f) * cellSize
                );

            GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity, container);
            instance.name = $"Block_{block.size}x{block.size}_{block.id}";
        }

        Debug.Log($"[LevelBuilder] Built {layout.blocks.Count} blocks from layout '{layout.name}'.");
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
