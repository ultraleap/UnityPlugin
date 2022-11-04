using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Preview.HandRays
{
    public class StraightRayInteractor : HandRayInteractor
    {
        public float maxRayDistance = 50f;

        protected override int UpdateRayInteractorLogic(HandRayDirection handRayDirection, out RaycastHit[] results, out RaycastHit primaryHit)
        {
            int hits = 0;
            results = null;
            if (Physics.Raycast(new Ray(handRayDirection.RayOrigin, handRayDirection.Direction), out primaryHit, maxRayDistance, layerMask))
            {
                hits = 1;
                results = new RaycastHit[] { primaryHit };
                linePoints = new Vector3[] { handRayDirection.VisualAimPosition, primaryHit.collider.transform.position };
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