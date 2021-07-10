using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdatableData : ScriptableObject
{
    public event System.Action OnValueChanged;
    public bool autoUpdate;

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (autoUpdate)
        {
            UnityEditor.EditorApplication.update += NotifyUpdate;
        }
    }

    public void NotifyUpdate()
    {
        UnityEditor.EditorApplication.update -= NotifyUpdate;
        OnValueChanged?.Invoke();
    }
#endif
}