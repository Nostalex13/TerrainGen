using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

// public class TestNature : MonoBehaviour
// {
//     public float radius = 1;
//     public Vector2 regionSize = Vector2.one;
//     public int stopSamples = 30;
//     public float displayRadius = 1;
//
//     private List<Vector2> points = new List<Vector2>();
//
//     private List<GameObject> pointsGo = new List<GameObject>();
//
//     public void GeneratePoints()
//     { 
//         ClearPoints();
//         points = NatureGeneration.GeneratePoints(radius, regionSize, stopSamples);
//         
//         if (points != null)
//         {
//             for (int i = 0; i < points.Count; i++)
//             {
//                 var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
//                 obj.transform.localScale = Vector3.one * (displayRadius * 2f);
//                 obj.transform.position = points[i];
//                 obj.transform.parent = transform;
//                 
//                 pointsGo.Add(obj);
//             }
//         }
//     }
//
//     public void ClearPoints()
//     {
//         for (int i = 0; i < pointsGo.Count; i++)
//         {
//             DestroyImmediate(pointsGo[i]);
//         }
//      
//         pointsGo.Clear();  
//     }
//
//     private void OnDrawGizmos()
//     {
//         Gizmos.DrawWireCube(regionSize / 2,regionSize);
//     }
// }

// #if UNITY_EDITOR
//
// [UnityEditor.CustomEditor(typeof(TestNature))]
// public class NatureGenerationEditor : UnityEditor.Editor
// {
//     public override void OnInspectorGUI()
//     {
//         base.OnInspectorGUI();
//         
//         var test = (TestNature)target;
//
//         if (GUILayout.Button("Generate points"))
//         {
//             test.GeneratePoints();
//         }
//         
//         if (GUILayout.Button("Clear points"))
//         {
//             test.ClearPoints();
//         }
//     }
// }
//
// #endif
