using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragAndDrop : MonoBehaviour
{
    private RaycastHit[] _results;

    private ISelectable _selectedObj;
    private Transform _selectableTransform;
    private float deltaDistance;

    private void Update()
    {
        TrySelect();
    }

    private void TrySelect()
    {
        if (_selectedObj == null)
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                _results = new RaycastHit[3];
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.RaycastNonAlloc(ray, _results) > 0)
                {
                    for (int i = 0; i < _results.Length; i++)
                    {
                        if (_results[i].distance != 0)
                        {
                            var selectable = _results[i].transform.GetComponent<ISelectable>();
                            if (selectable != null)
                            {
                                _selectedObj = selectable;
                                _selectableTransform = selectable.Transform;
                                _selectableTransform.localPosition += Vector3.up * 2f;

                                Debug.Log("selected");
                                break;
                            }
                        }
                    }
                }
            }
        }
        else
        {
            if (Input.GetKeyUp(KeyCode.Mouse0))
            {
                _selectableTransform.localPosition -= Vector3.up * 2f;
                _selectableTransform = null;
                _selectedObj = null;

                Debug.Log("unselected");
            }
            else
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.RaycastNonAlloc(ray, _results) > 0)
                {
                    for (int i = 0; i < _results.Length; i++)
                    {
                        if (_results[i].distance != 0)
                        {
                            var selectable = _results[i].transform.GetComponent<ISelectable>();
                            if (selectable == null)
                            {
                                _selectableTransform.position = _results[i].point + Vector3.up * 2f;;

                                // Debug.Log("unselected");
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}