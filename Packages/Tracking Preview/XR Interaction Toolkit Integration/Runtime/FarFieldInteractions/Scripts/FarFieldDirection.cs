/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap;
using Leap.Unity;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Preview.FarFieldInteractions
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
        public Vector3 RayOriginRaw;
        public Vector3 AimPosition;
        public Vector3 AimPositionRaw;
        public Vector3 Direction;
        public Vector3 DebugRayOrigin;
        public Vector3 DebugAimPosition;
        public Vector3 DebugDirection;
    }

    /// <summary>
    /// Provides Ray directions for far field interactions. 
    /// Different parameters can be specified such as where the ray should originate 
    /// or how much its direction is influenced by the wrist orientation vs it's relative 
    /// position to the shoulder.
    /// It requires a 'InferredBodyPosition' component on the same script.
    /// </summary>
    [RequireComponent(typeof(InferredBodyPositions))]
    public class FarFieldDirection : MonoBehaviour
    {
        /// <summary>
        /// the origin of the ray
        /// </summary>
        public enum RayOrigin
        {
            SHOULDER,
            ELBOW,
            WRIST,
            WRIST_SHOULDER_LERP
        }

        /// <summary>
        /// the position that the ray goes through
        /// </summary>
        public enum AimPosition
        {
            PREDICTED_PINCH,
            STABLE_PINCH,
        }

        /// <summary>
        /// a struct holding a hand and a shoulder position
        /// </summary>
        public struct HandShoulder
        {
            public Hand Hand;
            public Vector3 ShoulderPosition;
        }

        /// <summary>
        /// event that is called after new rays directions have been calculated. Subscribe to it to cast rays with the far field hand directions it provides.
        /// </summary>
        public static event Action<FarFieldHandDirection[]> OnFarFieldHandDirectionFrame;

        [Header("Raycast")]
        /// <summary>
        /// the aim position specifies the position that the ray goes through
        /// </summary>
        public AimPosition aimPosition = AimPosition.STABLE_PINCH;
        /// <summary>
        /// the ray origin specifies where the ray originates
        /// </summary>
        public RayOrigin rayOrigin = RayOrigin.SHOULDER;
        /// <summary>
        /// the elbow offset is only used when the rayOrigin is the elbow and specifies a position offset
        /// </summary>
        public Vector3 elbowOffset = Vector3.zero;

        /// <summary>
        /// The one Euro Filter reduces jitter and increases stability
        /// </summary>
        public bool useOneEuroFilter = true;

        [Header("WristOffset")]
        /// <summary>
        /// the wrist offset is only used when the rayOrigin is the wrist or wristShoulderLerp and 
        /// specifies a position offset
        /// </summary>
        public Vector3 wristOffset = new Vector3(0.0425f, 0.0652f, 0.0f);
        /// <summary>
        /// the wrist shoulder lerp amount is only used when the rayOrigin is wristShoulderLerp. 
        /// It specifies how much the wrist vs the shoulder is used as a ray origin.
        /// </summary>
        [Range(0.01f, 1)] public float wristShoulderLerpAmount;

        /// <summary>
        /// an array holding the left hand and shoulder and the right hand and shoulder
        /// </summary>
        [HideInInspector] public HandShoulder[] HandShoulders = new HandShoulder[2];
        /// <summary>
        /// an array holding left and right ray directions
        /// </summary>
        [HideInInspector] public FarFieldHandDirection[] FarFieldRays = new FarFieldHandDirection[2];

        private InferredBodyPositions inferredBodyPositions;
        private Transform transformHelper;
        private OneEuroFilter<Vector3>[] aimPositionFilters;
        private OneEuroFilter<Vector3>[] rayOriginFilters;

        // Debug filters used in case we need to output a value which isn't currently being filtered.
        // E.g. if we want to compare a different aim position to the one being used
        private OneEuroFilter<Vector3>[] debugAimPositionFilters;
        /// <summary>
        /// parameter beta for the one euro filter (see https://cristal.univ-lille.fr/~casiez/1euro/)
        /// </summary>
        public float oneEuroBeta = 100;
        /// <summary>
        /// parameter minCutoff for the one euro filter (see https://cristal.univ-lille.fr/~casiez/1euro/)
        /// </summary>
        public float oneEuroMinCutoff = 0.4f;
        private readonly float oneEurofreq = 30;

        // Start is called before the first frame update
        void Start()
        {
            transformHelper = new GameObject("FarFieldRaycast_TransformHelper").transform;
            transformHelper.SetParent(transform);

            if (inferredBodyPositions == null)
            {
                inferredBodyPositions = GetComponent<InferredBodyPositions>();
            }

            aimPositionFilters = new OneEuroFilter<Vector3>[2] { new OneEuroFilter<Vector3>(oneEurofreq), new OneEuroFilter<Vector3>(oneEurofreq) };
            debugAimPositionFilters = new OneEuroFilter<Vector3>[2] { new OneEuroFilter<Vector3>(oneEurofreq), new OneEuroFilter<Vector3>(oneEurofreq) };
            rayOriginFilters = new OneEuroFilter<Vector3>[2] { new OneEuroFilter<Vector3>(oneEurofreq), new OneEuroFilter<Vector3>(oneEurofreq) };
        }

        // Update is called once per frame
        void Update()
        {
            if (Hands.Provider == null || Hands.Provider.CurrentFrame == null)
            {
                return;
            }

            for (int i = 0; i < aimPositionFilters.Length; i++)
            {
                aimPositionFilters[i].UpdateParams(oneEurofreq, oneEuroMinCutoff, oneEuroBeta);
                debugAimPositionFilters[i].UpdateParams(oneEurofreq, oneEuroMinCutoff, oneEuroBeta);
                rayOriginFilters[i].UpdateParams(oneEurofreq, oneEuroMinCutoff, oneEuroBeta);
            }

            PopulateHandShoulders();
            CastRays();
        }

        private void PopulateHandShoulders()
        {
            List<Hand> hands = Hands.Provider.CurrentFrame.Hands;

            HandShoulders[0].Hand = null;
            HandShoulders[1].Hand = null;

            foreach (Hand hand in hands)
            {
                int index = hand.IsLeft ? 0 : 1;
                HandShoulders[index].Hand = hand;
                HandShoulders[index].ShoulderPosition = inferredBodyPositions.ShoulderPositions[index];
            }
        }

        private void CastRays()
        {
            for (int i = 0; i < HandShoulders.Length; i++)
            {
                FarFieldRays[i].Hand = HandShoulders[i].Hand;
                if (HandShoulders[i].Hand == null)
                {
                    continue;
                }

                FarFieldRays[i].AimPositionRaw = GetAimPosition(HandShoulders[i], AimPosition.PREDICTED_PINCH);
                FarFieldRays[i].RayOriginRaw = GetRayOrigin(HandShoulders[i]);

                //Filtering using the One Euro filter reduces jitter from both positions
                FarFieldRays[i].AimPosition = GetAimPosition(HandShoulders[i]);
                FarFieldRays[i].RayOrigin = FarFieldRays[i].RayOriginRaw;

                if (useOneEuroFilter)
                {
                    //Filtering using the One Euro filter reduces jitter from both positions
                    FarFieldRays[i].AimPosition = aimPositionFilters[i].Filter(GetAimPosition(HandShoulders[i]), Time.time);
                    FarFieldRays[i].RayOrigin = rayOriginFilters[i].Filter(FarFieldRays[i].RayOriginRaw, Time.time);
                }

                FarFieldRays[i].Direction = (FarFieldRays[i].AimPosition - FarFieldRays[i].RayOrigin).normalized;

                FarFieldRays[i].DebugAimPosition = FarFieldRays[i].AimPosition;
                FarFieldRays[i].DebugRayOrigin = FarFieldRays[i].RayOrigin;
                FarFieldRays[i].DebugDirection = (FarFieldRays[i].DebugAimPosition - FarFieldRays[i].DebugRayOrigin).normalized;
            }
            OnFarFieldHandDirectionFrame?.Invoke(FarFieldRays);
        }

        private Vector3 GetAimPosition(HandShoulder handShoulder)
        {
            return GetAimPosition(handShoulder, aimPosition);
        }

        private Vector3 GetAimPosition(HandShoulder handShoulder, AimPosition aimPos)
        {
            switch (aimPos)
            {
                case AimPosition.PREDICTED_PINCH:
                    return handShoulder.Hand.GetPredictedPinchPosition();
                case AimPosition.STABLE_PINCH:
                default:
                    return inferredBodyPositions.StablePinchPosition[handShoulder.Hand.IsLeft ? 0 : 1];
            }
        }

        private Vector3 GetRayOrigin(HandShoulder handShoulder)
        {
            Vector3 newRayOrigin = Vector3.zero;
            switch (rayOrigin)
            {
                case RayOrigin.SHOULDER:
                    newRayOrigin = handShoulder.ShoulderPosition;
                    break;

                case RayOrigin.ELBOW:
                    newRayOrigin = GetElbowOffsetPosition(handShoulder);
                    break;

                case RayOrigin.WRIST:
                    newRayOrigin = GetWristOffsetPosition(handShoulder);
                    break;

                case RayOrigin.WRIST_SHOULDER_LERP:
                    newRayOrigin = Vector3.Lerp(GetWristOffsetPosition(handShoulder), handShoulder.ShoulderPosition, wristShoulderLerpAmount);
                    break;
            }
            return newRayOrigin;
        }

        private Vector3 GetWristOffsetPosition(HandShoulder handShoulder)
        {
            Vector3 worldWristPosition = wristOffset;
            if (handShoulder.Hand.IsRight)
            {
                worldWristPosition.x = -worldWristPosition.x;
            }

            transformHelper.transform.position = handShoulder.Hand.WristPosition.ToVector3();
            transformHelper.transform.rotation = handShoulder.Hand.Rotation.ToQuaternion();

            return transformHelper.TransformPoint(worldWristPosition);
        }

        private Vector3 GetElbowOffsetPosition(HandShoulder handShoulder)
        {
            Vector3 worldElbowPosition = elbowOffset;
            if (handShoulder.Hand.IsRight)
            {
                worldElbowPosition.x = -worldElbowPosition.x;
            }

            transformHelper.transform.position = handShoulder.Hand.Arm.ElbowPosition.ToVector3();
            transformHelper.transform.rotation = handShoulder.Hand.Arm.Rotation.ToQuaternion();
            return transformHelper.TransformPoint(worldElbowPosition);
        }
    }
#pragma warning restore 0618
}