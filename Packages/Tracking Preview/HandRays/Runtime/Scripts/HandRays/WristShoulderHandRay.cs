/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;

namespace Leap.Unity.Preview.HandRays
{
    /// <summary>
    /// Calculates a far field hand ray based on the wrist, shoulder and pinch position.
    /// This allows a developer to decide how responsive the ray feels to the hand's rotation.
    /// For a more responsive ray, blend the WristShoulderBlendAmount towards the wrist, 
    /// For a more stable ray, blend the WristShoulderBlendAmount towards the shoulder.
    /// </summary>
    public class WristShoulderHandRay : HandRay
    {
        /// <summary>
        /// The wrist shoulder lerp amount is only used when the rayOrigin is wristShoulderLerp. 
        /// It specifies how much the wrist vs the shoulder is used as a ray origin.
        /// At 0, only the wrist position and rotation are taken into account.
        /// At 1, only the shoulder position and rotation are taken into account.
        /// For a more responsive far field ray, blend towards the wrist. For a more stable far field ray,
        /// blend towards the shoulder. Keep the value central for a blend between the two.
        /// </summary>
        [Tooltip("Our far field ray is a direction from a wristShoulder blend position, through a stable pinch position.\n" +
            "WristShoulderBlendAmount determines the wristShoulder blend position.\n" +
            " - At 0, only an wrist position is taken into account.\n" +
            " - At 1, only the shoulder position is taken into account.\n" +
            " - For a more responsive far field ray, blend towards the wrist.\n" +
            " - For a more stable far field ray, blend towards the shoulder.\n" +
            " - Keep the value central for a blend between the two.")]
        [Range(0f, 1)] public float wristShoulderBlendAmount = 0.532f;

        [SerializeField] protected InferredBodyPositions inferredBodyPositions;

        [Header("Debug Gizmos")]
        [SerializeField] private bool drawDebugGizmos;

        [SerializeField] private bool drawRay = true;
        [SerializeField] private Color rayColor = Color.green;

        [SerializeField] private bool drawRayAimAndOrigin = true;
        [SerializeField] private Color rayAimAndOriginColor = Color.red;

        [SerializeField] private bool drawWristShoulderBlend = false;
        [SerializeField] private Color wristShoulderBlendColor = Color.blue;

        private float gizmoRadius = 0.01f;

        /// <summary>
        /// This local-space offset from the wrist is used to better align the ray to the pinch position
        /// </summary>
        private Vector3 pinchWristOffset = new Vector3(0.0425f, 0.0652f, 0.0f);
        protected Transform transformHelper;

        protected OneEuroFilter<Vector3> aimPositionFilter;
        protected OneEuroFilter<Vector3> rayOriginFilter;

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

            ResetFilters();
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

        protected virtual Vector3 GetWristOffsetPosition(Hand hand)
        {
            Vector3 localWristPosition = pinchWristOffset;
            if (hand.IsRight)
            {
                localWristPosition.x = -localWristPosition.x;
            }

            transformHelper.transform.position = hand.WristPosition;
            transformHelper.transform.rotation = hand.Rotation;
            return transformHelper.TransformPoint(localWristPosition);
        }

        private bool IsFacingTransform(Transform facingTransform, Transform transformToCheck, float minAllowedDotProduct = 0.8F)
        {
            return Vector3.Dot((transformToCheck.transform.position - facingTransform.position).normalized, facingTransform.forward) > minAllowedDotProduct;
        }

        protected override Vector3 CalculateVisualAimPosition()
        {
            return handRayDirection.Hand.GetPredictedPinchPosition();
        }

        protected override Vector3 CalculateAimPosition()
        {
            return aimPositionFilter.Filter(handRayDirection.Hand.GetStablePinchPosition());
        }

        protected override Vector3 CalculateRayOrigin()
        {
            int index = handRayDirection.Hand.IsLeft ? 0 : 1;
            return rayOriginFilter.Filter(GetRayOrigin(handRayDirection.Hand, inferredBodyPositions.ShoulderPositions[index]));

        }

        protected virtual Vector3 GetRayOrigin(Hand hand, Vector3 shoulderPosition)
        {
            return Vector3.Lerp(GetWristOffsetPosition(hand), shoulderPosition, wristShoulderBlendAmount);
        }

        protected override Vector3 CalculateDirection()
        {
            return (handRayDirection.AimPosition - handRayDirection.RayOrigin).normalized;
        }

        public override void ResetRay()
        {
            ResetFilters();
        }

        protected void ResetFilters()
        {
            aimPositionFilter = new OneEuroFilter<Vector3>(oneEurofreq, oneEuroMinCutoff, oneEuroBeta);
            rayOriginFilter = new OneEuroFilter<Vector3>(oneEurofreq, oneEuroMinCutoff, oneEuroBeta);
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

            if (drawWristShoulderBlend)
            {
                Gizmos.color = wristShoulderBlendColor;

                Hand hand = leapProvider.CurrentFrame.GetHand(chirality);
                Vector3 shoulderPos = inferredBodyPositions.ShoulderPositions[hand.IsLeft ? 0 : 1];
                Vector3 wristPos = GetWristOffsetPosition(hand);
                Gizmos.DrawSphere(shoulderPos, gizmoRadius);
                Gizmos.DrawSphere(wristPos, gizmoRadius);
                Gizmos.DrawLine(shoulderPos, wristPos);
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