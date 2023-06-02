/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Attributes;
using System.Collections;
using UnityEngine;

namespace Leap.Unity
{
    /**
     * Detects when specified fingers are pointing in the specified manner.
     * 
     * Directions can be specified relative to the global frame of reference, relative to 
     * the camera frame of reference, or using a combination of the two -- relative to the 
     * camera direction in the x-z plane, but not changing relative to the horizon.
     * 
     * You can alternatively specify a target game object.
     * 
     * If added to a HandModelBase instance or one of its children, this detector checks the
     * finger direction at the interval specified by the Period variable. You can also specify
     * which hand model to observe explicitly by setting handModel in the Unity editor or 
     * in code.
     * 
     * @since 4.1.2
     */
    public class FingerDirectionDetector : Detector
    {
        /**
         * The interval at which to check finger state.
         * @since 4.1.2
         */
        [Units("seconds")]
        [Tooltip("The interval in seconds at which to check this detector's conditions.")]
        [MinValue(0)]
        public float Period = .1f; //seconds

        /**
         * The HandModelBase instance to observe. 
         * Set automatically if not explicitly set in the editor.
         * @since 4.1.2
         */
        [Tooltip("The hand model to watch. Set automatically if detector is on a hand.")]
        public HandModelBase HandModel = null;

        /**
         * The finger to compare to the specified direction.
         * @since 4.1.2
         */
        [Tooltip("The finger to observe.")]
        public Finger.FingerType FingerName = Finger.FingerType.TYPE_INDEX;


        /**
         * Specifies how to interprete the direction specified by PointingDirection.
         * 
         * - RelativeToCamera -- the target direction is defined relative to the camera's forward vector, i.e. (0, 0, 1) is the cmaera's 
         *                       local forward direction.
         * - RelativeToHorizon -- the target direction is defined relative to the camera's forward vector, 
         *                        except that it does not change with pitch.
         * - RelativeToWorld -- the target direction is defined as a global direction that does not change with camera movement. For example,
         *                      (0, 1, 0) is always world up, no matter which way the camera is pointing.
         * - AtTarget -- a target object is used as the pointing direction (The specified PointingDirection is ignored).
         * 
         * In VR scenes, RelativeToHorizon with a direction of (0, 0, 1) for camera forward and RelativeToWorld with a direction
         * of (0, 1, 0) for absolute up, are often the most useful settings.
         * @since 4.1.2
         */
        [Header("Direction Settings")]
        [Tooltip("How to treat the target direction.")]
        public PointingType PointingType = PointingType.RelativeToHorizon;

        /**
         * The target direction as interpreted by the PointingType setting.
         * Ignored when Pointingtype is "AtTarget."
         * @since 4.1.2
         */
        [Tooltip("The target direction.")]
        [DisableIf("PointingType", isEqualTo: PointingType.AtTarget)]
        public Vector3 PointingDirection = Vector3.forward;

        /**
         * The object to point at when the PointingType is "AtTarget." Ignored otherwise.
         */
        [Tooltip("A target object(optional). Use PointingType.AtTarget")]
        [DisableIf("PointingType", isNotEqualTo: PointingType.AtTarget)]
        public Transform TargetObject = null;
        /**
         * The turn-on angle. The detector activates when the specified finger points within this
         * many degrees of the target direction.
         * @since 4.1.2
         */
        [Tooltip("The angle in degrees from the target direction at which to turn on.")]
        [Range(0, 180)]
        public float OnAngle = 15f; //degrees

        /**
        * The turn-off angle. The detector deactivates when the specified finger points more than this
        * many degrees away from the target direction. The off angle must be larger than the on angle.
        * @since 4.1.2
        */
        [Tooltip("The angle in degrees from the target direction at which to turn off.")]
        [Range(0, 180)]
        public float OffAngle = 25f; //degrees
        /** Whether to draw the detector's Gizmos for debugging. (Not every detector provides gizmos.)
         * @since 4.1.2 
         */
        [Header("")]
        [Tooltip("Draw this detector's Gizmos, if any. (Gizmos must be on in Unity edtor, too.)")]
        public bool ShowGizmos = true;

        private IEnumerator watcherCoroutine;

        private void OnValidate()
        {
            if (OffAngle < OnAngle)
            {
                OffAngle = OnAngle;
            }
        }

        private void Awake()
        {
            watcherCoroutine = fingerPointingWatcher();
        }

        private void OnEnable()
        {
            StartCoroutine(watcherCoroutine);
        }

        private void OnDisable()
        {
            StopCoroutine(watcherCoroutine);
            Deactivate();
        }

        private IEnumerator fingerPointingWatcher()
        {
            Hand hand;
            Vector3 fingerDirection;
            Vector3 targetDirection;
            int selectedFinger = selectedFingerOrdinal();
            while (true)
            {
                if (HandModel != null && HandModel.IsTracked)
                {
                    hand = HandModel.GetLeapHand();
                    if (hand != null)
                    {
                        targetDirection = selectedDirection(hand.Fingers[selectedFinger].TipPosition);
                        fingerDirection = hand.Fingers[selectedFinger].Bone(Bone.BoneType.TYPE_DISTAL).Direction;
                        float angleTo = Vector3.Angle(fingerDirection, targetDirection);
                        if (HandModel.IsTracked && angleTo <= OnAngle)
                        {
                            Activate();
                        }
                        else if (!HandModel.IsTracked || angleTo >= OffAngle)
                        {
                            Deactivate();
                        }
                    }
                }
                yield return new WaitForSeconds(Period);
            }
        }

        private Vector3 selectedDirection(Vector3 tipPosition)
        {
            switch (PointingType)
            {
                case PointingType.RelativeToHorizon:
                    Quaternion cameraRot = Camera.main.transform.rotation;
                    float cameraYaw = cameraRot.eulerAngles.y;
                    Quaternion rotator = Quaternion.AngleAxis(cameraYaw, Vector3.up);
                    return rotator * PointingDirection;
                case PointingType.RelativeToCamera:
                    return Camera.main.transform.TransformDirection(PointingDirection);
                case PointingType.RelativeToWorld:
                    return PointingDirection;
                case PointingType.AtTarget:
                    return TargetObject.position - tipPosition;
                default:
                    return PointingDirection;
            }
        }

        private int selectedFingerOrdinal()
        {
            switch (FingerName)
            {
                case Finger.FingerType.TYPE_INDEX:
                    return 1;
                case Finger.FingerType.TYPE_MIDDLE:
                    return 2;
                case Finger.FingerType.TYPE_PINKY:
                    return 4;
                case Finger.FingerType.TYPE_RING:
                    return 3;
                case Finger.FingerType.TYPE_THUMB:
                    return 0;
                default:
                    return 1;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (ShowGizmos && HandModel != null && HandModel.IsTracked)
            {
                Color innerColor;
                if (IsActive)
                {
                    innerColor = OnColor;
                }
                else
                {
                    innerColor = OffColor;
                }
                Finger finger = HandModel.GetLeapHand().Fingers[selectedFingerOrdinal()];
                Vector3 fingerDirection = finger.Bone(Bone.BoneType.TYPE_DISTAL).Direction;
                Leap.Unity.Utils.DrawCone(finger.TipPosition, fingerDirection, OnAngle, finger.Length, innerColor);
                Leap.Unity.Utils.DrawCone(finger.TipPosition, fingerDirection, OffAngle, finger.Length, LimitColor);
                Gizmos.color = DirectionColor;
                Gizmos.DrawRay(finger.TipPosition, selectedDirection(finger.TipPosition));
            }
        }
#endif
    }
}