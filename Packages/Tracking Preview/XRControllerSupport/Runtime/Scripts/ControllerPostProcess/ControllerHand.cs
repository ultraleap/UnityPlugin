/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem;
#endif

namespace Leap.Controllers
{
    /// <summary>
    /// Takes in XR Controller data and converts it to Leap Hand Data
    /// </summary>
    [System.Serializable]
    public class ControllerHand
    {
        /// <summary>
        /// Represents a controller axis as a finger (e.g. translates trigger to index finger)
        /// </summary>
        [System.Serializable]
        public class ControllerFinger
        {
            [HideInInspector]
            public string name = "";

            [Tooltip("If enabled, in the legacy input system this will be read as an axis which varies between 0 & 1. If disabled it will be read as a button, which can be either on or off.")]
            public bool analog = true;

            [Tooltip("If enabled, values will smoothly interpolate over interpolationTime to its current value")]
            public bool interpolate = true;

            [Tooltip("The amount of time it takes to interpolate to the current value")]
            public float interpolationTime = 0.04f;

            [Tooltip("The axes this finger is bound to.")]
            public List<string> axes = new List<string>();

            [Tooltip("If enabled, finger joint rotations will be compounded, meaning the metacarpal value will be added to the proximal etc.")]
            public bool compoundRotations = true;

            [Tooltip("Euler angles. These values are compounded by default, meaning the metacarpal value will be added to the proximal etc.")]
            public Vector3 metacarpalRotation = Vector3.zero, proximalRotation = Vector3.zero, intermediateRotation = Vector3.zero, distalRotation = Vector3.zero;

            [HideInInspector] public float value = 0;
#if ENABLE_INPUT_SYSTEM
            private InputAction _inputAction;
            private System.Type _inputType = null;

            public void CreateAction()
            {
                _inputAction = new InputAction(name);
                for (int i = 0; i < axes.Count; i++)
                {
                    _inputAction.AddBinding().WithPath(axes[i]);
                }
                _inputAction.Enable();
            }
#endif

            public void Update()
            {
                float result = 0;
#if ENABLE_INPUT_SYSTEM
                if (_inputType == null && _inputAction.activeControl != null)
                {
                    _inputType = _inputAction.activeControl.valueType;
                }
                try
                {
                    if (_inputType == typeof(Vector2))
                    {
                        result = _inputAction.ReadValue<Vector2>().magnitude;
                    }
                    else if (_inputType == typeof(Vector3))
                    {
                        result = _inputAction.ReadValue<Vector3>().magnitude;
                    }
                    else
                    {
                        result = _inputAction.ReadValue<float>();
                    }
                }
                catch
                {
                    Debug.LogError("Unable to read input of " + _inputAction.name);
                }

#else
                float temp = 0;
                for (int i = 0; i < this.axes.Count; i++)
                {
                    if (this.analog)
                    {
                        temp = Mathf.Abs(Input.GetAxis(this.axes[i]));
                        if (temp > result)
                        {
                            result = temp;
                        }
                    }
                    else
                    {
                        if (Input.GetButton(this.axes[i]))
                        {
                            result = 1;
                            break;
                        }
                    }
                }
#endif
                if (interpolate)
                {
                    this.value = Mathf.Lerp(this.value, result, 1f / interpolationTime * Time.deltaTime);
                }
                else
                {
                    this.value = result;
                }
            }
        }

#if ENABLE_INPUT_SYSTEM
        [HideInInspector]
        public XRController controller;
        [Tooltip("An offset to the device position read in from the input system")]
        public Vector3 offsetPosition;

        public Vector3 ControllerPosition
        {
            get
            {
                if (!controller.added)
                {
                    return Vector3.zero;
                }
                return controller.devicePosition.ReadValue() + (ControllerRotation * offsetPosition);
            }
        }

        public Quaternion ControllerRotation
        {
            get
            {
                if (!controller.added)
                {
                    return Quaternion.identity;
                }
                return controller.deviceRotation.ReadValue() * Quaternion.Euler(offsetRotationEuler);
            }
        }

#else
        public Transform transform = null;
#endif
        private Hand _hand;
        public Hand Hand
        {
            get
            {
                Hand h = new Hand();
                if (_hand == null)
                {
                    _hand = GenerateControllerHand();
                }
                h.CopyFrom(_hand); return h;
            }
        }

        private Vector3 _oldPosition = Vector3.zero;
        private Vector3 _currentPosition;
        private Quaternion _currentRotation;

        [Tooltip("An offset to the device rotation read in from the input system")]
        public Vector3 offsetRotationEuler;
        public ControllerFinger[] fingers = new ControllerFinger[5];

        private Chirality _chirality = Chirality.Left;

        [HideInInspector]
        public float timeVisible = 0;
        private float _oldTime = 0;

        public ControllerHand(Chirality chirality)
        {
            this._chirality = chirality;
        }

        #region Properties
        public ControllerFinger Thumb
        {
            get
            {
                return fingers[0];
            }
            set
            {
                fingers[0] = value;
            }
        }

        public ControllerFinger IndexFinger
        {
            get
            {
                return fingers[1];
            }
            set
            {
                fingers[1] = value;
            }
        }

        public ControllerFinger MiddleFinger
        {
            get
            {
                return fingers[2];
            }
            set
            {
                fingers[2] = value;
            }
        }

        public ControllerFinger RingFinger
        {
            get
            {
                return fingers[3];
            }
            set
            {
                fingers[3] = value;
            }
        }

        public ControllerFinger PinkyFinger
        {
            get
            {
                return fingers[4];
            }
            set
            {
                fingers[4] = value;
            }
        }
        #endregion

        public void Setup(Chirality chirality)
        {
            this._chirality = chirality;
            _oldTime = Time.time;
#if ENABLE_INPUT_SYSTEM
            switch (this._chirality)
            {
                case Chirality.Left:
                    controller = XRController.leftHand;
                    break;
                case Chirality.Right:
                    controller = XRController.rightHand;
                    break;
            }
            for (int i = 0; i < fingers.Length; i++)
            {
                fingers[i].CreateAction();
            }
            if (controller != null)
            {
                _oldPosition = ControllerPosition;
            }
#else
            _oldPosition = transform.position;
#endif
        }

        public void Update()
        {
            this.timeVisible += Time.deltaTime;
            for (int i = 0; i < this.fingers.Length; i++)
            {
                this.fingers[i].Update();
            }
            UpdateControllerVariables();
            this._hand = GenerateControllerHand();
        }

        /// <summary>
        /// Updates position and rotation based on controller position and rotation
        /// </summary>
        private void UpdateControllerVariables()
        {
#if ENABLE_INPUT_SYSTEM
            _currentPosition = this.ControllerPosition;
            _currentRotation = this.ControllerRotation;
#else
            _currentPosition = this.transform.position;
            _currentRotation = this.transform.rotation * Quaternion.Euler(offsetRotationEuler);
#endif
        }

        #region Generate Fingers

        /// <summary>
        /// Generates fingers, axes & Hand Poses
        /// </summary>
        public void GenerateFingers()
        {
            for (int i = 0; i < this.fingers.Length; i++)
            {
                this.fingers[i] = new ControllerFinger();
            }
#if ENABLE_INPUT_SYSTEM
            GenerateInputaxes(this._chirality);
#else
            GenerateLegacyaxes(this._chirality);
#endif
            GenerateHandPoses(this._chirality);
        }

        /// <summary>
        /// Generates sensible default rotations for fingers
        /// </summary>
        /// <param name="chirality"></param>
        private void GenerateHandPoses(Chirality chirality)
        {
            this.Thumb.analog = false;
            this.Thumb.interpolate = true;
            this.Thumb.proximalRotation = new Vector3(-5, 0, 25);
            this.Thumb.intermediateRotation = new Vector3(-10, 0, -65);
            this.Thumb.distalRotation = new Vector3(0, 0, -45);
            this.Thumb.name = $"{chirality} Thumb";

            this.IndexFinger.interpolate = true;
            this.IndexFinger.proximalRotation = new Vector3(-70, -10, 0);
            this.IndexFinger.intermediateRotation = new Vector3(-45, 0, -5);
            this.IndexFinger.distalRotation = new Vector3(-45, 0, 0);
            this.IndexFinger.name = $"{chirality} Index";

            this.MiddleFinger.interpolate = true;
            this.MiddleFinger.metacarpalRotation = new Vector3(0, -2, 0);
            this.MiddleFinger.proximalRotation = new Vector3(-65, 1, 0);
            this.MiddleFinger.intermediateRotation = new Vector3(-55, 0, 2);
            this.MiddleFinger.distalRotation = new Vector3(-40, -6, 3);
            this.MiddleFinger.name = $"{chirality} Middle";

            this.RingFinger.interpolate = true;
            this.RingFinger.proximalRotation = new Vector3(-65, 8, 0);
            this.RingFinger.intermediateRotation = new Vector3(-50, 12, 0);
            this.RingFinger.distalRotation = new Vector3(-40, -35, 14);
            this.RingFinger.name = $"{chirality} Ring";

            this.PinkyFinger.interpolate = true;
            this.PinkyFinger.proximalRotation = new Vector3(-65, 15, 2);
            this.PinkyFinger.intermediateRotation = new Vector3(-40, 9, 5);
            this.PinkyFinger.distalRotation = new Vector3(-45, -20, 20);
            this.PinkyFinger.name = $"{chirality} Pinky";

            this.offsetRotationEuler = new Vector3(-10, 260, 235);
            if (chirality == Chirality.Right)
            {
                this.offsetRotationEuler.y *= -1;
                this.offsetRotationEuler.z *= -1;

                for (int i = 0; i < this.fingers.Length; i++)
                {
                    this.fingers[i].metacarpalRotation.y *= -1;
                    this.fingers[i].proximalRotation.y *= -1;
                    this.fingers[i].intermediateRotation.y *= -1;
                    this.fingers[i].distalRotation.y *= -1;
                    this.fingers[i].metacarpalRotation.z *= -1;
                    this.fingers[i].proximalRotation.z *= -1;
                    this.fingers[i].intermediateRotation.z *= -1;
                    this.fingers[i].distalRotation.z *= -1;
                }
            }
        }
#if ENABLE_INPUT_SYSTEM

        /// <summary>
        /// Links controller axes to fingers
        /// </summary>
        /// <param name="chirality"></param>
        private void GenerateInputaxes(Chirality chirality)
        {
            string hand = chirality.ToString();

            this.Thumb.axes = new List<string>() { "<XRController>{" + hand + "Hand}/touchpadTouched",
                "<XRController>{" + hand + "Hand}/touchpadClicked",
                "<XRController>{" + hand + "Hand}/primaryTouched",
                "<XRController>{" + hand + "Hand}/secondaryTouched",
                "<XRController>{" + hand + "Hand}/thumbstickTouched"};

            this.IndexFinger.axes = new List<string>() { "<XRController>{" + hand + "Hand}/trigger" };

            this.MiddleFinger.axes = new List<string>() { "<XRController>{" + hand + "Hand}/grip" };
            this.RingFinger.axes = new List<string>() { "<XRController>{" + hand + "Hand}/grip" };
            this.PinkyFinger.axes = new List<string>() { "<XRController>{" + hand + "Hand}/grip" };
        }
#else
        /// <summary>
        /// Links legacy controller axes to fingers
        /// </summary>
        /// <param name="chirality"></param>
        private void GenerateLegacyaxes(Chirality chirality)
        {
            string hand = chirality.ToString();

            this.Thumb.axes.Add($"XRI_{hand}_Primary2DAxisTouch");
            this.Thumb.axes.Add($"XRI_{hand}_Primary2DAxisClick");
            this.Thumb.axes.Add($"XRI_{hand}_PrimaryButton");

            this.IndexFinger.axes.Add($"XRI_{hand}_Trigger");

            this.MiddleFinger.axes.Add($"XRI_{hand}_Grip");

            this.RingFinger.axes.Add($"XRI_{hand}_Grip");

            this.PinkyFinger.axes.Add($"XRI_{hand}_Grip");
        }
#endif
        #endregion

        #region Generate Hands

        /// <summary>
        /// Uses ControllerFingers to generate a LeapHand
        /// </summary>
        /// <returns></returns>
        private Hand GenerateControllerHand()
        {
            //TODO: this could do with being changed so it doesn't generate each time.
            // annoying to do because finger positions are in world space, meaning transforms are hard to carry between frames.
            Hand h = TestHandFactory.MakeTestHand(_chirality == Chirality.Left);
            SetFingerSizes(ref h, 0.013f);

            // Thumb
            RotateFinger(h.fingers[0], this.Thumb);

            // index Finger
            RotateFinger(h.fingers[1], this.IndexFinger);

            // Grip Fingers
            RotateFinger(h.fingers[2], this.MiddleFinger);
            RotateFinger(h.fingers[3], this.RingFinger);
            RotateFinger(h.fingers[4], this.PinkyFinger);

            h.Transform(_currentPosition, _currentRotation);

            // If this Time.time -_oldTime = 0 then this has been called on the same frame, so we don't want to recalc palmVelocity
            if (Time.time - _oldTime != 0)
            {
                h.PalmVelocity = (_currentPosition - _oldPosition) / (Time.time - _oldTime);
            }

            _oldPosition = _currentPosition;

            h.TimeVisible = this.timeVisible;
            h.Id = _chirality == Chirality.Left ? 0 : 1;
            this._oldTime = Time.time;
            return h;
        }

        private void SetFingerSizes(ref Hand hand, float boneWidth)
        {
            for (int i = 0; i < hand.fingers.Length; i++)
            {
                for (int j = 0; j < hand.fingers[i].bones.Length; j++)
                {
                    hand.fingers[i].bones[j].Width = boneWidth;
                }
            }
        }
        /// <summary>
        /// Generates a Leap Finger from ControllerFinger
        /// </summary>
        /// <param name="leapFinger"></param>
        /// <param name="finger"></param>
        private void RotateFinger(Finger leapFinger, ControllerFinger finger)
        {
            RotateJoint(ref leapFinger.bones[0], ref leapFinger.bones[1], Vector3.Slerp(Vector3.zero, finger.metacarpalRotation, finger.value));
            if (finger.compoundRotations)
            {
                RotateJoint(ref leapFinger.bones[1], ref leapFinger.bones[2], Vector3.Slerp(Vector3.zero, finger.metacarpalRotation + finger.proximalRotation, finger.value));
                RotateJoint(ref leapFinger.bones[2], ref leapFinger.bones[3], Vector3.Slerp(Vector3.zero, finger.metacarpalRotation + finger.proximalRotation + finger.intermediateRotation, finger.value));
                RotateJoint(ref leapFinger.bones[3], ref leapFinger.bones[3], Vector3.Slerp(Vector3.zero, finger.metacarpalRotation + finger.proximalRotation + finger.intermediateRotation + finger.distalRotation, finger.value));
            }
            else
            {
                RotateJoint(ref leapFinger.bones[1], ref leapFinger.bones[2], Vector3.Slerp(Vector3.zero, finger.proximalRotation, finger.value));
                RotateJoint(ref leapFinger.bones[2], ref leapFinger.bones[3], Vector3.Slerp(Vector3.zero, finger.intermediateRotation, finger.value));
                RotateJoint(ref leapFinger.bones[3], ref leapFinger.bones[3], Vector3.Slerp(Vector3.zero, finger.distalRotation, finger.value));
            }
        }

        private void RotateJoint(ref Bone b, ref Bone b2, Vector3 deg)
        {
            if (deg == Vector3.zero)
                return;

            b.NextJoint = RotatePointAroundPivot(b.NextJoint, b.PrevJoint, deg);
            Vector3 diffPos = b.NextJoint - b2.PrevJoint;
            if (b2 != b)
            {
                b2.PrevJoint += diffPos;
                b2.NextJoint += diffPos;
            }
            b.Center = (b.PrevJoint + b.NextJoint) / 2.0f;

            if (b.Length < float.Epsilon)
            {
                b.Direction = Vector3.zero;
            }
            else
            {
                b.Direction = (b.NextJoint - b.PrevJoint) / b.Length;
            }

            b.Rotation = Quaternion.Euler(deg) * b.Rotation;
        }

        public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
        {
            return Quaternion.Euler(angles) * (point - pivot) + pivot;
        }

        #endregion
    }
}