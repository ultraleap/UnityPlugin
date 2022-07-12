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
    /// a FarFieldHandDirection holds data about the hand and the corresponding ray.
    /// Including the ray's origin, aim position and direction.
    /// </summary>
    public struct FarFieldHandDirection
    {
        public Hand Hand;
        public Vector3 RayOrigin;
        public Vector3 AimPosition;
        public Vector3 VisualAimPosition;
        public Vector3 Direction;
    }

    /// <summary>
    /// Provides Ray directions for far field interactions. 
    /// Different parameters can be specified such as where the ray should originate 
    /// or how much its direction is influenced by the wrist orientation vs it's relative 
    /// position to the shoulder.
    /// </summary>
    public class WristShoulderFarFieldRay : MonoBehaviour
    {
        /// <summary>
        /// The leap provider provides the hand data from which we calculate the far field ray directions 
        /// </summary>
        public LeapProvider leapProvider;

        /// <summary>
        /// The hand this ray is generated for
        /// </summary>
        public Chirality chirality;

        /// <summary>
        /// An event that is called after new ray direction has been calculated. Subscribe to it to cast rays with the far field hand directions it provides.
        /// </summary>
        public event Action<FarFieldHandDirection> OnFarFieldHandDirectionFrame;

        /// <summary>
        /// This local-space offset from the wrist is used to better align the ray to the pinch position
        /// </summary>
        private Vector3 wristOffset = new Vector3(0.0425f, 0.0652f, 0.0f);

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

        /// <summary>
        /// The most recently calculated far field direction
        /// </summary>
        [HideInInspector] public FarFieldHandDirection FarFieldRay = new FarFieldHandDirection();

        [SerializeField] private InferredBodyPositions inferredBodyPositions;
        private Transform wristOffsetTransformHelper;
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

        // Start is called before the first frame update
        void Start()
        {
            if (leapProvider == null)
            {
                leapProvider = FindObjectOfType<LeapServiceProvider>();
                if (leapProvider == null)
                {
                    leapProvider = FindObjectOfType<LeapProvider>();
                    if (leapProvider == null)
                    {
                        Debug.LogWarning("No leap provider in scene - FarFieldDirection is dependent on one.");
                    }
                }
            }

            wristOffsetTransformHelper = new GameObject("FarFieldRaycast_WristOffset").transform;
            wristOffsetTransformHelper.SetParent(transform);

            if (inferredBodyPositions == null)
            {
                inferredBodyPositions = gameObject.AddComponent<InferredBodyPositions>();
            }

            aimPositionFilter = new OneEuroFilter<Vector3>(oneEurofreq, oneEuroMinCutoff, oneEuroBeta);
            rayOriginFilter = new OneEuroFilter<Vector3>(oneEurofreq, oneEuroMinCutoff, oneEuroBeta);
        }

        // Update is called once per frame
        void Update()
        {
            if (leapProvider == null || leapProvider.CurrentFrame == null)
            {
                return;
            }
            CalculateRayDirection();
        }

        private void CalculateRayDirection()
        {
            Hand hand = leapProvider.CurrentFrame.GetHand(chirality);
            if(hand == null)
            {
                return;
            }

            int index = hand.IsLeft ? 0 : 1;
            Vector3 shoulderPosition = inferredBodyPositions.ShoulderPositions[index];

            FarFieldRay.Hand = hand;
            FarFieldRay.VisualAimPosition = hand.GetPredictedPinchPosition();
            Vector3 unfilteredRayOrigin = GetRayOrigin(hand, shoulderPosition);

            //Filtering using the One Euro filter reduces jitter from both positions
            FarFieldRay.AimPosition = aimPositionFilter.Filter(hand.GetStablePinchPosition(), Time.time);
            FarFieldRay.RayOrigin = rayOriginFilter.Filter(unfilteredRayOrigin, Time.time);

            FarFieldRay.Direction = (FarFieldRay.AimPosition - FarFieldRay.RayOrigin).normalized;  
            OnFarFieldHandDirectionFrame?.Invoke(FarFieldRay);
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

            wristOffsetTransformHelper.transform.position = hand.WristPosition.ToVector3();
            wristOffsetTransformHelper.transform.rotation = hand.Rotation.ToQuaternion();
            return wristOffsetTransformHelper.TransformPoint(worldWristPosition);
        }
    }
#pragma warning restore 0618
}