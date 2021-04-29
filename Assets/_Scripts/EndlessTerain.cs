using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerain : MonoBehaviour
{
    [SerializeField] private Transform viewer;

    private const float maxViewDistance = 450;
    private static Vector2 viewerPosition;
    private static TerrainGenerator terrainGenerator;

    [SerializeField] private Material mapMaterial;
    private int chunkSize;
    private int chunksVisible;

    Dictionary<Vector2, TerrainChunk> terrainChunksDict = new Dictionary<Vector2, TerrainChunk>();
    private List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();
    
    private void Start()
    {
        terrainGenerator = FindObjectOfType<TerrainGenerator>();
        chunkSize = TerrainGenerator.mapChunkSize - 1;
        chunksVisible = Mathf.RoundToInt(maxViewDistance / chunkSize);
    }

    private void Update()
    {
        var position = viewer.position;
        viewerPosition = new Vector2(position.x, position.z);
        UpdateVisibleChunks();
    }

    void UpdateVisibleChunks()
    {
        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();
        
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunksVisible; yOffset <= chunksVisible; yOffset++)
        {
            for (int xOffset = -chunksVisible; xOffset <= chunksVisible; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunksDict.ContainsKey(viewedChunkCoord))
                {
                    terrainChunksDict[viewedChunkCoord].UpdateChunk();
                    
                    if (terrainChunksDict[viewedChunkCoord].IsVisible())
                    {
                        terrainChunksVisibleLastUpdate.Add(terrainChunksDict[viewedChunkCoord]);
                    }
                }
                else
                {
                    terrainChunksDict.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, transform, mapMaterial));
                }
            }
        }
    }
    
    private class TerrainChunk
    {
        private GameObject meshObject;
        private Vector2 position;
        private Bounds bounds;
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        
        public TerrainChunk(Vector2 coord, int size, Transform parent, Material material)
        {
            position = coord * size;
            bounds = new Bounds(position, Vector3.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0f, position.y);

            meshObject = new GameObject("TerrainChunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;
            
            meshObject.transform.position = positionV3;
            meshObject.transform.parent = parent;
            SetVisible(false);
            
            terrainGenerator.RequestMapData(OnMapDataReceived);
        }

        public void UpdateChunk()
        {
            float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool visible = viewerDistanceFromNearestEdge <= maxViewDistance;
            SetVisible(visible);
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }

        private void OnMapDataReceived(TerrainGenerator.MapData mapData)
        {
            terrainGenerator.RequestMeshData(mapData, OnMeshDataReceived);
        }
        
        private void OnMeshDataReceived(MeshData meshData)
        {
            meshFilter.mesh = meshData.CreateMesh();
        }
    }
}

