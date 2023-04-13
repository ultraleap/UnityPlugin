/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
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
    public class InferredBodyPositions : MonoBehaviour
    {
        [Header("Inferred Eye Settings")]
        /// <summary>
        /// The eye's offset from the head position
        /// </summary>
        [Tooltip("The eye's offset from the head position.")]
        public Vector2 eyeOffset = new Vector2(0.03f, 0.01f);

        [Header("Inferred Neck Settings")]
        /// <summary>
        /// The neck's vertical offset from the head
        /// </summary>
        [Tooltip("The neck's vertical offset from the head")]
        public float neckOffset = -0.1f;

        /// <summary>
        /// Use a deadzone for a neck's y-rotation.
        /// If true, the neck's Yaw is not affected by the head's Yaw 
        /// until the head's Yaw has moved over a certain threshold - this has the
        /// benefit of keeping the neck's rotation fairly stable.
        /// </summary>
        [Tooltip("Use a deadzone for a neck's y-rotation.\n" +
            "If true, the neck's Yaw is not affected by the head's Yaw until the head's" +
            " Yaw has moved over a certain threshold - this has the benefit of keeping the" +
            " neck's rotation fairly stable.")]
        public bool useNeckYawDeadzone = true;

        /// <summary>
        /// Blends between the NeckPositionLocalOffset and the NeckPositionWorldOffset.
        /// At 0, only the local position is into account.
        /// At 1, only the world position is into account.
        /// A blend between the two stops head roll & pitch rotation (z & x rotation) 
        /// having a large effect on the neck position
        /// </summary>
        [Tooltip("Blends between the NeckPositionLocalOffset and the NeckPositionWorldOffset.\n" +
            " - At 0, only the local position is into account.\n" +
            " - At 1, only the world position is into account.\n" +
            " - A blend between the two stops head roll & pitch rotation (z & x rotation) " +
            "having a large effect on the neck position")]
        [Range(0f, 1)]
        public float worldLocalNeckPositionBlend = 0.5f;

        /// <summary>
        /// How quickly the neck rotation updates
        /// Used to smooth out sudden large rotations
        /// </summary>
        [Tooltip("How quickly the neck rotation updates\n" +
            "Used to smooth out sudden large rotations")]
        [Range(0.01f, 30)]
        public float neckRotationLerpSpeed = 22;

        [Header("Inferred Shoulder Settings")]
        /// <summary>
        /// Should the Neck Position be used to predict shoulder positions?
        /// If true, the shoulders are less affected by head roll & pitch rotation (z & x rotation)
        /// </summary>
        [Tooltip("Should the Neck Position be used to predict shoulder positions?\n" +
            "If true, the shoulders are less affected by head roll & pitch rotation (z & x rotation)")]
        public bool useNeckPositionForShoulders = true;

        /// <summary>
        /// The shoulder's horizontal offset from the neck.
        /// </summary>
        [Tooltip("The shoulder's horizontal offset from the neck.")]
        public float shoulderOffset = 0.1f;

        [Header("Inferred Hip Settings")]
        /// <summary>
        /// The hip's vertical offset from the shoulders.
        /// </summary>
        public float verticalHipOffset = -0.5f;

        [Header("Debug Gizmos")]
        [Tooltip("If true, draw any enabled debug gizmos")]
        public bool drawDebugGizmos = false;
        public Color debugGizmoColor = Color.green;
        public bool drawHeadPosition = true;
        public bool drawEyePositions = true;
        public bool drawNeckPosition = true;
        public bool drawShoulderPositions = true;
        public bool drawHipPositions = true;
        public bool drawConnectionBetweenHipsAndShoulders = true;

        private float headGizmoRadius = 0.09f;
        private float neckGizmoRadius = 0.02f;
        private float shoulderHipGizmoRadius = 0.02f;
        private float eyeGizmoRadius = 0.01f;
        private Quaternion storedNeckRotation;

        private EulerAngleDeadzone neckYawDeadzone;
        private Transform transformHelper;

        //Inferred Body Positions

        /// <summary>
        /// Inferred Neck position
        /// </summary>
        public Vector3 NeckPosition { get; private set; }

        /// <summary>
        /// Inferred neck position, based purely off of a local space offset to the head
        /// </summary>
        public Vector3 NeckPositionLocalSpace { get; private set; }

        /// <summary>
        /// Inferred neck position, based purely off of a world space offset to the head
        /// </summary>
        public Vector3 NeckPositionWorldSpace { get; private set; }

        /// <summary>
        /// Inferred neck rotation
        /// </summary>
        public Quaternion NeckRotation { get; private set; }

        /// <summary>
        /// Inferred shoulder position
        /// index:0 = left shoulder, index:1 = right shoulder
        /// </summary>
        public Vector3[] ShoulderPositions { get; private set; }

        /// <summary>
        /// Inferred shoulder position, based purely off of a local space offset to the head
        /// index:0 = left shoulder, index:1 = right shoulder
        /// </summary>
        public Vector3[] ShoulderPositionsLocalSpace { get; private set; }

        /// <summary>
        /// The head position, taken as the mainCamera from a LeapXRServiceProvider if found in the scene,
        /// otherwise Camera.main
        /// </summary>
        public Transform Head { get; private set; }

        /// <summary>
        /// The eye position, based off a local space offset from the head. 
        /// index:0 = left eye, index:1 = right eye
        /// </summary>
        public Vector3[] EyePositions { get; private set; }

        /// <summary>
        /// Inferred hip position. Based as a vertical offset from the shoulder positions
        /// index:0 = left hip, index:1 = right hip
        /// </summary>
        public Vector3[] HipPositions { get; private set; }

        /// <summary>
        /// Inferred waist position, calculated the midpoint between the two hips
        /// </summary>
        public Vector3 WaistPosition { get { return Vector3.Lerp(HipPositions[0], HipPositions[1], 0.5f); } }

        private void Start()
        {
            LeapXRServiceProvider leapXRServiceProvider = FindObjectOfType<LeapXRServiceProvider>();
            if (leapXRServiceProvider != null)
            {
                Head = leapXRServiceProvider.mainCamera.transform;
            }
            else
            {
                Head = Camera.main.transform;
            }
            transformHelper = new GameObject("InferredBodyPositions_TransformHelper").transform;
            transformHelper.SetParent(transform);

            ShoulderPositions = new Vector3[2];
            ShoulderPositionsLocalSpace = new Vector3[2];

            HipPositions = new Vector3[2];

            EyePositions = new Vector3[2];

            storedNeckRotation = Quaternion.identity;
            neckYawDeadzone = new EulerAngleDeadzone(25, true, 1.5f, 10, 1);
        }

        private void Update()
        {
            neckYawDeadzone.UpdateDeadzone(Head.rotation.eulerAngles.y);
            UpdateBodyPositions();
        }

        private void UpdateBodyPositions()
        {
            UpdateNeckPositions();
            UpdateNeckRotation();
            UpdateHeadOffsetShoulderPositions();
            UpdateShoulderPositions();
            UpdateHipPositions();
            UpdateEyePositions();
        }

        private void UpdateNeckPositions()
        {
            UpdateLocalOffsetNeckPosition();
            UpdateWorldOffsetNeckPosition();
            UpdateNeckPosition();
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

            transformHelper.position = Head.position;
            transformHelper.rotation = Head.rotation;
            NeckPositionLocalSpace = transformHelper.TransformPoint(localNeckOffset);
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
        private void UpdateHipPositions()
        {
            HipPositions[0] = ShoulderPositions[0];
            HipPositions[1] = ShoulderPositions[1];

            HipPositions[0].y += verticalHipOffset;
            HipPositions[1].y += verticalHipOffset;
        }

        private void UpdateEyePositions()
        {
            EyePositions[0] = Head.TransformPoint(-eyeOffset.x, eyeOffset.y, 0);
            EyePositions[1] = Head.TransformPoint(eyeOffset.x, eyeOffset.y, 0);
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !drawDebugGizmos)
            {
                return;
            }
            Gizmos.color = debugGizmoColor;

            if (drawHeadPosition)
            {
                Gizmos.matrix = Matrix4x4.TRS(Head.position, Head.rotation, Vector3.one);
                Gizmos.DrawCube(Vector3.zero, Vector3.one * headGizmoRadius);
                Gizmos.matrix = Matrix4x4.identity;
            }

            if (drawEyePositions)
            {
                Gizmos.DrawSphere(EyePositions[0], eyeGizmoRadius);
                Gizmos.DrawSphere(EyePositions[1], eyeGizmoRadius);
            }

            if (drawShoulderPositions)
            {
                Gizmos.DrawSphere(ShoulderPositions[0], shoulderHipGizmoRadius);
                Gizmos.DrawSphere(ShoulderPositions[1], shoulderHipGizmoRadius);
                Gizmos.DrawLine(ShoulderPositions[0], ShoulderPositions[1]);
            }

            if (drawNeckPosition)
            {
                Gizmos.DrawSphere(NeckPosition, neckGizmoRadius);
                Gizmos.DrawLine(Head.position, NeckPosition);
            }

            if (drawHipPositions)
            {
                Gizmos.color = debugGizmoColor;

                Gizmos.DrawSphere(HipPositions[0], shoulderHipGizmoRadius);
                Gizmos.DrawSphere(HipPositions[1], shoulderHipGizmoRadius);
                Gizmos.DrawLine(HipPositions[0], HipPositions[1]);
            }

            if (drawConnectionBetweenHipsAndShoulders)
            {
                Gizmos.DrawLine(HipPositions[0], ShoulderPositions[0]);
                Gizmos.DrawLine(HipPositions[1], ShoulderPositions[1]);
            }

        }
    }
}