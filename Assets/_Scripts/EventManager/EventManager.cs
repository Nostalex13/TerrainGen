using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// TODO add smth?
public interface IGlobalEvent
{
}

public class EventManager : MonoBehaviour
{
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

    public static void RaiseNavMesh<T>(NavMeshSurface surface) where T : INavMeshSurface
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