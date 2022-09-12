using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private Vector3 _offset;
    [SerializeField] private float _followSmoothness = 0.4f;

    private Transform _transform;

    private void Awake()
    {
        _transform = transform;
    }

    // ;ateasd
    private void LateUpdate()
    {
        if (_target != null)
        {
            var followPos = _target.position + _offset;
            _transform.position = Vector3.Lerp(_transform.position, followPos, _followSmoothness);
        }
        else
        {
            Debug.Log("camera target is null");
        }
    }
}
