using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Preview.HandRays
{
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

        Vector3 evaluateParabola(Vector3 position, Vector3 velocity, Vector3 acceleration, float time)
        {
            return position + (velocity * time) + (0.5f * acceleration * (time * time));
        }

        protected override int UpdateRayInteractorLogic(HandRayDirection handRayDirection, out RaycastHit[] results)
        {
            _parabolaPositions.Clear();

            // Calculate the projection of the hand if it extends beyond the handMergeDistance.
            Vector3 handProjection = handRayDirection.Direction;
            float handShoulderDist = handProjection.magnitude;
            float projectionDistance = Mathf.Max(0.0f, handShoulderDist - handMergeDistance);
            float projectionAmount = (projectionDistance + 0.15f) * projectionScale;

            results = null;
            int hits = 0;
            if (projectionDistance > 0f)
            {
                Vector3 startPos = handRayDirection.AimPosition;
                _parabolaPositions.Add(handRayDirection.VisualAimPosition);
                Vector3 velocity = handProjection * projectionAmount;

                RaycastHit raycastResult;
                for (float i = 0; i < 8f; i += 0.1f)
                {
                    Vector3 segmentStart = evaluateParabola(startPos, velocity, Physics.gravity * 0.25f, i);
                    Vector3 segmentEnd = evaluateParabola(startPos, velocity, Physics.gravity * 0.25f, i + 0.1f);
                    _parabolaPositions.Add(segmentEnd);

                    if (Physics.Raycast(new Ray(segmentStart, segmentEnd - segmentStart), out raycastResult, Vector3.Distance(segmentStart, segmentEnd), layerMask))
                    {
                        hits = 1;
                        _parabolaPositions.Add(raycastResult.point);
                        results = new RaycastHit[] { raycastResult };
                    }

                    if (hits == 1) { break; }
                }
            }

            linePoints = _parabolaPositions.ToArray();
            numPoints = linePoints.Length;
            return hits;
        }
    }
}