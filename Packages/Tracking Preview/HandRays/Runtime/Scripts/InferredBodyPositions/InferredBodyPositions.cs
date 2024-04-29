/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Preview.HandRays
{
    /// <summary>
    /// Infers neck and shoulder positions using real world head data.
    /// </summary>
    public class InferredBodyPositions
    {
        private float neckOffset = -0.1f;
        private bool useNeckYawDeadzone = true;
        private float worldLocalNeckPositionBlend = 0.5f;
        private float neckRotationLerpSpeed = 22;
        
        private bool useNeckPositionForShoulders = true;
        private float shoulderOffset = 0.1f;

        private Quaternion storedNeckRotation;

        private EulerAngleDeadzone neckYawDeadzone;

        //Inferred Body Positions

        Vector3 NeckPosition;
        Vector3 NeckPositionLocalSpace;
        Vector3 NeckPositionWorldSpace;
        Quaternion NeckRotation;

        /// <summary>
        /// Inferred shoulder position
        /// index:0 = left shoulder, index:1 = right shoulder
        /// </summary>
        public Vector3[] ShoulderPositions { get; private set; }
        Vector3[] ShoulderPositionsLocalSpace;

        /// <summary>
        /// The head position, taken as the mainCamera from a LeapXRServiceProvider if found in the scene,
        /// otherwise Camera.main
        /// </summary>
        public Transform Head { get; private set; }

        public InferredBodyPositions()
        {
            Head = Camera.main.transform;

            ShoulderPositions = new Vector3[2];
            ShoulderPositionsLocalSpace = new Vector3[2];

            storedNeckRotation = Quaternion.identity;
            neckYawDeadzone = new EulerAngleDeadzone(25, true, 1.5f, 10, 1);
        }

        public void UpdatePositions()
        {
            neckYawDeadzone.UpdateDeadzone(Head.rotation.eulerAngles.y);

            UpdateLocalOffsetNeckPosition();
            UpdateWorldOffsetNeckPosition();
            UpdateNeckPosition();

            UpdateNeckRotation();
            UpdateHeadOffsetShoulderPositions();
            UpdateShoulderPositions();
        }

        private void UpdateNeckPosition()
        {
            NeckPosition = Vector3.Lerp(NeckPositionLocalSpace, NeckPositionWorldSpace, worldLocalNeckPositionBlend);
        }

        private void UpdateNeckRotation()
        {
            float neckYRotation = useNeckYawDeadzone ? neckYawDeadzone.DeadzoneCentre : Head.rotation.eulerAngles.y;

            Quaternion newNeckRotation = Quaternion.Euler(0, neckYRotation, 0);

            // Rotated too far in a single frame
            if (Quaternion.Angle(storedNeckRotation, newNeckRotation) > 15)
            {
                NeckRotation = newNeckRotation;
            }

            storedNeckRotation = newNeckRotation;

            NeckRotation = Quaternion.Lerp(NeckRotation, storedNeckRotation, Time.deltaTime * neckRotationLerpSpeed);
        }

        private void UpdateLocalOffsetNeckPosition()
        {
            Vector3 localNeckOffset = new Vector3()
            {
                x = 0,
                y = neckOffset,
                z = 0
            };

            NeckPositionLocalSpace = Head.TransformPoint(localNeckOffset);
        }

        private void UpdateWorldOffsetNeckPosition()
        {
            Vector3 worldNeckOffset = new Vector3()
            {
                x = 0,
                y = neckOffset,
                z = 0
            };

            Vector3 headPosition = Head.position;
            NeckPositionWorldSpace = headPosition + worldNeckOffset;
        }

        private void UpdateHeadOffsetShoulderPositions()
        {
            ShoulderPositionsLocalSpace[0] = Head.TransformPoint(-shoulderOffset, neckOffset, 0);
            ShoulderPositionsLocalSpace[1] = Head.TransformPoint(shoulderOffset, neckOffset, 0);
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
                ShoulderPositions = ShoulderPositionsLocalSpace;
            }
        }

        private Vector3 GetShoulderPosAtRotation(bool isLeft, Quaternion neckRotation)
        {
            Vector3 shoulderNeckOffset = new Vector3
            {
                x = isLeft ? -shoulderOffset : shoulderOffset,
                y = 0,
                z = 0
            };

            return Utils.TransformPoint(shoulderNeckOffset, NeckPosition, neckRotation);
        }
    }
}