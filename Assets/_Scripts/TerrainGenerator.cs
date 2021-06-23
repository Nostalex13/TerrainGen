using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(NatureGeneration))]
public class TerrainGenerator : MonoBehaviour
{
    [SerializeField] private TerrainData terrainData;
    [SerializeField] private NoiseData noiseData;
    [SerializeField] private TextureData textureData;

    [Space] [SerializeField] private Renderer textureRenderer;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private Material terrainMaterial;

    [Range(0, MeshGenerator.supportedChunkSizesLength - 1)]
    public int chunkSizeIndex;

    [Range(0, MeshGenerator.supportedFlatshadedChunkSizesLength - 1)]
    public int chunkFlatshadedSizeIndex;

    [Range(0, MeshGenerator.supportedLODs - 1)] [SerializeField]
    private int previewLevelOfDetail;

    [Space] [SerializeField] private bool autoUpdate = false;
    [SerializeField] private MapDrawMode drawMode = MapDrawMode.Noise;

    private float[,] falloffMap;
    private Queue<MapThreadInfo<MapData>> mapDataThreadQueue = new Queue<MapThreadInfo<MapData>>();
    private Queue<MapThreadInfo<MeshData>> meshDataThreadQueue = new Queue<MapThreadInfo<MeshData>>();

    private NatureGeneration natureGeneration;

    public bool AutoUpdate => autoUpdate;
    public TerrainData TerrainData => terrainData;
    public NoiseData NoiseData => noiseData;
    public NatureGeneration NatureGeneration => natureGeneration;

    public int mapChunkSize
    {
        get
        {
            if (terrainData.useFlatShading)
            {
                return MeshGenerator.supportedFlatshadedChunkSizes[chunkFlatshadedSizeIndex] -
                       1; // For flat shading smaller chunk sizes are required
            }
            else
            {
                return MeshGenerator.supportedChunkSizes[chunkSizeIndex] - 1;
            }
        }
    }

    private void Awake()
    {
        natureGeneration = GetComponent<NatureGeneration>();
        textureData.UpdateMeshHeight(terrainMaterial, terrainData.MinHeight, terrainData.MaxHeight);
        textureData.ApplyToMaterial(terrainMaterial);
    }

    private void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMap_Editor();
        }
    }

    void OnTextureValuesUpdated()
    {
        textureData.ApplyToMaterial(terrainMaterial);
    }

    #region Drawing Map

    private MapData GenerateMapData(Vector2 center)
    {
        var noiseMap = GenerateNoiseMap(center, mapChunkSize + 2, noiseData.seed, noiseData.noiseScale,
            noiseData.octaves, noiseData.persistance, noiseData.lacunarity,
            noiseData.normalizeMode);

        if (terrainData.doApplyFallofMap)
        {
            if (falloffMap == null)
            {
                falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize + 2);
            }

            for (int y = 0; y < mapChunkSize + 2; y++)
            {
                for (int x = 0; x < mapChunkSize + 2; x++)
                {
                    noiseMap[x, y] = Mathf.Clamp(noiseMap[x, y] - falloffMap[x, y], 0, 1);
                }
            }
        }

        return new MapData()
        {
            heightMap = noiseMap
        };
    }

    public void DrawMap_Editor()
    {
        textureData.UpdateMeshHeight(terrainMaterial, terrainData.MinHeight, terrainData.MaxHeight);
        MapData mapData = GenerateMapData(Vector2.zero);

        switch (drawMode)
        {
            case MapDrawMode.Noise:
                DrawTexture_Editor(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
                break;
            case MapDrawMode.Mesh:
                DrawMesh_Editor(
                    MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier,
                        terrainData.heightCurve,
                        previewLevelOfDetail, terrainData.useFlatShading));
                break;
            case MapDrawMode.FalloffMap:
                DrawTexture_Editor(
                    TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
                break;
        }
    }

    private void DrawTexture_Editor(Texture2D texture)
    {
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = Vector3.one;
    }

    private void DrawMesh_Editor(MeshData meshData)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshFilter.transform.localScale = Vector3.one * TerrainData.uniformScale;
    }

    public void ClearMesh()
    {
        meshFilter.sharedMesh = null;
    }

    private float[,] GenerateNoiseMap(Vector2 _positionOffset, int _mapChunkSize, int _seed, float _noiseScale,
        int _octaves, float _persistance, float _lacunarity, NormalizeMode _normalizeMode)
    {
        _positionOffset += noiseData.offset;
        var noiseMap = new float[_mapChunkSize, _mapChunkSize];

        if (_noiseScale <= 0f)
        {
            _noiseScale = 0.0001f;
        }

        var random = new System.Random(_seed);
        Vector2[] octaveOffsets = new Vector2[_octaves];
        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < _octaves; i++)
        {
            float offsetX = random.Next(-100000, 100000) + _positionOffset.x;
            float offsetY = random.Next(-100000, 100000) - _positionOffset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= _persistance;
        }

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        float halfWidth = _mapChunkSize * 0.5f;
        float halfHeight = _mapChunkSize * 0.5f;

        for (int y = 0; y < _mapChunkSize; y++)
        {
            for (int x = 0; x < _mapChunkSize; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < _octaves; i++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / _noiseScale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / _noiseScale * frequency;

                    float perlineVal = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;
                    noiseHeight += perlineVal * amplitude;

                    amplitude *= _persistance;
                    frequency *= _lacunarity;
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

        for (int y = 0; y < _mapChunkSize; y++)
        {
            for (int x = 0; x < _mapChunkSize; x++)
            {
                switch (_normalizeMode)
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
            MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier,
                terrainData.heightCurve, lod,
                terrainData.useFlatShading);
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
        if (terrainData != null)
        {
            terrainData.OnValueChanged -= OnValuesUpdated;
            terrainData.OnValueChanged += OnValuesUpdated;
        }

        if (noiseData != null)
        {
            noiseData.OnValueChanged -= OnValuesUpdated;
            noiseData.OnValueChanged += OnValuesUpdated;
        }

        if (textureData != null)
        {
            textureData.OnValueChanged -= OnTextureValuesUpdated;
            textureData.OnValueChanged += OnTextureValuesUpdated;
        }
    }

    #region Info types

    private enum MapDrawMode
    {
        Noise,
        Mesh,
        FalloffMap
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
    }

    #endregion
}

public enum NormalizeMode
{
    Local,
    Global
}