/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Interaction;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity
{
    public class HandRayParabolicLineRenderer : MonoBehaviour
    {
        [Tooltip("The scale of the projection of any hand distance from the approximated "
               + "shoulder beyond the handMergeDistance.")]
        [Range(0f, 35f)]
        public float projectionScale = 10f;

        [Tooltip("The distance from the approximated shoulder beyond which any additional "
               + "distance is exponentiated by the projectionExponent.")]
        [Range(0f, 1f)]
        public float handMergeDistance = 0.7f;

        public HandRay handRay;

        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Transform trailRendererTransform;
        [SerializeField] private float trailRendererYOffset = 0.04f;

        private TrailRenderer trailRenderer;
        private List<Vector3> parabolaPositions = new List<Vector3>();

        void Start()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponentInChildren<LineRenderer>();
                if (lineRenderer == null)
                {
                    Debug.LogWarning("HandRayParabolicLineRenderer needs a lineRenderer");
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
            lineRenderer.enabled = false;
        }

        private void OnEnable()
        {
            if (handRay == null)
            {
                handRay = FindObjectOfType<WristShoulderFarFieldHandRay>();
                if (handRay == null)
                {
                    Debug.LogWarning("HandRayParabolicLineRenderer needs a HandRay");
                    return;
                }
            }
            handRay.OnHandRayFrame += UpdateLineRenderer;
            handRay.OnHandRayEnable += EnableLineRenderer;
            handRay.OnHandRayDisable += DisableLineRenderer;
        }

        private void OnDisable()
        {
            if (handRay == null)
            {
                return;
            }

            handRay.OnHandRayFrame -= UpdateLineRenderer;
            handRay.OnHandRayEnable -= EnableLineRenderer;
            handRay.OnHandRayDisable -= DisableLineRenderer;
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
            parabolaPositions.Clear();

            // Calculate the projection of the hand if it extends beyond the handMergeDistance.
            Vector3 handProjection = handRayDirection.Direction;
            float handShoulderDist = handProjection.magnitude;
            float projectionDistance = Mathf.Max(0.0f, handShoulderDist - handMergeDistance);
            float projectionAmount = (projectionDistance + 0.15f) * projectionScale;

            if (projectionDistance > 0f)
            {
                bool hit = false;
                Vector3 startPos = handRayDirection.AimPosition;
                parabolaPositions.Add(handRayDirection.VisualAimPosition);
                Vector3 velocity = handProjection * projectionAmount;
                Vector3 segmentEnd = Vector3.zero;
                for (float i = 0; i < 8f; i += 0.1f)
                {
                    Vector3 segmentStart = evaluateParabola(startPos, velocity, Physics.gravity * 0.25f, i);
                    segmentEnd = evaluateParabola(startPos, velocity, Physics.gravity * 0.25f, i + 0.1f);
                    parabolaPositions.Add(segmentEnd);
                    RaycastHit hitInfo;
                    if (Physics.Raycast(new Ray(segmentStart, segmentEnd - segmentStart), out hitInfo, Vector3.Distance(segmentStart, segmentEnd)))
                    {
                        hit = true;
                        segmentEnd = hitInfo.point;
                    }

                    if (hit) { break; }
                }

                if (hit)
                {
                    if (trailRenderer != null)
                    {
                        UpdateTrailRenderer(segmentEnd);
                    }
                }
            }
            UpdateLineRenderer();
        }

        Vector3 evaluateParabola(Vector3 position, Vector3 velocity, Vector3 acceleration, float time)
        {
            return position + (velocity * time) + (0.5f * acceleration * (time * time));
        }

        void UpdateLineRenderer()
        {
            if (lineRenderer == null)
            {
                return;
            }

            lineRenderer.positionCount = parabolaPositions.Count;
            lineRenderer.SetPositions(parabolaPositions.ToArray());
        }

        void UpdateTrailRenderer(Vector3 hitPoint)
        {
            if (trailRendererTransform == null)
            {
                return;
            }

            // Add a small vertical offset to the hit point in order to see the trail better
            hitPoint.y += trailRendererYOffset;
            trailRendererTransform.position = hitPoint;
        }
    }
}