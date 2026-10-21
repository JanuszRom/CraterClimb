/// <summary>
/// What kind of prefab a placed block record refers to.
/// Standard  - plain square block, prefab looked up by size in the palette.
/// Variant   - a standard block swapped for a special prefab (same footprint), looked up by id.
/// Structure - a shaped prefab (Start, doorway, ...) placed before everything else, looked up by id.
/// </summary>
public enum BlockCategory
{
    Standard,
    Variant,
    Structure
}
