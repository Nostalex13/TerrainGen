using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TerrainGenerator : MonoBehaviour
{
    [SerializeField] private Renderer textureRenderer;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshFilter meshFilter;

    [Range(0, 6)] [SerializeField] private int levelOfDetail;
    [SerializeField] private float noiseScale;
    [SerializeField] private float meshHeightMultiplier;
    [SerializeField] private AnimationCurve heightCurve;

    [SerializeField] private int octaves;
    [Range(0f, 1f)] [SerializeField] private float persistance;
    [SerializeField] private float lacunarity;

    [SerializeField] private int seed;
    [SerializeField] private Vector2 offset;

    [Space] [SerializeField] private bool autoUpdate = false;
    [SerializeField] private MapDrawMode drawMode = MapDrawMode.Noise;
    [SerializeField] private TerrainType[] regions;

    public const int mapChunkSize = 241; // Actual mesh size is going to be 240x240
    
    private Queue<MapThreadInfo<MapData>> mapDataThreadQueue = new Queue<MapThreadInfo<MapData>>();
    private Queue<MapThreadInfo<MeshData>> meshDataThreadQueue = new Queue<MapThreadInfo<MeshData>>();

    public bool AutoUpdate
    {
        get => autoUpdate;
    }

    #region Drawing Map

    private MapData GenerateMapData()
    {
        var noiseMap = GenerateNoiseMap();
        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];

        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                colorMap[y * mapChunkSize + x] = GetHeightColor(noiseMap[x, y]);
            }
        }

        return new MapData()
        {
            colorMap = colorMap,
            heightMap = noiseMap
        };
    }

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData();

        switch (drawMode)
        {
            case MapDrawMode.Noise:
                DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
                break;
            case MapDrawMode.Colored:
                DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
                break;
            case MapDrawMode.Mesh:
                DrawMesh(
                    MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, heightCurve,
                        levelOfDetail),
                    TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
                break;
        }
    }

    private void DrawTexture(Texture2D texture)
    {
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(mapChunkSize, 1, mapChunkSize);
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
        var noiseMap = new float[mapChunkSize, mapChunkSize];

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

        float halfWidth = mapChunkSize * 0.5f;
        float halfHeight = mapChunkSize * 0.5f;

        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
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

        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }

        return noiseMap;
    }

    #endregion
    
    public void RequestMeshData(MapData mapData, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate { MeshDataThread(mapData, callback); };

        new Thread(threadStart).Start();
    }

    private void MeshDataThread(MapData mapData, Action<MeshData> callback)
    {
        MeshData meshData =
            MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, heightCurve, levelOfDetail);
        lock (meshDataThreadQueue)
        {
            meshDataThreadQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    public void RequestMapData(Action<MapData> callback)
    {
        ThreadStart threadStart = delegate { MapDataThread(callback); };

        new Thread(threadStart).Start();
    }

    private void MapDataThread(Action<MapData> callback)
    {
        MapData mapData = GenerateMapData();
        lock (mapDataThreadQueue)
        {
            mapDataThreadQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    private void Update()
    {
        if (mapDataThreadQueue.Count > 0)
        {
            for (int i = 0; i < mapDataThreadQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
        
        if (meshDataThreadQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    private void OnValidate()
    {
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }

        if (octaves < 1)
        {
            octaves = 1;
        }
    }

    #region Info types

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

    private readonly struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.parameter = parameter;
            this.callback = callback;
        }
    }

    public struct MapData
    {
        public float[,] heightMap;
        public Color[] colorMap;

        public MapData(Color[] colorMap, float[,] heightMap)
        {
            this.colorMap = colorMap;
            this.heightMap = heightMap;
        }
    }
    
    #endregion
}