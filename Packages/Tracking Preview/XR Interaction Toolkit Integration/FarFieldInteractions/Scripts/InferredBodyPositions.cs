/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap;
using Leap.Unity;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Preview.FarFieldInteractions
{
    /// <summary>
    /// infers a body position from .. including predicted shoulder positions and stable pinch position.
    /// Used by the 'FarFieldDirection', 
    /// and requires a 'RotationDeadzone' component on the same script.
    /// </summary>
    [RequireComponent(typeof(RotationDeadzone))]
    public class InferredBodyPositions : MonoBehaviour
    {
        /// <summary>
        /// should the NeckRotation be used to predict shoulder positions?
        /// </summary>
        public bool useNeckPositionForShoulders = true;
        /// <summary>
        /// use a deadzone for a neck's y-rotation
        /// </summary>
        public bool useNeckYawDeadzone = true;
        /// <summary>
        /// the shoulder offset from the neck
        /// </summary>
        public float shoulderOffset = 0.1f;
        /// <summary>
        /// the neck offset from the head
        /// </summary>
        public float neckOffset = -0.1f;
        /// <summary>
        /// blend between the NeckPositionLocalOffset and the NeckPositionWorldOffset
        /// </summary>
        [Range(0.01f, 1)] public float WorldLocalNeckPositionBlend = 0.5f;
        /// <summary>
        /// how quickly to update the neck rotation
        /// </summary>
        [Range(0.01f, 30)] public float NeckRotationLerpSpeed = 22;

        // Debug gizmo settings
        public bool drawDebugGizmos = true;
        public float gizmoRadius = 0.05f;
        public bool drawRawHead = true;
        public bool drawNeckPosition = true;
        public bool drawNeckOffset = true;
        public bool drawRawShoulderOffset = true;
        public bool drawRecentredShoulders = true;
        public bool drawDeadzonedShoulders = true;
        public bool drawPinchPositions = true;

        private RotationDeadzone neckYawDeadzone;
        private Transform head;
        private Transform transformHelper;

        //Inferred Body Positions

        /// <summary>
        /// Inferred Neck position
        /// </summary>
        public Vector3 NeckPosition { get; private set; }
        /// <summary>
        /// Inferred local offset of the neck position (relative to head)
        /// </summary>
        public Vector3 NeckPositionLocalOffset { get; private set; }
        /// <summary>
        /// Inferred world offset of the neck position (relative to head)
        /// </summary>
        public Vector3 NeckPositionWorldOffset { get; private set; }
        /// <summary>
        /// Inferred neck rotation
        /// </summary>
        public Quaternion NeckRotation { get; private set; }

        /// <summary>
        /// Inferrerd shoulder position
        /// </summary>
        public Vector3[] ShoulderPositions { get; private set; }
        /// <summary>
        /// Inferred recentred shoulder position
        /// </summary>
        public Vector3[] ShoulderPositionsRecentred { get; private set; }
        /// <summary>
        /// Inferred shoulder position offset (relative to head)
        /// </summary>
        public Vector3[] ShoulderPositionsHeadOffset { get; private set; }
        /// <summary>
        /// Inferred Stable pinch position. Can be used instead of the normal pinch position
        /// </summary>
        public Vector3[] StablePinchPosition { get; private set; }

        private void Start()
        {
            head = Camera.main.transform;
            transformHelper = new GameObject("InferredBodyPositions_TransformHelper").transform;
            transformHelper.SetParent(transform);

            ShoulderPositions = new Vector3[2];
            ShoulderPositionsRecentred = new Vector3[2];
            ShoulderPositionsHeadOffset = new Vector3[2];
            StablePinchPosition = new Vector3[2];

            if (neckYawDeadzone == null)
            {
                neckYawDeadzone = GetComponent<RotationDeadzone>();
            }
        }

        private void Update()
        {
            neckYawDeadzone.UpdateDeadzone(head.rotation.eulerAngles.y);
            UpdateBodyPositions();
        }

        private void UpdateBodyPositions()
        {
            UpdateNeckPositions();
            UpdateNeckRotation();
            UpdateHeadOffsetShoulderPositions();
            UpdateShoulderPositions();
            UpdateRecentredShoulderPositions();
            UpdatePinchPosition();
        }

        private void UpdatePinchPosition()
        {
            if (Hands.Provider == null || Hands.Provider.CurrentFrame == null)
            {
                return;
            }

            List<Hand> hands = Hands.Provider.CurrentFrame.Hands;
            foreach (Hand hand in hands)
            {
                UpdateStablePinchPosition(hand);
            }
        }

        // Predicted Pinch Position without influence from the thumb or index tip
        private void UpdateStablePinchPosition(Hand hand)
        {
            int index = hand.IsLeft ? 0 : 1;

            // The stable pinch point is a rigid point in hand-space linearly offset by the
            // index finger knuckle position and scaled by the index finger's length

            Vector3 indexKnuckle = hand.Fingers[1].bones[1].PrevJoint.ToVector3();
            float indexLength = hand.Fingers[1].Length;
            Vector3 radialAxis = hand.RadialAxis();
            Vector3 predictedPinchPoint = indexKnuckle + hand.PalmarAxis() * indexLength * 0.85F
                                                       + hand.DistalAxis() * indexLength * 0.20F
                                                       + radialAxis * indexLength * 0.20F;
            StablePinchPosition[index] = predictedPinchPoint;
        }

        private void UpdateNeckPositions()
        {
            UpdateLocalOffsetNeckPosition();
            UpdateWorldOffsetNeckPosition();
            UpdateNeckPosition();
        }

        private void UpdateNeckPosition()
        {
            NeckPosition = Vector3.Lerp(NeckPositionLocalOffset, NeckPositionWorldOffset, WorldLocalNeckPositionBlend);
        }

        private void UpdateNeckRotation()
        {
            float neckYRotation = useNeckYawDeadzone ? neckYawDeadzone.DeadzoneCentre : head.rotation.eulerAngles.y;
            NeckRotation = Quaternion.Lerp(NeckRotation, Quaternion.Euler(0, neckYRotation, 0), Time.deltaTime * NeckRotationLerpSpeed);
        }

        private void UpdateLocalOffsetNeckPosition()
        {
            Vector3 localNeckOffset = new Vector3()
            {
                x = 0,
                y = neckOffset,
                z = 0
            };

            transformHelper.position = head.position;
            transformHelper.rotation = head.rotation;
            NeckPositionLocalOffset = transformHelper.TransformPoint(localNeckOffset);
        }

        private void UpdateWorldOffsetNeckPosition()
        {
            Vector3 worldNeckOffset = new Vector3()
            {
                x = 0,
                y = neckOffset,
                z = 0
            };

            Vector3 headPosition = head.position;
            NeckPositionWorldOffset = headPosition + worldNeckOffset;
        }

        private void UpdateHeadOffsetShoulderPositions()
        {
            ShoulderPositionsHeadOffset[0] = head.TransformPoint(-shoulderOffset, neckOffset, 0);
            ShoulderPositionsHeadOffset[1] = head.TransformPoint(shoulderOffset, neckOffset, 0);
        }

        private void UpdateShoulderPositions()
        {
            if (useNeckPositionForShoulders)
            {
                ShoulderPositions[0] = GetShoulderPosAtRotation(true, NeckRotation);
                ShoulderPositions[1] = GetShoulderPosAtRotation(false, NeckRotation);
            }
            else
            {
                ShoulderPositions = ShoulderPositionsHeadOffset;
            }
        }

        private void UpdateRecentredShoulderPositions()
        {
            Quaternion neckRotation = Quaternion.Euler(0, head.rotation.eulerAngles.y + neckYawDeadzone.RecentredPositionDelta, 0);

            ShoulderPositionsRecentred[0] = GetShoulderPosAtRotation(true, neckRotation);
            ShoulderPositionsRecentred[1] = GetShoulderPosAtRotation(false, neckRotation);
        }

        private Vector3 GetShoulderPosAtRotation(bool isLeft, Quaternion neckRotation)
        {
            transformHelper.position = NeckPosition;
            transformHelper.rotation = neckRotation;

            Vector3 shoulderNeckOffset = new Vector3
            {
                x = isLeft ? -shoulderOffset : shoulderOffset,
                y = 0,
                z = 0
            };

            return transformHelper.TransformPoint(shoulderNeckOffset);
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (!drawDebugGizmos)
            {
                return;
            }

            if (drawRawHead)
            {
                // Draw head
                Gizmos.color = Color.white;
                Gizmos.matrix = Matrix4x4.TRS(head.position, head.rotation, Vector3.one);
                Gizmos.DrawCube(Vector3.zero, Vector3.one * gizmoRadius);
                Gizmos.matrix = Matrix4x4.identity;
            }

            //Draw deadzoned shoulder positions 
            if (drawDeadzonedShoulders)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(ShoulderPositions[0], gizmoRadius);
                Gizmos.DrawSphere(ShoulderPositions[1], gizmoRadius);

                //Draw a line between both stable shoulder positions
                Gizmos.color = Color.white;
                Gizmos.DrawLine(ShoulderPositions[0], ShoulderPositions[1]);
            }

            if (drawRawShoulderOffset)
            {
                //Draw the non-stabilised shoulder positions
                Gizmos.color = Color.green;

                Gizmos.DrawSphere(ShoulderPositionsHeadOffset[0], gizmoRadius);
                Gizmos.DrawSphere(ShoulderPositionsHeadOffset[1], gizmoRadius);
                Gizmos.DrawLine(ShoulderPositionsHeadOffset[0], head.position);
                Gizmos.DrawLine(ShoulderPositionsHeadOffset[1], head.position);

            }

            if (drawRecentredShoulders)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(ShoulderPositionsRecentred[0], gizmoRadius);
                Gizmos.DrawWireSphere(ShoulderPositionsRecentred[1], gizmoRadius);
            }

            if (drawNeckOffset)
            {
                //Draw a line between both neck positions
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(NeckPositionLocalOffset, NeckPositionWorldOffset);

                //Draw the local offset neck position
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(NeckPositionLocalOffset, gizmoRadius / 4);
                Gizmos.DrawLine(head.position, NeckPositionLocalOffset);

                //Draw the world offset neck position
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(NeckPositionWorldOffset, gizmoRadius / 4);
                Gizmos.DrawLine(head.position, NeckPositionWorldOffset);
            }

            if (drawNeckPosition)
            {
                //Draw the neck position
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(NeckPosition, gizmoRadius / 4);
                Gizmos.DrawLine(head.position, NeckPosition);
            }

            if (drawPinchPositions)
            {
                List<Hand> hands = Hands.Provider.CurrentFrame.Hands;
                foreach (Hand hand in hands)
                {
                    int index = hand.IsLeft ? 0 : 1;
                    Gizmos.color = LeapColor.orange;
                    Gizmos.DrawSphere(hand.GetPredictedPinchPosition(), gizmoRadius / 5);
                    Gizmos.color = LeapColor.green;
                    Gizmos.DrawSphere(StablePinchPosition[index], gizmoRadius / 5);
                }
            }
        }
    }
}