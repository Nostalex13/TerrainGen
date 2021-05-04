using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve,
        int levelOfDetail)
    {
        AnimationCurve tempHeightCurve = new AnimationCurve(heightCurve.keys);
        
        int meshLODIncrement = levelOfDetail == 0 ? 1 : levelOfDetail * 2;
        
        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2 * meshLODIncrement;
        int meshSizeUnsimplified = borderedSize - 2;
        
        float topLeftX = (meshSizeUnsimplified - 1) / -2f;
        float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

        int verteciesPerLine = (meshSize - 1) / meshLODIncrement + 1;

        var meshData = new MeshData(verteciesPerLine);
        int[,] vertexIndexMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;

        for (int y = 0; y < borderedSize; y += meshLODIncrement)
        {
            for (int x = 0; x < borderedSize; x += meshLODIncrement)
            {
                bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;

                if (isBorderVertex)
                {
                    vertexIndexMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else
                {
                    vertexIndexMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }
        
        for (int y = 0; y < borderedSize; y += meshLODIncrement)
        {
            for (int x = 0; x < borderedSize; x += meshLODIncrement)
            {
                int vertexIndex = vertexIndexMap[x, y];
                Vector2 percent = new Vector2((x - meshLODIncrement) / (float) meshSize, (y - meshLODIncrement) / (float) meshSize);
                float height = tempHeightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
                Vector3 vertexPosition = new Vector3(topLeftX + percent.x * meshSizeUnsimplified, height, topLeftZ - percent.y * meshSizeUnsimplified);

                meshData.AddVertex(vertexPosition, percent, vertexIndex);
                
                if (x < borderedSize - 1 && y < borderedSize - 1)
                {
                    int a = vertexIndexMap[x, y];
                    int b = vertexIndexMap[x + meshLODIncrement, y];
                    int c = vertexIndexMap[x, y + meshLODIncrement];
                    int d = vertexIndexMap[x + meshLODIncrement, y + meshLODIncrement];
                    
                    meshData.AddTriangle(a, d, c);
                    meshData.AddTriangle(d, a,b);
                }

                vertexIndex++;
            }
        }

        return meshData;
    }
}

public class MeshData
{
    Vector3[] verticies;
    int[] triangles;
    Vector2[] uvs;

    private Vector3[] borderVertices;
    private int[] borderTriangles;

    private int triangleIndex = 0;
    private int borderTriangleIndex = 0;

    public MeshData(int verticesPerLine)
    {
        verticies = new Vector3[verticesPerLine * verticesPerLine];
        uvs = new Vector2[verticesPerLine * verticesPerLine];
        triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];

        borderVertices = new Vector3[verticesPerLine * 4 + 4];
        borderTriangles = new int[24 * verticesPerLine];
    }

    public void AddVertex(Vector3 vertexPos, Vector2 uv, int vertexIndex)
    {
        if (vertexIndex < 0)
        {
            borderVertices[-vertexIndex - 1] = vertexPos;
        }
        else
        {
            verticies[vertexIndex] = vertexPos;
            uvs[vertexIndex] = uv;
        }
    }

    public void AddTriangle(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;

            borderTriangleIndex += 3;
        }
        else
        {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;

            triangleIndex += 3;
        }
    }
    
    private Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[verticies.Length];
        int triangleCount = triangles.Length / 3;

        for (int i = 0; i < triangleCount; i++)
        {
            int normalIndex = i * 3;
            int vertexIndexA = triangles[normalIndex];
            int vertexIndexB = triangles[normalIndex + 1];
            int vertexIndexC = triangles[normalIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromindices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }
        
        int borderTriangleCount = borderTriangles.Length / 3;

        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalIndex = i * 3;
            int vertexIndexA = borderTriangles[normalIndex];
            int vertexIndexB = borderTriangles[normalIndex + 1];
            int vertexIndexC = borderTriangles[normalIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromindices(vertexIndexA, vertexIndexB, vertexIndexC);

            if (vertexIndexA >= 0)
            {
                vertexNormals[vertexIndexA] += triangleNormal;
            }
            if (vertexIndexB >= 0)
            {
                vertexNormals[vertexIndexB] += triangleNormal;
            }
            if (vertexIndexC >= 0)
            {
                vertexNormals[vertexIndexC] += triangleNormal;
            }
        }

        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    private Vector3 SurfaceNormalFromindices(int indexA, int indexB, int indexC)
    {
        Vector3 pointA = indexA < 0 ? borderVertices[-indexA - 1] : verticies[indexA];
        Vector3 pointB = indexB < 0 ? borderVertices[-indexB - 1] : verticies[indexB];
        Vector3 pointC = indexC < 0 ? borderVertices[-indexC - 1] : verticies[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;

        return Vector3.Cross(sideAB, sideAC).normalized;
    } 

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = verticies;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals = CalculateNormals();

        return mesh;
    }
}