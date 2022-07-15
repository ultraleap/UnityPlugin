using Leap.Unity.Interaction;
using UnityEngine;

public class HandRayLineRenderer : MonoBehaviour
{
    public float LineRendererDistance = 50f;
    public HandRay HandRay;

    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform trailRendererTransform;
    private TrailRenderer trailRenderer;

    // Start is called before the first frame update
    void Start()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponentInChildren<LineRenderer>();
            if (lineRenderer == null)
            {
                Debug.LogWarning("HandRayLineRenderer needs a lineRenderer");
            }
        }

        if (trailRendererTransform != null)
        {
            trailRenderer = trailRendererTransform.GetComponentInChildren<TrailRenderer>();
            if (trailRenderer == null)
            {
                Debug.LogWarning("The trail renderer transform reference does not have a trailRenderer attached");
            }
        }

        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
    }

    private void OnEnable()
    {
        if (HandRay == null)
        {
            HandRay = FindObjectOfType<WristShoulderFarFieldHandRay>();
            if (HandRay == null)
            {
                Debug.LogWarning("HandRayLineRenderer needs a HandRay");
                return;
            }
        }
        HandRay.OnHandRayFrame += UpdateLineRenderer;
        HandRay.OnHandRayEnable += EnableLineRenderer;
        HandRay.OnHandRayDisable += DisableLineRenderer;
    }

    private void OnDisable()
    {
        if (HandRay == null)
        {
            return;
        }

        HandRay.OnHandRayFrame -= UpdateLineRenderer;
        HandRay.OnHandRayEnable -= EnableLineRenderer;
        HandRay.OnHandRayDisable -= DisableLineRenderer;
    }

    void EnableLineRenderer(HandRayDirection farFieldDirection)
    {
        lineRenderer.enabled = true;

        if (trailRendererTransform != null)
        {
            trailRenderer.enabled = true;
            trailRenderer.Clear();
        }
    }

    void DisableLineRenderer(HandRayDirection farFieldDirection)
    {
        lineRenderer.enabled = false;
        if (trailRendererTransform != null)
        {
            trailRenderer.enabled = false;
        }
    }

    void UpdateLineRenderer(HandRayDirection handRayDirection)
    {
        Vector3 lineRendererEndPos = handRayDirection.RayOrigin + handRayDirection.Direction * LineRendererDistance;
        UpdateLineRendererPositions(lineRenderer, handRayDirection.VisualAimPosition, lineRendererEndPos);
        if (trailRendererTransform != null)
        {
            UpdateTrailRendererPosition(lineRendererEndPos);
        }
    }

    private void UpdateLineRendererPositions(LineRenderer lineRenderer, Vector3 start, Vector3 end)
    {
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }

    private void UpdateTrailRendererPosition(Vector3 position)
    {
        trailRendererTransform.position = position;
    }
}