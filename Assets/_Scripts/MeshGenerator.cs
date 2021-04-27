using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    public static void GenerateTerrainMesh(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        var meshData  = new MeshData(width, height);
        int vertexIndex = 0;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                meshData.verticies[vertexIndex] = new Vector3(topLeftX + x, heightMap[x, y], topLeftZ - y);
                
                vertexIndex++;
            }
        }
    }
}
 
public class MeshData
{
    public Vector3[] verticies;
    public int[] triangles;

    private int triangleIndex;

    public MeshData(int meshWidth, int meshHeight)
    {
        verticies = new Vector3[meshHeight * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
    }

    public void AddTriangle(int a, int b, int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;
        
        triangleIndex += 3;
    }
}
