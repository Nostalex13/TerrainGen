using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AgentController : MonoBehaviour
{
    [SerializeField] private NavMeshAgent _agent;

    private Ray _ray;
    private RaycastHit[] hits;
    private Camera _mainCamera;

    public static int valuess = 123;

    // awake123123123
    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    // update
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            if (_agent == null)
            {
                Debug.Log("agent is null");
                return;
            }
            hits = new RaycastHit[1];

            _ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            int hitsCount = Physics.RaycastNonAlloc(_ray, hits);

            if (hitsCount > 0)
            {
                _agent.SetDestination(hits[0].point);
            }
        }
    }
}