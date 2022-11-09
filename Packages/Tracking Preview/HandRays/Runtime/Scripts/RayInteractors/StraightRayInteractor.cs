/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 * Ultraleap proprietary and confidential.                                    *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/
using UnityEngine;

namespace Leap.Unity.Preview.HandRays
{
    /// <summary>
    /// Casts a ray out in a straight line
    /// </summary>
    public class StraightRayInteractor : HandRayInteractor
    {
        /// <summary>
        /// Maximum distance the ray is cast
        /// </summary>
        public float maxRayDistance = 50f;

        protected override int UpdateRayInteractorLogic(HandRayDirection handRayDirection, out RaycastHit[] results, out RaycastHit primaryHit)
        {
            int hits = 0;
            results = null;
            if (Physics.Raycast(new Ray(handRayDirection.RayOrigin, handRayDirection.Direction), out primaryHit, maxRayDistance, layerMask))
            {
                hits = 1;
                results = new RaycastHit[] { primaryHit };
                linePoints = new Vector3[] { handRayDirection.VisualAimPosition, primaryHit.point};
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