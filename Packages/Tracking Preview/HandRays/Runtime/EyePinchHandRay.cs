/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap;
using Leap.Unity;
using Leap.Unity.Preview.HandRays;
using UnityEngine;

namespace Leap.Unity.Preview.HandRays
{
    /// <summary>
    /// Calculates a far field hand ray that goes from the eye through the pinch position.
    /// This allows for a really precise ray that is great for inferring gaze and selecting objects,
    /// as the user forced to look through the pinch position in order to aim.
    /// </summary>
    public class EyePinchHandRay : HandRay
    {
        [SerializeField] private InferredBodyPositions inferredBodyPositions;

        [Header("Debug Gizmos")]
        [SerializeField] private bool drawDebugGizmos;

        [SerializeField] private bool drawRay = true;
        [SerializeField] private Color rayColor = Color.green;

        [SerializeField] private bool drawRayAimAndOrigin = true;
        [SerializeField] private Color rayAimAndOriginColor = Color.red;

        private float gizmoRadius = 0.01f;

        /// <summary>
        /// This local-space offset from the wrist is used to better align the ray to the pinch position
        /// </summary>
        private Vector3 wristOffset = new Vector3(0.0425f, 0.0652f, 0.0f);
        private Transform transformHelper;

        private OneEuroFilter<Vector3> aimPositionFilter;
        private OneEuroFilter<Vector3> rayOriginFilter;

        /// <summary>
        /// Beta param for OneEuroFilter (see https://cristal.univ-lille.fr/~casiez/1euro/)
        /// If you're experiencing high speed lag, increase beta
        /// </summary>
        private float oneEuroBeta = 100;
        /// <summary>
        /// MinCutoff for OneEuroFilter (see https://cristal.univ-lille.fr/~casiez/1euro/)
        /// If you're experiencing slow speed jitter, decrease MinCutoff
        /// </summary>
        private float oneEuroMinCutoff = 5;

        private readonly float oneEurofreq = 30;

        /// <summary>
        /// The min dot product allowed when calculating if the hand is facing the camera
        /// </summary>
        private float minDotProductAllowedForFacingCamera = 0.55f;

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();
            transformHelper = new GameObject("WristShoulderFarFieldRay_TransformHelper").transform;
            transformHelper.SetParent(transform);

            if (inferredBodyPositions == null)
            {
                inferredBodyPositions = gameObject.AddComponent<InferredBodyPositions>();
            }

            aimPositionFilter = new OneEuroFilter<Vector3>(oneEurofreq, oneEuroMinCutoff, oneEuroBeta);
            rayOriginFilter = new OneEuroFilter<Vector3>(oneEurofreq, oneEuroMinCutoff, oneEuroBeta);
        }

        /// <summary>
        /// Calculates whether the hand ray should be enabled
        /// </summary>
        protected override bool ShouldEnableRay()
        {
            if (!base.ShouldEnableRay())
            {
                return false;
            }
            transformHelper.position = leapProvider.CurrentFrame.GetHand(chirality).PalmPosition;
            Quaternion palmForwardRotation = leapProvider.CurrentFrame.GetHand(chirality).Rotation * Quaternion.Euler(90, 0, 0);
            transformHelper.rotation = palmForwardRotation;
            return !IsFacingTransform(transformHelper, inferredBodyPositions.Head, minDotProductAllowedForFacingCamera);
        }

        private bool IsFacingTransform(Transform facingTransform, Transform transformToCheck, float minAllowedDotProduct = 0.8F)
        {
            return Vector3.Dot((transformToCheck.transform.position - facingTransform.position).normalized, facingTransform.forward) > minAllowedDotProduct;
        }

        /// <summary>
        /// Calculates the Ray Direction using the wrist and shoulder position aimed through a stable pinch position
        /// </summary>
        protected override void CalculateRayDirection()
        {
            Hand hand = leapProvider.CurrentFrame.GetHand(chirality);
            if (hand == null)
            {
                return;
            }

            handRayDirection.Hand = hand;
            handRayDirection.VisualAimPosition = hand.GetPredictedPinchPosition();
            Vector3 unfilteredRayOrigin = GetRayOrigin();

            //Filtering using the One Euro filter reduces jitter from both positions
            handRayDirection.AimPosition = aimPositionFilter.Filter(hand.GetPredictedPinchPosition(), Time.time);
            handRayDirection.RayOrigin = rayOriginFilter.Filter(unfilteredRayOrigin, Time.time);

            handRayDirection.Direction = (handRayDirection.AimPosition - handRayDirection.RayOrigin).normalized;
            InvokeOnHandRayFrame(handRayDirection);
        }

        private Vector3 GetRayOrigin()
        {
            return chirality == Chirality.Left ? inferredBodyPositions.EyePositions[0] : inferredBodyPositions.EyePositions[1];
        }

        private void OnDrawGizmos()
        {
            if (!drawDebugGizmos || !Application.isPlaying || !HandRayEnabled)
            {
                return;
            }

            if (drawRay)
            {
                Gizmos.color = rayColor;
                Gizmos.DrawRay(handRayDirection.RayOrigin, handRayDirection.Direction * 10);
            }

            if (drawRayAimAndOrigin)
            {
                Gizmos.color = rayAimAndOriginColor;
                Gizmos.DrawCube(handRayDirection.RayOrigin, Vector3.one * gizmoRadius);
                Gizmos.DrawSphere(handRayDirection.AimPosition, gizmoRadius);
            }
        }
    }
}