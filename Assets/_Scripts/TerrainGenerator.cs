using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TerrainGenerator : MonoBehaviour
{
    [SerializeField] private Renderer textureRenderer;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshFilter meshFilter;

    [SerializeField] private int mapWidth;
    [SerializeField] private int mapHeight;
    [SerializeField] private float noiseScale;

    [SerializeField] private int octaves;
    [Range(0f, 1f)] [SerializeField] private float persistance;
    [SerializeField] private float lacunarity;

    [SerializeField] private int seed;
    [SerializeField] private Vector2 offset;

    [Space] [SerializeField] private bool autoUpdate = false;
    [SerializeField] private MapDrawMode drawMode = MapDrawMode.Noise;
    [SerializeField] private TerrainType[] regions;

    public bool AutoUpdate
    {
        get => autoUpdate;
    }

    public void GenerateMap()
    {
        var noiseMap = GenerateNoiseMap();
        Color[] colorMap = new Color[mapWidth * mapHeight];
        
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {     
                colorMap[y * mapWidth + x] = GetHeightColor(noiseMap[x, y]);
            }
        }
        
        switch (drawMode)
        {
            case MapDrawMode.Noise:
                DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
                break;
            case MapDrawMode.Colored:
                DrawTexture(TextureGenerator.TextureFromColorMap(colorMap, mapWidth, mapHeight));
                break;
            case MapDrawMode.Mesh:
                DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap), TextureGenerator.TextureFromColorMap(colorMap, mapWidth, mapHeight));
                break;
        }
    }

    private void DrawTexture(Texture2D texture)
    {
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(mapWidth, 1, mapHeight);
    }

    private void DrawMesh(MeshData meshData, Texture2D texture)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }

    public void ClearMesh()
    {
        meshFilter.sharedMesh = null;
    }

    private Color GetHeightColor(float height)
    {
        Color color = regions[0].color;
                        
        for (int i = 0; i < regions.Length; i++)
        { 
            if (height >= regions[i].height)
            {
                color = regions[i].color;
            }
        }
                        
        return color;
    }

    private float[,] GenerateNoiseMap()
    {
        var noiseMap = new float[mapWidth, mapHeight];

        if (noiseScale <= 0f)
        {
            noiseScale = 0.0001f;
        }

        var random = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = random.Next(-100000, 100000) + offset.x;
            float offsetY = random.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        float halfWidth = mapWidth * 0.5f;
        float halfHeight = mapHeight * 0.5f;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth) / noiseScale * frequency + octaveOffsets[i].x;
                    float sampleY = (y - halfHeight) / noiseScale * frequency + octaveOffsets[i].y;

                    float perlineVal = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;
                    noiseHeight += perlineVal * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                }
                if (noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }

                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }

        return noiseMap;
    }

    private void OnValidate()
    {
        if (mapHeight < 1)
        {
            mapHeight = 1;
        }

        if (mapWidth < 1)
        {
            mapWidth = 1;
        }

        if (lacunarity < 1)
        {
            lacunarity = 1;
        }

        if (octaves < 1)
        {
            octaves = 1;
        }
    }

    private enum MapDrawMode
    {
        Noise,
        Colored,
        Mesh
    }

    [System.Serializable]
    private struct TerrainType
    {
        public string name;
        public float height;
        public Color color;
    }
}