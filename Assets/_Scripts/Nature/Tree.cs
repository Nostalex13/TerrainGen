using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tree : MonoBehaviour, ISelectable
{
    public bool IsSelected { get; private set; }
    private Transform _transform;

    public Transform Transform
    {
        get
        {
            if (_transform == null)
                _transform = GetComponent<Transform>();

            return _transform;
        }
    }

    public void Select()
    {
        if (IsSelected)
        {
            
        }
        else
        {
            
        }
    }

}
