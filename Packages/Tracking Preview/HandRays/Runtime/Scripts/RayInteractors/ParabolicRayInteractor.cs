/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Preview.HandRays
{
    /// <summary>
    /// Takes in a ray direction and casts it out in a parabolic curve
    /// </summary>
    public class ParabolicRayInteractor : HandRayInteractor
    {
        [Tooltip("The scale of the projection of any hand distance from the approximated "
       + "shoulder beyond the handMergeDistance.")]
        [Range(0f, 35f)]
        public float projectionScale = 10f;

        [Tooltip("The distance from the approximated shoulder beyond which any additional "
               + "distance is exponentiated by the projectionExponent.")]
        [Range(0f, 1f)]
        public float handMergeDistance = 0.7f;

        private List<Vector3> _parabolaPositions = new List<Vector3>();

        [Tooltip("If true, the points passed to the RayRenderer stop when the ray hits an object, or reaches max distance." +
            "\nIf false, the points passed to the RayRenderer stop when the ray hits the floor, or reaches max distance")]
        public bool stopRayOnHit = false;

        protected override int UpdateRayInteractorLogic(HandRayDirection handRayDirection, out RaycastHit[] results, out RaycastHit primaryHit)
        {
            _parabolaPositions.Clear();
            primaryHit = new RaycastHit();

            // Calculate the projection of the hand if it extends beyond the handMergeDistance.
            Vector3 handProjection = handRayDirection.Direction;
            float handShoulderDist = handProjection.magnitude;
            float projectionDistance = Mathf.Max(0.0f, handShoulderDist - handMergeDistance);
            float projectionAmount = (projectionDistance + 0.15f) * projectionScale;

            results = null;
            bool foundFloor = false;
            int hits = 0;
            if (projectionDistance > 0f)
            {
                Vector3 startPos = handRayDirection.AimPosition;
                _parabolaPositions.Add(handRayDirection.VisualAimPosition);
                Vector3 velocity = handProjection * projectionAmount;

                for (float i = 0; i < 8f; i += 0.1f)
                {
                    Vector3 segmentStart = EvaluateParabola(startPos, velocity, Physics.gravity * 0.25f, i);
                    Vector3 segmentEnd = EvaluateParabola(startPos, velocity, Physics.gravity * 0.25f, i + 0.1f);
                    _parabolaPositions.Add(segmentEnd);

                    if (Physics.Raycast(new Ray(segmentStart, segmentEnd - segmentStart), out RaycastHit hit, Vector3.Distance(segmentStart, segmentEnd), layerMask))
                    {
                        if (hits == 0)
                        {
                            primaryHit = hit;
                            results = new RaycastHit[] { primaryHit };
                            hits = 1;
                        }

                        if (hit.transform.gameObject.layer == farFieldLayerManager.FloorLayer)
                        {
                            foundFloor = true;
                        }

                        _parabolaPositions.Add(hit.point);
                    }

                    if ((hits == 1 && stopRayOnHit) || (foundFloor && !stopRayOnHit))
                    {
                        break;
                    }
                }
            }

            linePoints = _parabolaPositions.ToArray();
            numPoints = linePoints.Length;
            return hits;
        }

        private Vector3 EvaluateParabola(Vector3 position, Vector3 velocity, Vector3 acceleration, float time)
        {
            return position + (velocity * time) + (0.5f * acceleration * (time * time));
        }
    }
}