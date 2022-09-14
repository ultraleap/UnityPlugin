using Leap.Unity.Preview.HandRays;
using UnityEngine;

public class HandRayDotRenderer : HandRayRenderer
{
    public float lineRendererDistance = 2f;
    public float lineRendererSize = 0.1f;

    protected override bool UpdateLineRendererLogic(HandRayDirection handRayDirection, out RaycastHit raycastResult)
    {
        Vector3 lineRendererEndPos;
        raycastResult = new RaycastHit();
        bool hit = false;
        if (Physics.Raycast(new Ray(handRayDirection.RayOrigin, handRayDirection.Direction), out raycastResult, lineRendererDistance, _layerMask))
        {
            hit = true;
            lineRendererEndPos = raycastResult.point + handRayDirection.Direction * -(lineRendererSize/2);
        }
        else
        {
            lineRendererEndPos = handRayDirection.RayOrigin + handRayDirection.Direction * lineRendererDistance;
        }

        Vector3 lineRendererStartPos = lineRendererEndPos + handRayDirection.Direction * -lineRendererSize;
        UpdateLineRendererPositions(2, new Vector3[] { lineRendererStartPos, lineRendererEndPos });
        return hit;
    }
}
