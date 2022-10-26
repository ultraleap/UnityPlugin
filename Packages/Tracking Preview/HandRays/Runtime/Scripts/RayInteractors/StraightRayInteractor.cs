using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Preview.HandRays
{
    public class StraightRayInteractor : HandRayInteractor
    {
        public float maxRayDistance = 50f;

        protected override int UpdateRayInteractorLogic(HandRayDirection handRayDirection, out RaycastHit[] results)
        {
            RaycastHit raycastResult = new RaycastHit();
            int hits = 0;
            results = null;
            if (Physics.Raycast(new Ray(handRayDirection.RayOrigin, handRayDirection.Direction), out raycastResult, maxRayDistance, layerMask))
            {
                hits = 1;
                results = new RaycastHit[] { raycastResult };
                linePoints = new Vector3[] { handRayDirection.VisualAimPosition, raycastResult.collider.transform.position };
            } 
            else
            {
                linePoints = new Vector3[] { handRayDirection.VisualAimPosition, handRayDirection.VisualAimPosition + (handRayDirection.Direction * maxRayDistance) };
            }
            numPoints = linePoints.Length;
            return hits;
        }
    }
}