using UnityEngine;


[ExecuteAlways]
public class ProportionalTiling : MonoBehaviour
{
    [SerializeField] private Renderer blockRenderer;
    [SerializeField] private Transform sizeSource;
    [SerializeField] private float textureWorldSize = 1f; // world units one texture tile should cover

    private MaterialPropertyBlock propBlock;

    private void OnEnable()
    {
        Apply();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Apply(); // re-applies whenever you tweak scale or textureWorldSize in the Inspector, while editing
    }
#endif

    private void Apply()
    {
        if (blockRenderer == null || sizeSource == null) return;

        propBlock ??= new MaterialPropertyBlock();

        Vector3 scale = sizeSource.lossyScale; // read the parent's world scale directly

        float tileX = scale.x / textureWorldSize;
        float tileY = scale.z / textureWorldSize;

        propBlock.SetVector("_BaseMap_ST", new Vector4(tileX, tileY, 0, 0));
        propBlock.SetVector("_BumpMap_ST", new Vector4(tileX, tileY, 0, 0));
        blockRenderer.SetPropertyBlock(propBlock);
    }
}

