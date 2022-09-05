using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public interface INavMeshSurface : IGlobalEvent
{
    void OnNavMeshCreated(NavMeshSurface surface);
}

public class NavMeshGenerator : MonoBehaviour, INavMeshSurface
{
    private HashSet<NavMeshSurface> _navMeshSurfaces = new HashSet<NavMeshSurface>();

    // Awake
    private void Awake()
    {
        EventManager.SubscribeGlobal<INavMeshSurface>(this);
    }

    private void OnDestroy()
    {
        EventManager.UnsubscribeGlobal<INavMeshSurface>(this);
    }

    void INavMeshSurface.OnNavMeshCreated(NavMeshSurface surface)
    {
        _navMeshSurfaces.Add(surface);
        surface.BuildNavMesh();
    }
}
