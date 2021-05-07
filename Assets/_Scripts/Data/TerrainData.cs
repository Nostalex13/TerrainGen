using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdatableData
{
    public const float uniformScale = 1f;
    public float meshHeightMultiplier;
    public AnimationCurve heightCurve;
    
    public bool doApplyFallofMap = false;
    public bool useFlatShading = false;
}
