using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NatureGeneration : MonoBehaviour
{
    private const float radius = 3;
    private const int numSamplesBeforeStop = 2;

    [SerializeField] private GameObject treePrefab;

    public void GeneratePoints(Vector2 size, float[,] heightMap, Transform parent, TerrainData terrainData)
    {
        float cellSize = radius / Mathf.Sqrt(2);
        int[,] grid = new int[Mathf.CeilToInt(size.x / cellSize), Mathf.CeilToInt(size.y / cellSize)];
        List<Vector2> points = new List<Vector2>();
        
        List<Vector2> spawnPoints = new List<Vector2>();
        spawnPoints.Clear();
        spawnPoints.Add(size / 2f);

        while (spawnPoints.Count > 0)
        {
            int spawnIndex = Random.Range(0, spawnPoints.Count);
            Vector2 spawnCenter = spawnPoints[spawnIndex];
            bool candidateAccepted = false;

            for (int i = 0; i < numSamplesBeforeStop; i++)
            {
                float angle = Random.value * Mathf.PI * 2;
                Vector2 direction = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                Vector2 candidate = spawnCenter + direction * Random.Range(radius, 2 * radius);

                if (IsValid(candidate, size, cellSize, radius, points, grid))
                {
                    points.Add(candidate);
                    spawnPoints.Add(candidate);
                    grid[(int) (candidate.x / cellSize), (int) (candidate.y / cellSize)] = points.Count;
                    candidateAccepted = true;

                    break;
                }
            }

            if (!candidateAccepted)
            {
                spawnPoints.RemoveAt(spawnIndex);
            }
        }
        
        PlantSomeWeed(parent, points, heightMap, (int)Mathf.Abs(size.x), terrainData);
    }

    private void PlantSomeWeed(Transform parent, List<Vector2> points, float[,] heightMap, int size, TerrainData terrainData)
    {
        float halfSize = size / 2f;
        
        // TODO delete
        // Debug height verticies 
        // for (int x = 0; x < heightMap.GetLength(0); x++)
        // {
        //     for (int y = 0; y < heightMap.GetLength(1); y++)
        //     {
        //         float heightActual1 = terrainData.heightCurve.Evaluate(heightMap[x,y]) * terrainData.meshHeightMultiplier;
        //         float mirrorZ1 = -(y - halfSize); // Need to mirror z axes 
        //         
        //         var treeSphere = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //         treeSphere.transform.parent = parent;
        //         treeSphere.transform.localPosition = new Vector3(x - halfSize - 1, heightActual1, mirrorZ1 + 1); // x - 1 and z + 1 to compensate bounds of the heightmap
        //         treeSphere.transform.localScale = Vector3.one * 0.2f;
        //         treeSphere.name = $"{x.ToString()}|{y.ToString()}";
        //     }
        // }
        
        for (int i = 0; i < points.Count; i++)
        {
            var spawnPoint = points[i];
            
            int indexX = Mathf.RoundToInt(spawnPoint.x);
            int indexY = Mathf.RoundToInt(spawnPoint.y);

            float mediumHeight = heightMap[indexX, indexY];

            if (mediumHeight >= 0.47 && mediumHeight <= 0.65)
            {
                float heightActual = terrainData.heightCurve.Evaluate(mediumHeight) * terrainData.meshHeightMultiplier;
                var treeObj = Instantiate(treePrefab, parent, true);
                treeObj.transform.localScale = new Vector3(1f, 1f, 1f);
                float mirrorZ = -(indexY - halfSize); // Need to mirror z axes 
                treeObj.transform.localPosition = new Vector3(indexX - halfSize - 1, heightActual, mirrorZ + 1); // x - 1 and z + 1 to compensate bounds of the heightmap
                treeObj.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
        }
    }

    private static bool IsValid(Vector2 candidate, Vector2 sampleRegionSize, float cellSize, float radius,
        List<Vector2> points, int[,] grid)
    {
        if (candidate.x >= 0 && candidate.x < sampleRegionSize.x && candidate.y >= 0 &&
            candidate.y < sampleRegionSize.y)
        {
            int cellX = (int) (candidate.x / cellSize);
            int cellY = (int) (candidate.y / cellSize);
            int searchStartX = Mathf.Max(0, cellX - 2);
            int searchEndX = Mathf.Min(cellX + 2, grid.GetLength(0) - 1);
            int searchStartY = Mathf.Max(0, cellY - 2);
            int searchEndY = Mathf.Min(cellY + 2, grid.GetLength(1) - 1);

            for (int x = searchStartX; x <= searchEndX; x++)
            {
                for (int y = searchStartY; y <= searchEndY; y++)
                {
                    int pointIndex = grid[x, y] - 1;

                    if (pointIndex != -1)
                    {
                        float sqrMagnitude = (candidate - points[pointIndex]).sqrMagnitude;

                        if (sqrMagnitude < radius * radius)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        return false;
    }
}