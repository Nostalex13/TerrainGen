using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public interface IGlobalEvent
{
}

public class EventManager : MonoBehaviour
{
    private static List<IGlobalEvent> _globalEvents = new List<IGlobalEvent>();
    private static List<INavMeshSurface> _navMeshSurfaces = new List<INavMeshSurface>();

    private static Dictionary<int, HashSet<MonoBehaviour>> _subscribers = new Dictionary<int, HashSet<MonoBehaviour>>();

    public static void SubscribeGlobal<T>(MonoBehaviour subscriber) where T : IGlobalEvent
    {
        int typeCode = typeof(T).GetHashCode();
        
        if (_subscribers.TryGetValue(typeCode, out var subscribersList))
        {
            subscribersList.Add(subscriber);
        }
        else
        {
            _subscribers.Add(typeCode, new HashSet<MonoBehaviour> {subscriber});
        }
            
    }

    public static void UnsubscribeGlobal<T>(MonoBehaviour subscriber) where T : IGlobalEvent
    {
        int typeCode = typeof(T).GetHashCode();
        
        if (_subscribers.TryGetValue(typeCode, out var subscribersList))
        {
            subscribersList?.Add(subscriber);
        }

    }

    public static void RaiseNavMesh<T>(NavMeshSurface surface) where T : IGlobalEvent
    {
        if (typeof(T) == typeof(INavMeshSurface))
        {
            int typeCode = typeof(T).GetHashCode();
        
            if (_subscribers.TryGetValue(typeCode, out var subscribersList))
            {
                foreach (var subscriber in subscribersList)
                {
                    INavMeshSurface navMeshSurface = (INavMeshSurface) subscriber;
                    
                    if (navMeshSurface != null)
                    {
                        navMeshSurface.OnNavMeshCreated(surface);
                    }
                }
            }
        }
    }
}