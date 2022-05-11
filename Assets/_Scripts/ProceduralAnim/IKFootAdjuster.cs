using UnityEngine;
using Random = UnityEngine.Random;

public class IKFootAdjuster : MonoBehaviour
{
    private static bool _isLegMoving;

    [SerializeField] private Transform _body;
    [SerializeField] private float _stepDistance = 0.15f;
    [SerializeField] private float _stepHeight = 0.2f;
    [SerializeField] private float _stepSpeed = 1f;

    private Vector3 _newPos;
    private Vector3 _oldPos;
    private Vector3 _currentPos;
    private Vector3 _bodyFootVector;

    private float _stepLerpVal;

    private AgentMovement _agentMovement;

    private void Awake()
    {
        _agentMovement = FindObjectOfType<AgentMovement>();

        Vector3 pos = transform.position;
        _newPos = pos;
        _oldPos = pos;
        _currentPos = pos;
        _bodyFootVector = _body.position - pos;
    }

    void Update()
    {
        transform.position = _currentPos;
        Ray ray = new Ray(_body.position - _bodyFootVector + Vector3.up / 2f, Vector3.down);
        // Ray ray = new Ray(_body.position + Vector3.up * 0.15f, _body.position - _bodyFootVector); // TODO body -> foot vector instead of UPfoot -> foot
        // Debug.DrawLine(_body.position + Vector3.up * 0.15f, _body.position - _bodyFootVector, Color.green);

        if (Physics.Raycast(ray, out RaycastHit hit, 20f))
        {
            if (!_isLegMoving && Vector3.Distance(_newPos, hit.point) > _stepDistance)
            {
                _isLegMoving = true;

                _stepLerpVal = 0f;
                _newPos = hit.point + _agentMovement.MovementVector * _stepDistance;
            }
        }

        if (_stepLerpVal < 1)
        {
            Vector3 footPos = Vector3.Lerp(_oldPos, _newPos, _stepLerpVal);
            footPos.y += Mathf.Sin(_stepLerpVal * Mathf.PI) * _stepHeight;

            _currentPos = footPos;
            _stepLerpVal += Time.deltaTime * _stepSpeed;

            if (_stepLerpVal >= 1)
            {
                _isLegMoving = false;
            }
        }
        else
        {
            _oldPos = _newPos;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_newPos, 0.03f);
    }
}