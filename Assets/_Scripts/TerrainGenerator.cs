using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    [SerializeField] private Renderer textureRenderer;
    
    [SerializeField] private int mapWidth;
    [SerializeField] private int mapHeight;
    [SerializeField] private float noiseScale;

    [SerializeField] private bool autoUpdate = false;

    public bool AutoUpdate { get => autoUpdate; }

    public void GenerateTerrain()
    {
        var noiseMap = GenerateNoiseMap(mapWidth, mapHeight, noiseScale);
        Texture2D texture = new Texture2D(mapWidth, mapHeight);
        Color[] colorMap = new Color[mapWidth * mapHeight];

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                colorMap[y * mapWidth + x] = Color.Lerp(Color.black, Color.white, noiseMap[x, y]);
            }
        }
        
        texture.SetPixels(colorMap);
        texture.Apply();

        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(mapWidth, 1, mapHeight);
    }
    
    private float[,] GenerateNoiseMap(int width, int height, float scale)
    {
        var noiseMap = new float[width, height];

        if (scale <= 0f)
        {
            scale = 0.0001f;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float sampleX = x / scale;
                float sampleY = y / scale;

                float perlineVal = Mathf.PerlinNoise(sampleX, sampleY);
                noiseMap[x, y] = perlineVal;
            }
        }

        return noiseMap;
    }
}
