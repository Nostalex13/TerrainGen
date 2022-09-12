using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EndlessTerain : MonoBehaviour
{
    [SerializeField] private Transform viewer;
    [SerializeField] private int colliderLODIndex;
    [SerializeField] private GameObject chunkPrefab;

    [SerializeField] private LODInfo[] detailLevels;
    [SerializeField] private Material mapMaterial;
    private int chunkSize;
    private int chunksVisible;

    // viewrer
    private const float viewerThresholdForChunkUpdate = 25f;

    private const float sqrviewerThresholdForChunkUpdate =
        viewerThresholdForChunkUpdate * viewerThresholdForChunkUpdate;

    private const float colliderGenerationDistanceThreshold = 5f;

    private static float maxViewDistance;
    private static Vector2 viewerPosition;
    private static Vector2 viewerPositionOld;
    private static TerrainGenerator terrainGenerator;

    private static Dictionary<Vector2, TerrainChunk> terrainChunksDict = new Dictionary<Vector2, TerrainChunk>();
    private static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    private void Awake()
    {
        colliderLODIndex = colliderLODIndex >= detailLevels.Length ? detailLevels.Length : colliderLODIndex;
        maxViewDistance = 50;
        terrainGenerator = FindObjectOfType<TerrainGenerator>();
        chunkSize = terrainGenerator.mapChunkSizes - 1;
        chunksVisible = Mathf.RoundToInt(maxViewDistance / chunkSize);

        UpdateVisibleChunks();
    }

    private void Update()
    {
        var position = viewer.position;
        viewerPosition = new Vector2(position.x, position.z) / TerrainData.uniformScale;

        if (viewerPosition != viewerPositionOld)
        {
            for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
            {
                terrainChunksVisibleLastUpdate[i].UpdateCollisionMesh();
            }
        }

        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrviewerThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();
        for (int i = terrainChunksVisibleLastUpdate.Count - 1; i >= 0; i--)
        {
            var item = terrainChunksVisibleLastUpdate[i];
            alreadyUpdatedChunkCoords.Add(item.Coord);
            item.UpdateChunk();
        }

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunksVisible; yOffset <= chunksVisible; yOffset++)
        {
            for (int xOffset = -chunksVisible; xOffset <= chunksVisible; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (alreadyUpdatedChunkCoords.Contains(viewedChunkCoord))
                {
                    continue;
                }

                if (terrainChunksDict.ContainsKey(viewedChunkCoord))
                {
                    terrainChunksDict[viewedChunkCoord].UpdateChunk();
                }
                else
                {
                    var chunk = new TerrainChunk(viewedChunkCoord, chunkSize, transform,
                        mapMaterial, detailLevels,
                        colliderLODIndex, chunkPrefab);
                    terrainChunksDict.Add(viewedChunkCoord, chunk);
                    
                    chunk.callback = (surf) =>
                    {
                        StartCoroutine(StartCallback(surf));
                    };
                    chunk.CallCallback();
                }
            }

            IEnumerator StartCallback(NavMeshSurface surface)
            {
                yield return new WaitForSeconds(1f);
                EventManager.RaiseNavMesh<INavMeshSurface>(surface);
                Debug.Log("nav mesh updated");
            }
        }
    }

    private class TerrainChunk
    {
        public Vector2 Coord;

        private GameObject meshObject;
        public Action<NavMeshSurface> callback;
        private NavMeshSurface navMeshSurface;
        private Vector2 position;
        private Bounds bounds;

        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private MeshCollider meshCollider;

        private LODInfo[] detailLevels;
        private LODMesh[] lodMeshes;
        private int colliderLODIndex;

        private TerrainGenerator.MapData mapData;
        private int previousLODIndex = -1;
        private int size;

        private bool mapDataReceived;
        private bool hasSetCollider;

        public TerrainChunk(Vector2 coord, int size, Transform parent, Material material, LODInfo[] detailLevels,
            int colliderLODIndex, GameObject chunkPrefab)
        {
            this.size = size;
            this.Coord = coord;
            this.detailLevels = detailLevels;
            this.colliderLODIndex = colliderLODIndex;
            position = coord * size;
            bounds = new Bounds(position, Vector3.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0f, position.y);

            // meshObject = new GameObject("TerrainChunk");
            meshObject = Instantiate(chunkPrefab);
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            navMeshSurface = meshObject.GetComponent<NavMeshSurface>();
            meshRenderer.material = material;

            meshObject.name = $"{meshObject.name}_{coord}";
            meshObject.transform.position = positionV3 * TerrainData.uniformScale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * TerrainData.uniformScale;
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];

            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod);
                lodMeshes[i].updateCallback += UpdateChunk;

                if (i == colliderLODIndex)
                {
                    lodMeshes[i].updateCallback += UpdateCollisionMesh;
                }
            }

            terrainGenerator.RequestMapData(OnMapDataReceived, position);
        }

        public void CallCallback()
        {
            callback(navMeshSurface);
        }

        public void UpdateChunk()
        {
            if (!mapDataReceived)
            {
                return;
            }

            bool wasVisible = IsVisible();
            float viewerDistanceFromNearestEdge =
                Mathf.Sqrt(bounds.SqrDistance(viewerPosition)); // TODO get rid of sqrt heavy
            bool visible = viewerDistanceFromNearestEdge <= maxViewDistance;

            if (visible)
            {
                int lodIndex = 0;

                for (int i = 0; i < detailLevels.Length - 1; i++)
                {
                    if (viewerDistanceFromNearestEdge > detailLevels[i].visibleDistanceThreshold)
                    {
                        lodIndex = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }

                if (lodIndex != previousLODIndex)
                {
                    LODMesh lodMesh = lodMeshes[lodIndex];

                    if (lodMesh.hasMesh)
                    {
                        previousLODIndex = lodIndex;
                        meshFilter.mesh = lodMesh.mesh;
                        terrainGenerator.NatureGeneration.GeneratePoints(new Vector2(size, size), mapData.heightMap,
                            meshObject.transform, terrainGenerator.TerrainData);
                    }
                    else if (!lodMesh.hasRequestedMesh)
                    {
                        lodMesh.RequestMesh(mapData);
                    }
                }
            }

            if (wasVisible != visible)
            {
                if (visible)
                {
                    terrainChunksVisibleLastUpdate.Add(this);
                }
                else
                {
                    terrainChunksVisibleLastUpdate.Remove(this);
                }
            }

            SetVisible(visible);
        }

        public void UpdateCollisionMesh()
        {
            if (hasSetCollider)
            {
                return;
            }

            float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);

            if (sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDstThreshold)
            {
                if (!lodMeshes[colliderLODIndex].hasRequestedMesh)
                {
                    lodMeshes[colliderLODIndex].RequestMesh(mapData);
                }
            }

            if (sqrDstFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold)
            {
                if (lodMeshes[colliderLODIndex].hasMesh)
                {
                    meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
                    hasSetCollider = true;
                }
            }

            // var cou = lodMeshes[0].mesh.vert;
        }

        private void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        private bool IsVisible()
        {
            return meshObject.activeSelf;
        }

        private void OnMapDataReceived(TerrainGenerator.MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;

            UpdateChunk();
        }
    }

    private class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;

        private int lod;

        public event System.Action updateCallback;

        public LODMesh(int lod)
        {
            this.lod = lod;
        }

        private static bool spawned = false;

        private void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            // if (!spawned)
            // {
            //     for (int i = 0; i < meshData.cubesTransformsPos.Count; i++)
            //     {
            //         var tree = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //         tree.transform.localScale = new Vector3(1f, 1f, 1f);
            //         tree.transform.localPosition = meshData.cubesTransformsPos[i];
            //     }
            //
            //     spawned = true;
            // }

            updateCallback();
        }

        public void RequestMesh(TerrainGenerator.MapData mapData)
        {
            hasRequestedMesh = true;
            terrainGenerator.RequestMeshData(mapData, OnMeshDataReceived, lod);
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        [Range(0, MeshGenerator.supportedLODs - 1)]
        public int lod;

        public float visibleDistanceThreshold;

        public float sqrVisibleDstThreshold => visibleDistanceThreshold * visibleDistanceThreshold;
    }
}