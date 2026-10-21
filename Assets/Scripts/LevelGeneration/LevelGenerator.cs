using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Generates a logical 2D layout for a climbable wall:
///   1. Place the Start structure, then every target structure (reserving their space first).
///   2. Grow guaranteed-traversable paths from Start to the entry cells of every target.
///   3. Fill remaining empty space with standard blocks (weighted random, larger preferred).
///   4. Swap some standard blocks for variant prefabs (respecting per-level counts and path rules).
///   5. Validate everything; retry with a new seed-derived state on failure.
///
/// Works entirely on lightweight data. No GameObjects are created here.
/// </summary>
public class LevelGenerator : MonoBehaviour
{
    [Header("Grid Size")]
    public int width = 40;
    public int height = 80;

    [Header("Jump Parameters")]
    public JumpSettings jump = new JumpSettings();

    [Header("Palette")]
    [Tooltip("Standard block sizes, structures (Start / targets) and variants. Assign the same asset to LevelBuilder.")]
    public LevelPalette palette;

    [Header("Start")]
    [Tooltip("Structure id from the palette. Leave empty to use a plain block of 'Start Block Size'.")]
    public string startStructureId = "";
    public int startBlockSize = 2;
    [Tooltip("Desired bottom-left cell of the Start's bounding box.")]
    public Vector2Int startDesiredPosition = new Vector2Int(0, 0);
    public int startPositionTolerance = 4;

    [Header("Targets")]
    public List<TargetDefinition> targets = new List<TargetDefinition>();

    [Header("Path Generation")]
    [Tooltip("Later targets continue their path from the previous target's entry platform instead of starting over from Start.")]
    public bool allowSharedTrunk = true;
    [Tooltip("Maximum platforms a single path may grow through before the generator gives up on that attempt.")]
    public int maxStepsPerPath = 200;
    [Tooltip("How many candidate placements to try for a single step before backtracking.")]
    public int maxCandidatesPerStep = 24;
    [Tooltip("How many backtracks a single path may use before the whole attempt is abandoned.")]
    public int maxBacktracksPerPath = 40;

    [Header("Fill Settings")]
    [Tooltip("Fill empty cells in random order instead of a fixed row-by-row scan.")]
    public bool randomizeFillOrder = true;
    [Tooltip("0 = uniform among sizes that fit. Higher values favor larger blocks more strongly.")]
    public float sizeBiasExponent = 2f;
    [Tooltip("Maximum same-size blocks stacked directly on top of each other in one column before a different size is forced.")]
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
    private HashSet<int> pathBlockIds;
    private int nextBlockId;
    private System.Random rng;
    private int generatedStartBlockId = -1;
    private List<LevelTargetData> generatedTargets;
    private List<int> standardSizesDesc;

    // =====================================================================
    // PUBLIC ENTRY POINTS
    // =====================================================================

    [ContextMenu("Generate")]
    public bool Generate()
    {
        lastGenerationSucceeded = false;
        lastFailureReason = "";

        if (!ValidateSetup(out string setupError))
        {
            lastFailureReason = setupError;
            Debug.LogError($"[LevelGenerator] Invalid setup: {setupError}");
            return false;
        }

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
    /// Does not write to disk - use SaveLayoutAssetWithDialog (editor only) for that.
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
    // SETUP VALIDATION
    // =====================================================================

    private bool ValidateSetup(out string error)
    {
        if (palette == null)
        {
            error = "No LevelPalette assigned.";
            return false;
        }

        standardSizesDesc = palette.StandardSizes.OrderByDescending(s => s).ToList();
        if (standardSizesDesc.Count == 0 || standardSizesDesc.Any(s => s < 1))
        {
            error = "The palette needs at least one standard block size, and all sizes must be 1 or larger.";
            return false;
        }

        if (!string.IsNullOrEmpty(startStructureId) && !CheckStructure(startStructureId, "Start", out error))
        {
            return false;
        }

        foreach (TargetDefinition t in targets)
        {
            if (!string.IsNullOrEmpty(t.structureId) && !CheckStructure(t.structureId, $"target '{t.targetId}'", out error))
            {
                return false;
            }
            if (string.IsNullOrEmpty(t.structureId) && t.fallbackBlockSize < 1)
            {
                error = $"Target '{t.targetId}' has an invalid fallback block size.";
                return false;
            }
        }

        foreach (VariantDefinition v in palette.variants)
        {
            if (v == null || string.IsNullOrEmpty(v.id))
            {
                error = "A variant in the palette has no id.";
                return false;
            }
        }

        error = "";
        return true;
    }

    private bool CheckStructure(string id, string owner, out string error)
    {
        StructureDefinition def = palette.GetStructure(id);
        if (def == null)
        {
            error = $"{owner} refers to structure '{id}', which doesn't exist in the palette.";
            return false;
        }
        return def.Validate(out error);
    }

    // =====================================================================
    // GENERATION ATTEMPT
    // =====================================================================

    private void ResetState()
    {
        occupied = new bool[width, height];
        blocks = new List<GeneratedBlockData>();
        blockById = new Dictionary<int, GeneratedBlockData>();
        edges = new List<PathEdgeData>();
        pathBlockIds = new HashSet<int>();
        nextBlockId = 0;
        generatedStartBlockId = -1;
        generatedTargets = new List<LevelTargetData>();
    }

    private bool TryGenerateAttempt(out string failReason)
    {
        // STEP 1: Start (placed before everything else)
        GeneratedBlockData start = PlaceFixedElement(
            startStructureId, startBlockSize, startDesiredPosition,
            startPositionTolerance, startDesiredPosition.x, out string startError);
        if (start == null)
        {
            failReason = $"Could not place Start: {startError}";
            return false;
        }
        generatedStartBlockId = start.id;
        float approachX = GetPlatforms(start)[0].x;

        // STEP 2: every target, so their space is reserved before any path block exists
        var targetBlocks = new List<GeneratedBlockData>();
        foreach (TargetDefinition td in targets)
        {
            GeneratedBlockData tb = PlaceFixedElement(
                td.structureId, td.fallbackBlockSize, td.desiredPosition,
                td.positionTolerance, approachX, out string targetError);
            if (tb == null)
            {
                failReason = $"Could not place target '{td.targetId}': {targetError}";
                return false;
            }
            targetBlocks.Add(tb);
            generatedTargets.Add(new LevelTargetData(td.targetId, tb.id));
        }

        // STEP 3: guaranteed paths
        GeneratedBlockData trunkOrigin = start;
        for (int i = 0; i < targets.Count; i++)
        {
            bool shared = allowSharedTrunk && targets[i].allowSharedTrunk;
            GeneratedBlockData origin = shared ? trunkOrigin : start;

            if (!TryGeneratePathToTarget(origin, targetBlocks[i], out string pathFailReason))
            {
                failReason = $"Path to target '{targets[i].targetId}' failed: {pathFailReason}";
                return false;
            }

            if (shared) trunkOrigin = targetBlocks[i];
        }

        // STEP 4: fill the rest
        FillRemainingSpace();

        // STEP 5: variants (footprint-preserving swaps)
        if (!ApplyVariants(out string variantError))
        {
            failReason = variantError;
            return false;
        }

        // STEP 6: final validation
        if (!ValidateLayout(out string validationError))
        {
            failReason = $"Validation failed: {validationError}";
            return false;
        }

        failReason = "";
        return true;
    }

    // =====================================================================
    // FIXED ELEMENTS (Start / targets): structures or plain fallback blocks
    // =====================================================================

    private GeneratedBlockData PlaceFixedElement(string structureId, int fallbackSize, Vector2Int desired,
                                                 int tolerance, float approachX, out string error)
    {
        error = "";

        if (string.IsNullOrEmpty(structureId))
        {
            foreach (Vector2Int offset in SpiralOffsets(tolerance))
            {
                int px = desired.x + offset.x;
                int py = desired.y + offset.y;
                if (!FitsInBounds(px, py, fallbackSize, fallbackSize)) continue;
                if (!IsAreaFree(px, py, fallbackSize, fallbackSize)) continue;
                return PlaceBlockRaw(fallbackSize, px, py);
            }

            error = $"no free {fallbackSize}x{fallbackSize} space within {tolerance} cells of {desired}.";
            return null;
        }

        StructureDefinition def = palette.GetStructure(structureId);

        foreach (Vector2Int offset in SpiralOffsets(tolerance))
        {
            int px = desired.x + offset.x;
            int py = desired.y + offset.y;
            if (!FitsInBounds(px, py, def.width, def.height)) continue;

            // Pick the orientation whose entry is closest (horizontally) to where the path comes from.
            bool found = false;
            bool bestMirrored = false;
            float bestDist = float.MaxValue;

            int orientationCount = def.allowMirror ? 2 : 1;
            for (int m = 0; m < orientationCount; m++)
            {
                bool mirrored = (m == 1);
                if (!AreCellsFree(px, py, def.GetReservedCells(mirrored))) continue;

                float dist = EntryDistanceX(def, mirrored, px, approachX);
                bool better = !found
                              || dist < bestDist - 0.001f
                              || (Mathf.Abs(dist - bestDist) <= 0.001f && rng.Next(2) == 0);
                if (better)
                {
                    found = true;
                    bestMirrored = mirrored;
                    bestDist = dist;
                }
            }

            if (found) return PlaceStructure(def, px, py, bestMirrored);
        }

        error = $"no free space for structure '{structureId}' ({def.width}x{def.height}) within {tolerance} cells of {desired}.";
        return null;
    }

    private float EntryDistanceX(StructureDefinition def, bool mirrored, int px, float approachX)
    {
        float best = float.MaxValue;
        foreach (Vector2Int e in def.GetEntryCells(mirrored))
        {
            float d = Mathf.Abs((px + e.x + 0.5f) - approachX);
            if (d < best) best = d;
        }
        return best;
    }

    private bool AreCellsFree(int px, int py, IEnumerable<Vector2Int> cells)
    {
        foreach (Vector2Int c in cells)
        {
            if (occupied[px + c.x, py + c.y]) return false;
        }
        return true;
    }

    /// <summary>
    /// The 1x1-style platforms the player can stand on / land on for a block.
    /// For structures these are the entry cells; for everything else it's the block itself.
    /// </summary>
    private List<GeneratedBlockData> GetPlatforms(GeneratedBlockData b)
    {
        var list = new List<GeneratedBlockData>();

        if (b.category == BlockCategory.Structure)
        {
            StructureDefinition def = palette.GetStructure(b.prefabId);
            foreach (Vector2Int e in def.GetEntryCells(b.mirrored))
            {
                list.Add(new GeneratedBlockData(-1, 1, b.x + e.x, b.y + e.y));
            }
        }
        else
        {
            list.Add(b);
        }

        return list;
    }

    // =====================================================================
    // PATH GENERATION
    // =====================================================================

    private bool TryGeneratePathToTarget(GeneratedBlockData originBlock, GeneratedBlockData targetBlock, out string failReason)
    {
        List<GeneratedBlockData> targetPlatforms = GetPlatforms(targetBlock);
        Vector2Int targetPos = new Vector2Int(targetPlatforms[0].x, targetPlatforms[0].y);
        GeneratedBlockData originGeometry = GetPlatforms(originBlock)[0];

        var chainPlaced = new List<GeneratedBlockData>();
        int backtracks = 0;

        for (int step = 0; step < maxStepsPerPath; step++)
        {
            bool hasChain = chainPlaced.Count > 0;
            GeneratedBlockData currentGeometry = hasChain ? chainPlaced[chainPlaced.Count - 1] : originGeometry;
            int currentId = hasChain ? chainPlaced[chainPlaced.Count - 1].id : originBlock.id;

            // Close enough to jump onto one of the target's entry platforms?
            if (CanReachAny(currentGeometry, targetPlatforms))
            {
                LinkEdge(currentId, targetBlock.id);
                failReason = "";
                return true;
            }

            if (TryGrowStep(currentGeometry, targetPos, out GeneratedBlockData next))
            {
                LinkEdge(currentId, next.id);
                pathBlockIds.Add(next.id);
                chainPlaced.Add(next);
            }
            else
            {
                backtracks++;
                if (backtracks > maxBacktracksPerPath || chainPlaced.Count == 0)
                {
                    failReason = "Exceeded backtrack limit while growing path.";
                    return false;
                }

                GeneratedBlockData failedNode = chainPlaced[chainPlaced.Count - 1];
                chainPlaced.RemoveAt(chainPlaced.Count - 1);
                RemoveBlock(failedNode);
            }
        }

        failReason = $"Exceeded max steps ({maxStepsPerPath}) without reaching the target.";
        return false;
    }

    private bool TryGrowStep(GeneratedBlockData from, Vector2Int targetPos, out GeneratedBlockData placed)
    {
        for (int i = 0; i < maxCandidatesPerStep; i++)
        {
            int size = standardSizesDesc[rng.Next(standardSizesDesc.Count)];

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

            if (!FitsInBounds(candidateX, candidateY, size, size)) continue;
            if (!IsAreaFree(candidateX, candidateY, size, size)) continue;

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

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // =====================================================================
    // JUMP MODEL
    // Replace IsReachable later with a real ballistic/arc calculation if needed.
    // =====================================================================

    public static bool IsReachable(GeneratedBlockData from, GeneratedBlockData to, JumpSettings settings)
    {
        // Horizontal distance: closest edge to closest edge, not center to center.
        float horizontalGap = Mathf.Max(0, Mathf.Max(to.x - from.Right, from.x - to.Right));

        // maxJumpDistance applies to both upward and downward jumps.
        if (horizontalGap > settings.maxJumpDistance) return false;

        float verticalDiff = to.Top - from.Top;
        if (verticalDiff > 0)
        {
            return verticalDiff <= settings.maxJumpHeight;
        }
        return -verticalDiff <= settings.maxDropHeight;
    }

    private bool CanReachAny(GeneratedBlockData from, List<GeneratedBlockData> toPlatforms)
    {
        foreach (GeneratedBlockData to in toPlatforms)
        {
            if (IsReachable(from, to, jump)) return true;
        }
        return false;
    }

    private bool CanReachAnyPair(List<GeneratedBlockData> fromPlatforms, List<GeneratedBlockData> toPlatforms)
    {
        foreach (GeneratedBlockData from in fromPlatforms)
        {
            if (CanReachAny(from, toPlatforms)) return true;
        }
        return false;
    }

    // =====================================================================
    // FILL REMAINING SPACE
    // =====================================================================

    private void FillRemainingSpace()
    {
        // Tracks, per X column, the size and run-length of same-size blocks stacked directly
        // on top of each other, so straight vertical columns get broken up.
        var columnRun = new Dictionary<int, (int size, int count)>();

        foreach (Vector2Int cell in BuildCellScanOrder())
        {
            int x = cell.x;
            int y = cell.y;
            if (occupied[x, y]) continue;

            var validSizes = new List<int>();
            foreach (int size in standardSizesDesc)
            {
                if (!FitsInBounds(x, y, size, size)) continue;
                if (!IsAreaFree(x, y, size, size)) continue;
                validSizes.Add(size);
            }

            if (validSizes.Count == 0) continue;

            int chosenSize = PickWeightedSize(validSizes, x, columnRun);
            PlaceBlockRaw(chosenSize, x, y);
            UpdateColumnRun(x, chosenSize, columnRun);
        }
    }

    private List<Vector2Int> BuildCellScanOrder()
    {
        var cells = new List<Vector2Int>(width * height);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                cells.Add(new Vector2Int(x, y));

        if (randomizeFillOrder) Shuffle(cells);
        return cells;
    }

    private int PickWeightedSize(List<int> validSizes, int x, Dictionary<int, (int size, int count)> columnRun)
    {
        List<int> candidates = validSizes;

        if (maxSameSizeVerticalRun > 0 && columnRun.TryGetValue(x, out var run))
        {
            List<int> filtered = validSizes.Where(s => !(s == run.size && run.count >= maxSameSizeVerticalRun)).ToList();
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
    // VARIANTS
    // Swaps standard blocks for variant prefabs. The footprint never changes,
    // so reachability is unaffected even when a variant sits on a path.
    // =====================================================================

    private bool ApplyVariants(out string error)
    {
        error = "";

        foreach (VariantDefinition v in palette.variants)
        {
            int min = Mathf.Max(0, v.minPerLevel);
            int max = Mathf.Max(min, v.maxPerLevel);

            List<GeneratedBlockData> eligible = blocks
                .Where(b => b.category == BlockCategory.Standard
                            && b.width == v.size
                            && (v.allowedOnPaths || !pathBlockIds.Contains(b.id)))
                .ToList();

            if (eligible.Count < min)
            {
                string where = v.allowedOnPaths ? "" : " off the paths";
                error = $"Variant '{v.id}' needs at least {min} eligible {v.size}x{v.size} blocks{where}, but only {eligible.Count} exist.";
                return false;
            }

            int count = Mathf.Min(rng.Next(min, max + 1), eligible.Count);
            Shuffle(eligible);

            for (int i = 0; i < count; i++)
            {
                eligible[i].category = BlockCategory.Variant;
                eligible[i].prefabId = v.id;
            }
        }

        return true;
    }

    // =====================================================================
    // GRID / BLOCK HELPERS
    // =====================================================================

    private bool FitsInBounds(int x, int y, int w, int h) =>
        x >= 0 && y >= 0 && x + w <= width && y + h <= height;

    private bool IsAreaFree(int x, int y, int w, int h)
    {
        for (int dx = 0; dx < w; dx++)
            for (int dy = 0; dy < h; dy++)
                if (occupied[x + dx, y + dy]) return false;
        return true;
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

    private GeneratedBlockData PlaceStructure(StructureDefinition def, int x, int y, bool mirrored)
    {
        var block = new GeneratedBlockData(nextBlockId++, BlockCategory.Structure, def.id,
                                           x, y, def.width, def.height, mirrored, 1);
        blocks.Add(block);
        blockById[block.id] = block;

        foreach (Vector2Int c in def.GetReservedCells(mirrored))
        {
            occupied[x + c.x, y + c.y] = true;
        }

        return block;
    }

    /// <summary>Removes a standard block (used when backtracking a path).</summary>
    private void RemoveBlock(GeneratedBlockData block)
    {
        blocks.Remove(block);
        blockById.Remove(block.id);
        pathBlockIds.Remove(block.id);
        edges.RemoveAll(e => e.fromBlockId == block.id || e.toBlockId == block.id);

        for (int dx = 0; dx < block.width; dx++)
            for (int dy = 0; dy < block.height; dy++)
                occupied[block.x + dx, block.y + dy] = false;
    }

    private void LinkEdge(int fromId, int toId)
    {
        edges.Add(new PathEdgeData(fromId, toId));
    }

    /// <summary>Cells a block actually occupies, relative to its bottom-left corner.</summary>
    private IEnumerable<Vector2Int> GetOccupiedCells(GeneratedBlockData b)
    {
        if (b.category == BlockCategory.Structure)
        {
            StructureDefinition def = palette.GetStructure(b.prefabId);
            foreach (Vector2Int c in def.GetReservedCells(b.mirrored))
            {
                yield return c;
            }
            yield break;
        }

        for (int dx = 0; dx < b.width; dx++)
            for (int dy = 0; dy < b.height; dy++)
                yield return new Vector2Int(dx, dy);
    }

    // =====================================================================
    // VALIDATION
    // =====================================================================

    private bool ValidateLayout(out string error)
    {
        // Bounds + overlap, rebuilt independently of the live grid as a safety net.
        bool[,] check = new bool[width, height];
        foreach (GeneratedBlockData b in blocks)
        {
            if (!FitsInBounds(b.x, b.y, b.width, b.height))
            {
                error = $"Block {b.id} is out of bounds.";
                return false;
            }

            foreach (Vector2Int c in GetOccupiedCells(b))
            {
                int cx = b.x + c.x;
                int cy = b.y + c.y;
                if (check[cx, cy])
                {
                    error = $"Overlap detected at ({cx},{cy}) involving block {b.id}.";
                    return false;
                }
                check[cx, cy] = true;
            }
        }

        // Variants must respect their on-path rule.
        foreach (GeneratedBlockData b in blocks)
        {
            if (b.category != BlockCategory.Variant) continue;

            VariantDefinition vd = palette.GetVariant(b.prefabId);
            if (vd == null)
            {
                error = $"Block {b.id} uses unknown variant '{b.prefabId}'.";
                return false;
            }
            if (!vd.allowedOnPaths && pathBlockIds.Contains(b.id))
            {
                error = $"Variant '{vd.id}' was placed on a path block ({b.id}), which it isn't allowed to.";
                return false;
            }
        }

        // Every path edge must be a valid jump.
        foreach (PathEdgeData e in edges)
        {
            if (!blockById.TryGetValue(e.fromBlockId, out GeneratedBlockData from)
                || !blockById.TryGetValue(e.toBlockId, out GeneratedBlockData to))
            {
                error = $"Path edge {e.fromBlockId} -> {e.toBlockId} refers to a missing block.";
                return false;
            }

            if (!CanReachAnyPair(GetPlatforms(from), GetPlatforms(to)))
            {
                error = $"Path edge {e.fromBlockId} -> {e.toBlockId} is not a valid jump.";
                return false;
            }
        }

        // Every target must be reachable from Start over the path graph.
        var adjacency = new Dictionary<int, List<int>>();
        foreach (PathEdgeData e in edges)
        {
            if (!adjacency.ContainsKey(e.fromBlockId)) adjacency[e.fromBlockId] = new List<int>();
            adjacency[e.fromBlockId].Add(e.toBlockId);
        }

        foreach (LevelTargetData target in generatedTargets)
        {
            if (!BfsReaches(generatedStartBlockId, target.blockId, adjacency))
            {
                error = $"Target '{target.targetId}' (block {target.blockId}) is not reachable from Start.";
                return false;
            }
        }

        error = "";
        return true;
    }

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
    // Grey/blue/green/yellow/red = standard sizes, orange = variants,
    // cyan = structures (yellow boxes mark their entry cells),
    // yellow lines = path edges, green circle = Start, red circles = targets.
    // =====================================================================

#if UNITY_EDITOR
    [Header("Gizmo Settings")]
    public bool drawGizmos = true;
    public float gizmoCellSize = 1f;

    private Vector3 GizmoPoint(float cellX, float cellY, float z = 0f) =>
        transform.position + new Vector3(cellX * gizmoCellSize, cellY * gizmoCellSize, z);

    private void DrawCellCube(float x, float y, float w, float h)
    {
        Gizmos.DrawCube(GizmoPoint(x + w * 0.5f, y + h * 0.5f),
                        new Vector3(w * gizmoCellSize * 0.95f, h * gizmoCellSize * 0.95f, 0.1f));
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.white;
        Vector3 boundsSize = new Vector3(width * gizmoCellSize, height * gizmoCellSize, 0.1f);
        Gizmos.DrawWireCube(transform.position + boundsSize * 0.5f, boundsSize);

        if (blocks == null || blockById == null || palette == null) return;

        foreach (GeneratedBlockData b in blocks)
        {
            switch (b.category)
            {
                case BlockCategory.Structure:
                    Gizmos.color = new Color(0.2f, 0.9f, 0.9f);
                    foreach (Vector2Int c in GetOccupiedCells(b))
                    {
                        DrawCellCube(b.x + c.x, b.y + c.y, 1, 1);
                    }
                    Gizmos.color = Color.yellow;
                    foreach (GeneratedBlockData p in GetPlatforms(b))
                    {
                        Gizmos.DrawWireCube(GizmoPoint(p.x + 0.5f, p.y + 0.5f, -0.1f),
                                            new Vector3(gizmoCellSize, gizmoCellSize, 0.2f));
                    }
                    break;

                case BlockCategory.Variant:
                    Gizmos.color = new Color(1f, 0.5f, 0f);
                    DrawCellCube(b.x, b.y, b.width, b.height);
                    break;

                default:
                    Gizmos.color = ColorForSize(b.width);
                    DrawCellCube(b.x, b.y, b.width, b.height);
                    break;
            }
        }

        if (edges != null)
        {
            Gizmos.color = Color.yellow;
            foreach (PathEdgeData e in edges)
            {
                if (!blockById.TryGetValue(e.fromBlockId, out GeneratedBlockData from)) continue;
                if (!blockById.TryGetValue(e.toBlockId, out GeneratedBlockData to)) continue;

                Vector3 a = GizmoPoint(from.x + from.width * 0.5f, from.y + from.height * 0.5f, -0.2f);
                Vector3 bPos = GizmoPoint(to.x + to.width * 0.5f, to.y + to.height * 0.5f, -0.2f);
                Gizmos.DrawLine(a, bPos);
            }
        }

        if (generatedStartBlockId >= 0 && blockById.TryGetValue(generatedStartBlockId, out GeneratedBlockData s))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(GizmoPoint(s.x + s.width * 0.5f, s.y + s.height * 0.5f, -0.3f),
                                  Mathf.Max(s.width, s.height) * gizmoCellSize * 0.6f);
        }

        if (generatedTargets != null)
        {
            Gizmos.color = Color.red;
            foreach (LevelTargetData t in generatedTargets)
            {
                if (!blockById.TryGetValue(t.blockId, out GeneratedBlockData tb)) continue;
                Gizmos.DrawWireSphere(GizmoPoint(tb.x + tb.width * 0.5f, tb.y + tb.height * 0.5f, -0.3f),
                                      Mathf.Max(tb.width, tb.height) * gizmoCellSize * 0.6f);
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
