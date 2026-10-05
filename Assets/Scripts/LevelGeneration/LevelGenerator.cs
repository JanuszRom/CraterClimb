using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Generates a logical 2D layout for a climbable wall:
///   1. Place Start.
///   2. Grow guaranteed-traversable paths from Start to every required target.
///   3. Fill remaining empty space with blocks (largest-first).
///   4. Validate the whole thing; retry with a new seed-derived state on failure.
///
/// Operates entirely on lightweight data (no GameObjects, no physics) until
/// BuildLayoutAsset() packages the accepted result for LevelBuilder to use.
/// </summary>
public class LevelGenerator : MonoBehaviour
{
    [Header("Grid Size")]
    public int width = 40;
    public int height = 80;

    [Header("Jump Parameters")]
    public JumpSettings jump = new JumpSettings();

    [Header("Available Block Sizes")]
    [Tooltip("Only 'size' is used by generation logic. 'prefab' is optional here and purely for your convenience; LevelBuilder has its own prefab mapping.")]
    public List<BlockSizeOption> blockSizeOptions = new List<BlockSizeOption>
    {
        new BlockSizeOption{ size = 1 },
        new BlockSizeOption{ size = 2 },
        new BlockSizeOption{ size = 4 },
    };

    [Header("Start Platform")]
    public Vector2Int startDesiredPosition = new Vector2Int(0, 0);
    public int startBlockSize = 2;

    [Header("Targets")]
    public List<TargetDefinition> targets = new List<TargetDefinition>();

    [Header("Path Generation")]
    [Tooltip("Allow different targets' paths to reuse the same early platforms before branching off.")]
    public bool allowSharedTrunk = true;
    [Tooltip("Maximum platforms a single path may grow through before the generator gives up on that attempt.")]
    public int maxStepsPerPath = 200;
    [Tooltip("How many candidate placements to try for a single step before backtracking.")]
    public int maxCandidatesPerStep = 24;
    [Tooltip("How many backtracks a single path may use before the whole attempt is abandoned.")]
    public int maxBacktracksPerPath = 40;

    [Header("Fill Settings")]
    [Tooltip("If true, empty cells are filled in random order instead of a fixed row-by-row scan. Strongly recommended - a fixed scan order tends to produce repeating vertical columns of the same block size.")]
    public bool randomizeFillOrder = true;
    [Tooltip("Controls how strongly filling favors larger blocks. 0 = pick uniformly at random among all sizes that fit. Higher values (e.g. 2-4) increasingly favor the largest size that fits at each cell. 'Always pick the single biggest' (the old behavior) corresponds to a very high value.")]
    public float sizeBiasExponent = 2f;
    [Tooltip("Maximum number of same-size blocks allowed to stack directly on top of one another at the same horizontal position before the generator is forced to pick a different size there. This is what prevents a straight, no-sideways-movement climbing column.")]
    public int maxSameSizeVerticalRun = 2;

    [Header("Randomness")]
    public int seed = 12345;
    public int maxGenerationAttempts = 10;

    [Header("Result (read-only)")]
    [SerializeField] private bool lastGenerationSucceeded;
    [SerializeField] private string lastFailureReason = "";

    public bool LastGenerationSucceeded => lastGenerationSucceeded;
    public string LastFailureReason => lastFailureReason;

    // --- runtime generation state ---
    private bool[,] occupied;
    private List<GeneratedBlockData> blocks;
    private Dictionary<int, GeneratedBlockData> blockById;
    private List<PathEdgeData> edges;
    private int nextBlockId;
    private System.Random rng;
    private int generatedStartBlockId = -1;
    private List<LevelTargetData> generatedTargets;

    private List<int> SortedSizesDescending =>
        blockSizeOptions.Select(b => b.size).Distinct().OrderByDescending(s => s).ToList();

    // =====================================================================
    // PUBLIC ENTRY POINTS
    // =====================================================================

    [ContextMenu("Generate")]
    public bool Generate()
    {
        lastGenerationSucceeded = false;
        lastFailureReason = "";

        for (int attempt = 0; attempt < maxGenerationAttempts; attempt++)
        {
            rng = new System.Random(seed + attempt);
            ResetState();

            if (!TryGenerateAttempt(out string failReason))
            {
                lastFailureReason = $"Attempt {attempt + 1}/{maxGenerationAttempts} failed: {failReason}";
                continue;
            }

            lastGenerationSucceeded = true;
            lastFailureReason = "";
            Debug.Log($"[LevelGenerator] Succeeded on attempt {attempt + 1}/{maxGenerationAttempts}.");
            return true;
        }

        Debug.LogError($"[LevelGenerator] Generation failed after {maxGenerationAttempts} attempts. Last reason: {lastFailureReason}");
        return false;
    }

    [ContextMenu("Randomize Seed")]
    public void RandomizeSeed()
    {
        seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
    }

    /// <summary>
    /// Packages the last successful generation result into a LevelLayout instance.
    /// Does not write to disk - call SaveLayoutAssetWithDialog (editor only) for that.
    /// </summary>
    public LevelLayout BuildLayoutAsset()
    {
        if (!lastGenerationSucceeded)
        {
            Debug.LogWarning("[LevelGenerator] BuildLayoutAsset called without a successful generation. Returning null.");
            return null;
        }

        LevelLayout layout = ScriptableObject.CreateInstance<LevelLayout>();
        layout.width = width;
        layout.height = height;
        layout.seed = seed;
        layout.blocks = new List<GeneratedBlockData>(blocks);
        layout.startBlockId = generatedStartBlockId;
        layout.targets = new List<LevelTargetData>(generatedTargets);
        layout.pathEdges = new List<PathEdgeData>(edges);
        return layout;
    }

#if UNITY_EDITOR
    [ContextMenu("Save Layout Asset...")]
    public void SaveLayoutAssetWithDialog()
    {
        LevelLayout layout = BuildLayoutAsset();
        if (layout == null) return;

        string path = UnityEditor.EditorUtility.SaveFilePanelInProject(
            "Save Level Layout", "NewLevelLayout", "asset", "Choose where to save the generated layout.");
        if (string.IsNullOrEmpty(path)) return;

        UnityEditor.AssetDatabase.CreateAsset(layout, path);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log($"[LevelGenerator] Saved layout to {path}");
    }
#endif

    // =====================================================================
    // GENERATION ATTEMPT
    // =====================================================================

    private void ResetState()
    {
        occupied = new bool[width, height];
        blocks = new List<GeneratedBlockData>();
        blockById = new Dictionary<int, GeneratedBlockData>();
        edges = new List<PathEdgeData>();
        nextBlockId = 0;
        generatedStartBlockId = -1;
        generatedTargets = new List<LevelTargetData>();
    }

    private bool TryGenerateAttempt(out string failReason)
    {
        // STEP 2: place Start
        GeneratedBlockData start = PlaceBlock(startBlockSize, startDesiredPosition.x, startDesiredPosition.y, clampToBounds: true);
        if (start == null)
        {
            failReason = "Could not place Start platform.";
            return false;
        }
        generatedStartBlockId = start.id;

        // STEP 3 + 4: generate guaranteed paths to every target
        GeneratedBlockData trunkTip = start;

        foreach (TargetDefinition targetDef in targets)
        {
            GeneratedBlockData branchOrigin = (allowSharedTrunk && targetDef.allowSharedTrunk) ? trunkTip : start;

            if (!TryGeneratePathToTarget(branchOrigin, targetDef, out GeneratedBlockData targetBlock, out string pathFailReason))
            {
                failReason = $"Path to target '{targetDef.targetId}' failed: {pathFailReason}";
                return false;
            }

            generatedTargets.Add(new LevelTargetData(targetDef.targetId, targetBlock.id));

            if (allowSharedTrunk && targetDef.allowSharedTrunk)
            {
                trunkTip = targetBlock;
            }
        }

        // STEP 5: fill remaining space
        FillRemainingSpace();

        // Final validation
        if (!ValidateLayout(out string validationError))
        {
            failReason = $"Validation failed after fill: {validationError}";
            return false;
        }

        failReason = "";
        return true;
    }

    // =====================================================================
    // PATH GENERATION
    // =====================================================================

    private bool TryGeneratePathToTarget(GeneratedBlockData from, TargetDefinition targetDef, out GeneratedBlockData targetBlock, out string failReason)
    {
        GeneratedBlockData current = from;
        int backtracks = 0;
        List<GeneratedBlockData> chainPlaced = new List<GeneratedBlockData>();

        for (int step = 0; step < maxStepsPerPath; step++)
        {
            if (TryPlaceTargetHere(current, targetDef, out targetBlock))
            {
                LinkEdge(current.id, targetBlock.id);
                failReason = null;
                return true;
            }

            if (TryGrowStep(current, targetDef.desiredPosition, out GeneratedBlockData next))
            {
                LinkEdge(current.id, next.id);
                chainPlaced.Add(next);
                current = next;
            }
            else
            {
                backtracks++;
                if (backtracks > maxBacktracksPerPath || chainPlaced.Count == 0)
                {
                    failReason = "Exceeded backtrack limit while growing path.";
                    targetBlock = null;
                    return false;
                }

                GeneratedBlockData failedNode = chainPlaced[chainPlaced.Count - 1];
                chainPlaced.RemoveAt(chainPlaced.Count - 1);
                RemoveBlock(failedNode);
                current = chainPlaced.Count > 0 ? chainPlaced[chainPlaced.Count - 1] : from;
            }
        }

        failReason = $"Exceeded max steps ({maxStepsPerPath}) without reaching target.";
        targetBlock = null;
        return false;
    }

    private bool TryPlaceTargetHere(GeneratedBlockData current, TargetDefinition targetDef, out GeneratedBlockData targetBlock)
    {
        foreach (Vector2Int offset in SpiralOffsets(targetDef.positionTolerance))
        {
            int tx = targetDef.desiredPosition.x + offset.x;
            int ty = targetDef.desiredPosition.y + offset.y;

            foreach (int size in SortedSizesDescending)
            {
                if (!FitsInBounds(tx, ty, size)) continue;
                if (!IsAreaFree(tx, ty, size)) continue;

                var candidate = new GeneratedBlockData(-1, size, tx, ty);
                if (!IsReachable(current, candidate, jump)) continue;

                targetBlock = PlaceBlockRaw(size, tx, ty);
                return true;
            }
        }

        targetBlock = null;
        return false;
    }

    private bool TryGrowStep(GeneratedBlockData from, Vector2Int targetPos, out GeneratedBlockData placed)
    {
        for (int i = 0; i < maxCandidatesPerStep; i++)
        {
            int size = SortedSizesDescending[rng.Next(SortedSizesDescending.Count)];

            bool preferUp = (targetPos.y > from.Top);
            float dy = preferUp
                ? RandomRange(0.5f, jump.maxJumpHeight)
                : RandomRange(-jump.maxDropHeight, jump.maxJumpHeight);
            int candidateTop = Mathf.RoundToInt(from.Top + dy);
            int candidateY = candidateTop - size;

            bool preferRight = (targetPos.x >= from.x);
            bool goRight = (rng.NextDouble() < 0.75) ? preferRight : !preferRight;
            float gap = RandomRange(0f, jump.maxJumpDistance);
            int candidateX = goRight
                ? from.Right + Mathf.RoundToInt(gap)
                : from.x - size - Mathf.RoundToInt(gap);

            if (!FitsInBounds(candidateX, candidateY, size)) continue;
            if (!IsAreaFree(candidateX, candidateY, size)) continue;

            var candidate = new GeneratedBlockData(-1, size, candidateX, candidateY);
            if (!IsReachable(from, candidate, jump)) continue;

            placed = PlaceBlockRaw(size, candidateX, candidateY);
            return true;
        }

        placed = null;
        return false;
    }

    private IEnumerable<Vector2Int> SpiralOffsets(int tolerance)
    {
        yield return Vector2Int.zero;
        for (int r = 1; r <= tolerance; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                yield return new Vector2Int(dx, r);
                yield return new Vector2Int(dx, -r);
            }
            for (int dy = -r + 1; dy <= r - 1; dy++)
            {
                yield return new Vector2Int(r, dy);
                yield return new Vector2Int(-r, dy);
            }
        }
    }

    private float RandomRange(float min, float max) => (float)(min + rng.NextDouble() * (max - min));

    // =====================================================================
    // JUMP MODEL
    // Replace this single function later with a real ballistic/arc calculation
    // if the simple box-distance approximation isn't accurate enough.
    // =====================================================================

    public static bool IsReachable(GeneratedBlockData from, GeneratedBlockData to, JumpSettings settings)
    {
        // Horizontal distance: closest edge to closest edge, not center to center.
        float horizontalGap = Mathf.Max(0,
            Mathf.Max(to.x - from.Right, from.x - to.Right));

        // maxJumpDistance applies to both upward and downward jumps.
        if (horizontalGap > settings.maxJumpDistance) return false;

        float verticalDiff = to.Top - from.Top;
        if (verticalDiff > 0)
        {
            return verticalDiff <= settings.maxJumpHeight;
        }
        else
        {
            return -verticalDiff <= settings.maxDropHeight;
        }
    }

    // =====================================================================
    // FILL REMAINING SPACE
    // =====================================================================

    private void FillRemainingSpace()
    {
        List<int> sizesDesc = SortedSizesDescending;

        // Tracks, per X column, the size and run-length of same-size blocks stacked
        // directly on top of each other, so we can break up straight vertical columns.
        var columnRun = new Dictionary<int, (int size, int count)>();

        List<Vector2Int> cells = BuildCellScanOrder();

        foreach (Vector2Int cell in cells)
        {
            int x = cell.x;
            int y = cell.y;
            if (occupied[x, y]) continue;

            List<int> validSizes = new List<int>();
            foreach (int size in sizesDesc)
            {
                if (!FitsInBounds(x, y, size)) continue;
                if (!IsAreaFree(x, y, size)) continue;
                validSizes.Add(size);
            }

            if (validSizes.Count == 0) continue;

            int chosenSize = PickWeightedSize(validSizes, x, columnRun);

            PlaceBlockRaw(chosenSize, x, y);
            UpdateColumnRun(x, chosenSize, columnRun);
        }
    }

    /// <summary>
    /// Returns every grid cell once, in random order if randomizeFillOrder is enabled
    /// (Fisher-Yates shuffle, using the generator's seeded RNG so results stay reproducible),
    /// otherwise in a plain row-by-row scan.
    /// </summary>
    private List<Vector2Int> BuildCellScanOrder()
    {
        var cells = new List<Vector2Int>(width * height);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                cells.Add(new Vector2Int(x, y));

        if (!randomizeFillOrder) return cells;

        for (int i = cells.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (cells[i], cells[j]) = (cells[j], cells[i]);
        }
        return cells;
    }

    /// <summary>
    /// Picks one of the valid sizes using a weighted random choice biased toward
    /// larger sizes (weight = size ^ sizeBiasExponent). If picking a given size would
    /// extend a same-size vertical run at this column past maxSameSizeVerticalRun,
    /// that size is excluded from consideration (falling back to a smaller one, or to
    /// the only option left if just one size fits).
    /// </summary>
    private int PickWeightedSize(List<int> validSizes, int x, Dictionary<int, (int size, int count)> columnRun)
    {
        List<int> candidates = validSizes;

        if (maxSameSizeVerticalRun > 0 && columnRun.TryGetValue(x, out var run))
        {
            var filtered = validSizes.Where(s => !(s == run.size && run.count >= maxSameSizeVerticalRun)).ToList();
            if (filtered.Count > 0) candidates = filtered;
        }

        if (candidates.Count == 1) return candidates[0];

        double totalWeight = 0;
        var weights = new double[candidates.Count];
        for (int i = 0; i < candidates.Count; i++)
        {
            weights[i] = Mathf.Pow(candidates[i], sizeBiasExponent);
            totalWeight += weights[i];
        }

        double roll = rng.NextDouble() * totalWeight;
        double cumulative = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative) return candidates[i];
        }

        return candidates[candidates.Count - 1];
    }

    private void UpdateColumnRun(int x, int chosenSize, Dictionary<int, (int size, int count)> columnRun)
    {
        if (columnRun.TryGetValue(x, out var run) && run.size == chosenSize)
        {
            columnRun[x] = (chosenSize, run.count + 1);
        }
        else
        {
            columnRun[x] = (chosenSize, 1);
        }
    }

    // =====================================================================
    // GRID / BLOCK HELPERS
    // =====================================================================

    private bool FitsInBounds(int x, int y, int size) =>
        x >= 0 && y >= 0 && x + size <= width && y + size <= height;

    private bool IsAreaFree(int x, int y, int size)
    {
        for (int dx = 0; dx < size; dx++)
            for (int dy = 0; dy < size; dy++)
                if (occupied[x + dx, y + dy]) return false;
        return true;
    }

    private GeneratedBlockData PlaceBlock(int size, int x, int y, bool clampToBounds)
    {
        if (clampToBounds)
        {
            x = Mathf.Clamp(x, 0, Mathf.Max(0, width - size));
            y = Mathf.Clamp(y, 0, Mathf.Max(0, height - size));
        }

        if (!FitsInBounds(x, y, size) || !IsAreaFree(x, y, size)) return null;
        return PlaceBlockRaw(size, x, y);
    }

    private GeneratedBlockData PlaceBlockRaw(int size, int x, int y)
    {
        var block = new GeneratedBlockData(nextBlockId++, size, x, y);
        blocks.Add(block);
        blockById[block.id] = block;

        for (int dx = 0; dx < size; dx++)
            for (int dy = 0; dy < size; dy++)
                occupied[x + dx, y + dy] = true;

        return block;
    }

    private void RemoveBlock(GeneratedBlockData block)
    {
        blocks.Remove(block);
        blockById.Remove(block.id);
        edges.RemoveAll(e => e.fromBlockId == block.id || e.toBlockId == block.id);

        for (int dx = 0; dx < block.size; dx++)
            for (int dy = 0; dy < block.size; dy++)
                occupied[block.x + dx, block.y + dy] = false;
    }

    private void LinkEdge(int fromId, int toId)
    {
        edges.Add(new PathEdgeData(fromId, toId));
    }

    // =====================================================================
    // VALIDATION
    // =====================================================================

    private bool ValidateLayout(out string error)
    {
        bool[,] check = new bool[width, height];
        foreach (var b in blocks)
        {
            if (!FitsInBoundsStatic(b, width, height))
            {
                error = $"Block {b.id} is out of bounds.";
                return false;
            }
            for (int dx = 0; dx < b.size; dx++)
                for (int dy = 0; dy < b.size; dy++)
                {
                    int cx = b.x + dx, cy = b.y + dy;
                    if (check[cx, cy])
                    {
                        error = $"Overlap detected at ({cx},{cy}) involving block {b.id}.";
                        return false;
                    }
                    check[cx, cy] = true;
                }
        }

        var adjacency = new Dictionary<int, List<int>>();
        foreach (var e in edges)
        {
            if (!adjacency.ContainsKey(e.fromBlockId)) adjacency[e.fromBlockId] = new List<int>();
            adjacency[e.fromBlockId].Add(e.toBlockId);
        }

        foreach (var target in generatedTargets)
        {
            if (!BfsReaches(generatedStartBlockId, target.blockId, adjacency))
            {
                error = $"Target '{target.targetId}' (block {target.blockId}) is not reachable from Start via guaranteed path edges.";
                return false;
            }
        }

        error = "";
        return true;
    }

    private static bool FitsInBoundsStatic(GeneratedBlockData b, int width, int height) =>
        b.x >= 0 && b.y >= 0 && b.x + b.size <= width && b.y + b.size <= height;

    private bool BfsReaches(int startId, int targetId, Dictionary<int, List<int>> adjacency)
    {
        if (startId == targetId) return true;

        var visited = new HashSet<int> { startId };
        var queue = new Queue<int>();
        queue.Enqueue(startId);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            if (!adjacency.TryGetValue(current, out List<int> neighbors)) continue;

            foreach (int n in neighbors)
            {
                if (n == targetId) return true;
                if (visited.Add(n)) queue.Enqueue(n);
            }
        }
        return false;
    }

    // =====================================================================
    // GIZMOS (editor visualization only - not part of runtime generation)
    // =====================================================================

#if UNITY_EDITOR
    [Header("Gizmo Settings")]
    public bool drawGizmos = true;
    public float gizmoCellSize = 1f;

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.white;
        Vector3 origin = transform.position;
        Vector3 boundsSize = new Vector3(width * gizmoCellSize, height * gizmoCellSize, 0.1f);
        Gizmos.DrawWireCube(origin + boundsSize * 0.5f, boundsSize);

        if (blocks == null) return;

        foreach (var b in blocks)
        {
            Gizmos.color = ColorForSize(b.size);
            Vector3 pos = origin + new Vector3(
                (b.x + b.size * 0.5f) * gizmoCellSize,
                (b.y + b.size * 0.5f) * gizmoCellSize,
                0f);
            Vector3 cubeSize = new Vector3(b.size * gizmoCellSize * 0.95f, b.size * gizmoCellSize * 0.95f, 0.1f);
            Gizmos.DrawCube(pos, cubeSize);
        }

        if (edges != null && blockById != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var e in edges)
            {
                if (!blockById.ContainsKey(e.fromBlockId) || !blockById.ContainsKey(e.toBlockId)) continue;
                var from = blockById[e.fromBlockId];
                var to = blockById[e.toBlockId];
                Vector3 a = origin + new Vector3((from.x + from.size * 0.5f) * gizmoCellSize, (from.y + from.size * 0.5f) * gizmoCellSize, -0.2f);
                Vector3 bPos = origin + new Vector3((to.x + to.size * 0.5f) * gizmoCellSize, (to.y + to.size * 0.5f) * gizmoCellSize, -0.2f);
                Gizmos.DrawLine(a, bPos);
            }
        }

        if (generatedStartBlockId >= 0 && blockById != null && blockById.ContainsKey(generatedStartBlockId))
        {
            Gizmos.color = Color.green;
            var s = blockById[generatedStartBlockId];
            Vector3 pos = origin + new Vector3((s.x + s.size * 0.5f) * gizmoCellSize, (s.y + s.size * 0.5f) * gizmoCellSize, -0.3f);
            Gizmos.DrawWireSphere(pos, s.size * gizmoCellSize * 0.6f);
        }

        if (generatedTargets != null && blockById != null)
        {
            Gizmos.color = Color.red;
            foreach (var t in generatedTargets)
            {
                if (!blockById.ContainsKey(t.blockId)) continue;
                var tb = blockById[t.blockId];
                Vector3 pos = origin + new Vector3((tb.x + tb.size * 0.5f) * gizmoCellSize, (tb.y + tb.size * 0.5f) * gizmoCellSize, -0.3f);
                Gizmos.DrawWireSphere(pos, tb.size * gizmoCellSize * 0.6f);
            }
        }
    }

    private Color ColorForSize(int size)
    {
        switch (size)
        {
            case 1: return new Color(0.6f, 0.6f, 0.6f);
            case 2: return new Color(0.3f, 0.6f, 1f);
            case 4: return new Color(0.3f, 1f, 0.4f);
            case 6: return new Color(1f, 0.8f, 0.2f);
            case 8: return new Color(1f, 0.3f, 0.3f);
            default: return Color.magenta;
        }
    }
#endif
}
