using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainGenerator))]
public class ManualTerrainGenerator : Editor
{
    public override void OnInspectorGUI()
    {
        var terrainGenerator = (TerrainGenerator)target;

        if (DrawDefaultInspector())
        {
            if (terrainGenerator.AutoUpdate)
            {
                terrainGenerator.GenerateTerrain();
            }
        }

        if (GUILayout.Button("Generate Terrain"))
        {
            terrainGenerator.GenerateTerrain();
        }
    }
}
