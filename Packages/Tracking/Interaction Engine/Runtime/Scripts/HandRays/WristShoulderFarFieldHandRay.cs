/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;

namespace Leap.Unity.Interaction
{
#pragma warning disable 0618

    /// <summary>
    /// Calculates a far field hand ray based on the wrist, shoulder and pinch position
    /// </summary>
    public class WristShoulderFarFieldHandRay : HandRay
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

        [SerializeField] private InferredBodyPositions inferredBodyPositions;

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
            transformHelper.position = leapXRServiceProvider.CurrentFrame.GetHand(chirality).PalmPosition.ToVector3();
            Quaternion palmForwardRotation = leapXRServiceProvider.CurrentFrame.GetHand(chirality).Rotation.ToQuaternion() * Quaternion.Euler(90, 0, 0);
            transformHelper.rotation = palmForwardRotation;
            return !SimpleFacingCameraCallbacks.GetIsFacingCamera(transformHelper, leapXRServiceProvider.mainCamera, minDotProductAllowedForFacingCamera);
        }

        /// <summary>
        /// Calculates the Ray Direction using the wrist and shoulder position aimed through a stable pinch position
        /// </summary>
        protected override void CalculateRayDirection()
        {
            Hand hand = leapXRServiceProvider.CurrentFrame.GetHand(chirality);
            if(hand == null)
            {
                return;
            }

            int index = hand.IsLeft ? 0 : 1;
            Vector3 shoulderPosition = inferredBodyPositions.ShoulderPositions[index];

            HandRayDirection.Hand = hand;
            HandRayDirection.VisualAimPosition = hand.GetPredictedPinchPosition();
            Vector3 unfilteredRayOrigin = GetRayOrigin(hand, shoulderPosition);

            //Filtering using the One Euro filter reduces jitter from both positions
            HandRayDirection.AimPosition = aimPositionFilter.Filter(hand.GetStablePinchPosition(), Time.time);
            HandRayDirection.RayOrigin = rayOriginFilter.Filter(unfilteredRayOrigin, Time.time);

            HandRayDirection.Direction = (HandRayDirection.AimPosition - HandRayDirection.RayOrigin).normalized;  
            InvokeOnHandRayFrame(HandRayDirection);
        }

        private Vector3 GetRayOrigin(Hand hand, Vector3 shoulderPosition)
        {
            return Vector3.Lerp(GetWristOffsetPosition(hand), shoulderPosition, wristShoulderBlendAmount);
        }

        private Vector3 GetWristOffsetPosition(Hand hand)
        {
            Vector3 worldWristPosition = wristOffset;
            if (hand.IsRight)
            {
                worldWristPosition.x = -worldWristPosition.x;
            }

            transformHelper.transform.position = hand.WristPosition.ToVector3();
            transformHelper.transform.rotation = hand.Rotation.ToQuaternion();
            return transformHelper.TransformPoint(worldWristPosition);
        }
    }
#pragma warning restore 0618
}