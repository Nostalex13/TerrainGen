using UnityEngine;

public class AgentMovement : MonoBehaviour
{
    public Vector3 MovementVector { get; private set; }

    private Vector3 _previousPos;
    private Vector3 _currentPos;

    private Transform _transform;

    private void Awake()
    {
        _transform = transform;
        
        _currentPos = _transform.position;
        _previousPos = _transform.position;
    }

    void Update()
    {
        _currentPos = _transform.position;
        MovementVector = (_currentPos - _previousPos).normalized;
        _previousPos = _currentPos;
    }
}
