using Leap.Unity;
using Leap.Unity.Interaction;
using Leap.Unity.Interaction.PhysicsHands;
using Leap.Unity.Preview.HandRays;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public abstract class HandRayRenderer : MonoBehaviour
{
    [SerializeField] private HandRay _handRay;

    [Header("Rendering")]
    [SerializeField] private LineRenderer _lineRenderer;

    [Header("Trail")]
    [SerializeField] private Transform _trailRendererTransform;
    [SerializeField] private float _trailRendererYOffset = 0.04f;
    private TrailRenderer _trailRenderer;

    [Tooltip("Note that the Interaction Engine and Physics Hands layers will be ignored automatically.")]
    [Header("Layer Logic")]
    [SerializeField] private List<SingleLayer> _layersToIgnore = new List<SingleLayer>();

    protected LayerMask _layerMask;

    [Header("Events")]
    public UnityEvent<RaycastHit> OnRayUpdate;

    private void Start()
    {
        _layerMask = -1;
        InteractionManager interactionManager = FindObjectOfType<InteractionManager>();
        if (interactionManager != null)
        {
            _layerMask ^= interactionManager.contactBoneLayer.layerMask;
        }

        PhysicsProvider physicsProvider = FindObjectOfType<PhysicsProvider>();
        if (physicsProvider != null)
        {
            _layerMask ^= physicsProvider.HandsLayer.layerMask;
            _layerMask ^= physicsProvider.HandsResetLayer.layerMask;
        }

        foreach (var layers in _layersToIgnore)
        {
            _layerMask ^= layers.layerMask;
        }

        if (_lineRenderer == null)
        {
            _lineRenderer = GetComponentInChildren<LineRenderer>();
            if (_lineRenderer == null)
            {
                Debug.LogWarning("HandRayParabolicLineRenderer needs a lineRenderer");
            }
        }

        if (_trailRendererTransform != null)
        {
            _trailRenderer = _trailRendererTransform.GetComponentInChildren<TrailRenderer>();
            if (_trailRenderer == null)
            {
                Debug.LogWarning("The trail renderer transform reference does not have a trailRenderer attached");
            }
        }
        _lineRenderer.enabled = false;
    }

    private void OnEnable()
    {
        if (_handRay == null)
        {
            _handRay = FindObjectOfType<WristShoulderFarFieldHandRay>();
            if (_handRay == null)
            {
                Debug.LogWarning("HandRayParabolicLineRenderer needs a HandRay");
                return;
            }
        }
        _handRay.OnHandRayFrame += UpdateLineRenderer;
        _handRay.OnHandRayEnable += EnableLineRenderer;
        _handRay.OnHandRayDisable += DisableLineRenderer;
    }

    private void OnDisable()
    {
        if (_handRay == null)
        {
            return;
        }

        _handRay.OnHandRayFrame -= UpdateLineRenderer;
        _handRay.OnHandRayEnable -= EnableLineRenderer;
        _handRay.OnHandRayDisable -= DisableLineRenderer;
    }

    void EnableLineRenderer(HandRayDirection farFieldDirection)
    {
        _lineRenderer.enabled = true;

        if (_trailRendererTransform != null)
        {
            _trailRenderer.enabled = true;
            _trailRenderer.Clear();
        }
    }

    void DisableLineRenderer(HandRayDirection farFieldDirection)
    {
        _lineRenderer.enabled = false;
        if (_trailRendererTransform != null)
        {
            _trailRenderer.enabled = false;
        }
    }

    private void UpdateLineRenderer(HandRayDirection handRayDirection)
    {
        if (!_lineRenderer.enabled)
        {
            _lineRenderer.enabled = true;
        }
        if(_trailRendererTransform != null)
        {
            _trailRenderer.enabled = true;
        }
        if (UpdateLineRendererLogic(handRayDirection, out RaycastHit result))
        {
            OnRayUpdate?.Invoke(result);
        }
    }

    protected abstract bool UpdateLineRendererLogic(HandRayDirection handRayDirection, out RaycastHit raycastResult);

    protected void UpdateLineRendererPositions(int positionCount, Vector3[] positions, bool updateTrail = true)
    {
        _lineRenderer.positionCount = positionCount;
        if (positions.Length > 0)
        {
            _lineRenderer.SetPositions(positions);
            if (updateTrail)
            {
                UpdateTrailRenderer(positions[positions.Length - 1]);
            }
        }
    }

    private void UpdateTrailRenderer(Vector3 position)
    {
        if(_trailRendererTransform == null)
        {
            return;
        }

        // Add a small vertical offset to the hit point in order to see the trail better
        position.y += _trailRendererYOffset;
        _trailRendererTransform.position = position;
    }

    private void OnValidate()
    {
        if (_lineRenderer == null)
        {
            _lineRenderer = GetComponentInChildren<LineRenderer>();
        }
    }
}
