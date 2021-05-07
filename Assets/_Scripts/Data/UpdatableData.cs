using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdatableData : ScriptableObject
{
    public event System.Action OnValueChanged;
    public bool autoUpdate;

    protected virtual void OnValidate()
    {
        if (autoUpdate)
        {
            NotifyUpdate();
        }
    }

    public void NotifyUpdate()
    {
        OnValueChanged?.Invoke();
    }
}
