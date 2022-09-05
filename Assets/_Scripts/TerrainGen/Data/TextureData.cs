using System.Linq;
using UnityEngine;

[CreateAssetMenu()]
public class TextureData : UpdatableData
{
    private const int textureSize = 512;
    private const TextureFormat textureFormat = TextureFormat.RGB565;
    public Layer[] layers;

    private float savedMinHeight;
    private float savedMaxHeight;

    public void ApplyToMaterial(Material mat)
    {
        mat.SetInt("layer_count", layers.Length);
        mat.SetColorArray("base_colors", layers.Select(x => x.tint).ToArray());
        mat.SetFloatArray("base_start_heights", layers.Select(x => x.startHeight).ToArray());
        mat.SetFloatArray("base_Blends", layers.Select(x => x.blendStrength).ToArray());
        mat.SetFloatArray("base_color_strength", layers.Select(x => x.tingStrength).ToArray());
        mat.SetFloatArray("base_texture_scale", layers.Select(x => x.textureScale).ToArray());
        Texture2DArray texture2DArray = GenerateTextureArray(layers.Select(x => x.texture).ToArray());
        mat.SetTexture("base_textures", texture2DArray);

        UpdateMeshHeight(mat, savedMinHeight, savedMaxHeight);
    }
    
    public void UpdateMeshHeight(Material mat, float minHeight, float maxHeight)
    {
        savedMinHeight = minHeight;
        savedMaxHeight = maxHeight;

        mat.SetFloat("minHeight", minHeight);
        mat.SetFloat("maxHeight", maxHeight);
    }

    Texture2DArray GenerateTextureArray(Texture2D[] textures)
    {
        var textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);

        for (int i = 0; i < textures.Length; i++)
        {
            textureArray.SetPixels(textures[i].GetPixels(), i);
        }

        textureArray.Apply();

        return textureArray;
    }

    [System.Serializable]
    public class Layer
    {
        public Texture2D texture;
        public Color tint;
        [Range(0, 1)] public float tingStrength;
        [Range(0, 1)] public float startHeight;
        [Range(0, 1)] public float blendStrength;
        public float textureScale;
    }
}