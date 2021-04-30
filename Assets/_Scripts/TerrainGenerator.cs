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

    [Range(0, 6)] [SerializeField] private int previewLevelOfDetail;
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
    [SerializeField] private NormalizeMode normalizeMode;
    [SerializeField] private TerrainType[] regions;

    public const int mapChunkSize = 241; // Actual mesh size is going to be 240x240
    
    private Queue<MapThreadInfo<MapData>> mapDataThreadQueue = new Queue<MapThreadInfo<MapData>>();
    private Queue<MapThreadInfo<MeshData>> meshDataThreadQueue = new Queue<MapThreadInfo<MeshData>>();

    public bool AutoUpdate
    {
        get => autoUpdate;
    }

    #region Drawing Map

    private MapData GenerateMapData(Vector2 center)
    {
        var noiseMap = GenerateNoiseMap(center);
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
        MapData mapData = GenerateMapData(Vector2.zero);

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
                        previewLevelOfDetail),
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

    private float[,] GenerateNoiseMap(Vector2 positionOffset)
    {
        positionOffset += offset;
        var noiseMap = new float[mapChunkSize, mapChunkSize];

        if (noiseScale <= 0f)
        {
            noiseScale = 0.0001f;
        }

        var random = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = random.Next(-100000, 100000) + positionOffset.x;
            float offsetY = random.Next(-100000, 100000) - positionOffset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        float halfWidth = mapChunkSize * 0.5f;
        float halfHeight = mapChunkSize * 0.5f;

        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / noiseScale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / noiseScale * frequency;

                    float perlineVal = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;
                    noiseHeight += perlineVal * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
                }

                if (noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
                }

                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                switch (normalizeMode)
                {
                    case NormalizeMode.Local:
                        noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                        break;
                    case NormalizeMode.Global:
                        float normalizedHeight = (noiseMap[x, y] + 1) / (2f * maxPossibleHeight / 1.7f);
                        noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                        break;
                }
            }
        }

        return noiseMap;
    }

    #endregion
    
    public void RequestMeshData(MapData mapData, Action<MeshData> callback, int lod)
    {
        ThreadStart threadStart = delegate { MeshDataThread(mapData, callback, lod); };

        new Thread(threadStart).Start();
    }

    private void MeshDataThread(MapData mapData, Action<MeshData> callback, int lod)
    {
        MeshData meshData =
            MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, heightCurve, lod);
        lock (meshDataThreadQueue)
        {
            meshDataThreadQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    public void RequestMapData(Action<MapData> callback, Vector2 center)
    {
        ThreadStart threadStart = delegate { MapDataThread(callback, center); };

        new Thread(threadStart).Start();
    }

    private void MapDataThread(Action<MapData> callback, Vector2 center)
    {
        MapData mapData = GenerateMapData(center);
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

    public enum NormalizeMode
    {
        Local,
        Global
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