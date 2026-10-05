using System;
using UnityEngine;

/// <summary>
/// One available block size. The generator only reads "size" to know which
/// footprints exist; "prefab" is used by LevelBuilder to know what to instantiate.
/// </summary>
[Serializable]
public class BlockSizeOption
{
    public int size = 1;
    public GameObject prefab;
}
