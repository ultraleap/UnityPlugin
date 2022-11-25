using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// PullCord updates the handle projection position, 
/// updates the resting position for the pullCordHandle,
/// provides a value for the progress of the pull cord 
/// and updates the exploding items.
/// </summary>
public class PullCord : MonoBehaviour 
{
    [SerializeField] private Transform _pullCordStart, _pullCordEnd;
    [SerializeField] private PullCordHandle _pullCordHandle;

    [SerializeField, Tooltip("This is the projection of the handle onto the line between start and end positions")]
    private Transform _handleProjection;

    [SerializeField, Tooltip("Between 0 and 1. The pullCordHandle will be at this pos by default when it is collapsed")]
    private float _restingPoint = 0.2f;
    [SerializeField, Tooltip("Between 0 and 1. If pullCord progress exceeds this number, the handle's resting position is changed to the pullCord end")] 
    private float _explosionPoint = 0.7f;

    /// <summary>
    /// Dispatched when the Progress of the pull cord gets bigger than the _explosionPoint
    /// </summary>
    public UnityEvent OnExploded;
    /// <summary>
    /// Dispatched when the Progress of the pull cord gets smaller than the _explosionPoint
    /// </summary>
    public UnityEvent OnImploded;

    private float _progress;
    /// <summary>
    /// Progress of the pull cord. 0 means it is fully collapsed, 1 means it is fully exploded.
    /// </summary>
	[HideInInspector] public float Progress => _progress;

    private Vector3 _defaultRestingPos;
    private bool _exploded = false;

    private float _pullCordLength;
    private ExplodingItem[] _explodingItems;

    private void Start()
    {
        _pullCordLength = Vector3.Distance(_pullCordStart.position, _pullCordEnd.position);
        _explodingItems = FindObjectsOfType<ExplodingItem>();

        _defaultRestingPos = _pullCordStart.position + _restingPoint * (_pullCordEnd.position - _pullCordStart.position);
        _pullCordHandle.RestingPos = _defaultRestingPos;
    }


    private void Update()
    {
        UpdateProjection();

        float newProgress = Vector3.Distance(_pullCordStart.position, _handleProjection.position) / _pullCordLength;
        if (newProgress == Progress) return;

        _progress = newProgress;

        // update exploding items
        // needs to ignore the first bit of the progress as that is where the handle's resting pos is
        float explosionProgress = (Progress - _restingPoint) / (1 - _restingPoint);
        foreach (ExplodingItem explodingItem in _explodingItems)
        {
            explodingItem.SetPercent(explosionProgress);
        }

        // update resting pos of handle, if the explosion status has changed since last frame:
        if (!_exploded && _progress > _explosionPoint)
        {
            if (OnExploded != null) OnExploded.Invoke();
            _exploded = true;
            _pullCordHandle.RestingPos = _pullCordEnd.position;
        }
        else if (_exploded && _progress < _explosionPoint)
        {
            if (OnImploded != null) OnImploded.Invoke();
            _exploded = false;
            _pullCordHandle.RestingPos = _defaultRestingPos;
        }
    }

    private void UpdateProjection()
    {
        // get projection pos -> the closest point to the handle pos that is on the line between start and end positions
        Vector3 direction = _pullCordEnd.position - _pullCordStart.position;
        float length = direction.magnitude;
        direction.Normalize();
        float projectionLength = Mathf.Clamp(Vector3.Dot(_pullCordHandle.transform.position - _pullCordStart.position, direction), 0f, length);
        _handleProjection.position = _pullCordStart.position + direction * projectionLength;
    }
}
