using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class NoiseData : UpdatableData
{
    public NormalizeMode normalizeMode;
    public float noiseScale;

    public int octaves;
    [Range(0f, 1f)] public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        if (lacunarity < 1)
        {
            lacunarity = 1;
        }

        if (octaves < 1)
        {
            octaves = 1;
        }
    }
#endif
}