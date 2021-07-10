using UnityEditor;
using UnityEngine;

public interface ISelectable
{
    void Select();
    bool IsSelected { get; }
    Transform Transform { get; }
}
