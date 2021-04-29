using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve,
        int levelOfDetail)
    {
        AnimationCurve tempHeightCurve = new AnimationCurve(heightCurve.keys);
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        int meshLODIncrement = levelOfDetail == 0 ? 1 : levelOfDetail * 2;
        int verteciesPerLine = (width - 1) / meshLODIncrement + 1;

        var meshData = new MeshData(verteciesPerLine, verteciesPerLine);
        int vertexIndex = 0;

        for (int y = 0; y < height; y += meshLODIncrement)
        {
            for (int x = 0; x < width; x += meshLODIncrement)
            {
                meshData.verticies[vertexIndex] = new Vector3(topLeftX + x,
                    tempHeightCurve.Evaluate(heightMap[x, y]) * heightMultiplier, topLeftZ - y);
                meshData.uvs[vertexIndex] = new Vector2(x / (float) width, y / (float) height);

                if (x < width - 1 && y < height - 1)
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + verteciesPerLine + 1,
                        vertexIndex + verteciesPerLine);
                    meshData.AddTriangle(vertexIndex + verteciesPerLine + 1, vertexIndex, vertexIndex + 1);
                }

                vertexIndex++;
            }
        }

        return meshData;
    }
}

public class MeshData
{
    public Vector3[] verticies;
    public int[] triangles;
    public Vector2[] uvs;

    private int triangleIndex = 0;

    public MeshData(int meshWidth, int meshHeight)
    {
        verticies = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
    }

    public void AddTriangle(int a, int b, int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;

        triangleIndex += 3;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = verticies;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        return mesh;
    }
}