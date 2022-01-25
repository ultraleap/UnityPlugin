/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;
using Hand = Leap.Hand;
using Bone = Leap.Bone;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem;
#endif


namespace Leap.Unity.Controllers
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
            public string Name = "";
            public bool Analog = true, Interpolate = true;
            public List<string> Axes = new List<string>();
            public float Value = 0;
            public float InterpolationTime = 0.04f;
            public bool CompoundRotations = true;
            [Tooltip("Euler angles. These values are compounded by default, meaning the metacarpal value will be added to the proximal etc.")]
            public Vector3 MetacarpalRotation = Vector3.zero, ProximalRotation = Vector3.zero, IntermediateRotation = Vector3.zero, DistalRotation = Vector3.zero;

#if ENABLE_INPUT_SYSTEM
            private InputAction _inputAction;
            private System.Type _inputType = null;

            public void CreateAction()
            {
                _inputAction = new InputAction(Name);
                for (int i = 0; i < Axes.Count; i++)
                {
                    _inputAction.AddBinding().WithPath(Axes[i]);
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
                for (int i = 0; i < this.Axes.Count; i++)
                {
                    try
                    {
                        if (this.Analog)
                        {
                            temp = Mathf.Abs(Input.GetAxis(this.Axes[i]));
                            if (temp > result)
                            {
                                result = temp;
                            }
                        }
                        else
                        {
                            if (Input.GetButton(this.Axes[i]))
                            {
                                result = 1;
                                break;
                            }
                        }
                    }
                    catch (System.ArgumentException e)
                    {
                        Debug.LogError("The controller post processor is reliant on the" +
                            " XR Legacy Input Helpers package when using the Legacy Input Module. Please add this package to your project" +
                            "and Seed XR Input Bindings.\n" + e.Message);
                    }
                }
#endif
                if (Interpolate)
                {
                    this.Value = Mathf.Lerp(this.Value, result, 1f / InterpolationTime * Time.deltaTime);
                }
                else
                {
                    this.Value = result;
                }
            }
        }

#if ENABLE_INPUT_SYSTEM
        [HideInInspector]
        public XRController Controller;
        public Vector3 OffsetPosition;

        public Vector3 ControllerPosition
        {
            get
            {
                if (!Controller.added)
                {
                    return Vector3.zero;
                }
                return Controller.devicePosition.ReadValue() + (ControllerRotation * OffsetPosition);
            }
        }

        public Quaternion ControllerRotation
        {
            get
            {
                if (!Controller.added)
                {
                    return Quaternion.identity;
                }
                return Controller.deviceRotation.ReadValue() * Quaternion.Euler(OffsetRotationEuler);
            }
        }

#else
        public Transform Transform = null;
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

        public Vector3 OffsetRotationEuler;
        public ControllerFinger[] Fingers = new ControllerFinger[5];

        private Chirality _chirality = Chirality.Left;

        [HideInInspector]
        public float TimeVisible = 0;
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
                return Fingers[0];
            }
            set
            {
                Fingers[0] = value;
            }
        }

        public ControllerFinger IndexFinger
        {
            get
            {
                return Fingers[1];
            }
            set
            {
                Fingers[1] = value;
            }
        }

        public ControllerFinger MiddleFinger
        {
            get
            {
                return Fingers[2];
            }
            set
            {
                Fingers[2] = value;
            }
        }

        public ControllerFinger RingFinger
        {
            get
            {
                return Fingers[3];
            }
            set
            {
                Fingers[3] = value;
            }
        }

        public ControllerFinger PinkyFinger
        {
            get
            {
                return Fingers[4];
            }
            set
            {
                Fingers[4] = value;
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
                    Controller = XRController.leftHand;
                    break;
                case Chirality.Right:
                    Controller = XRController.rightHand;
                    break;
            }
            for (int i = 0; i < Fingers.Length; i++)
            {
                Fingers[i].CreateAction();
            }
            if (Controller != null) { _oldPosition = ControllerPosition; }
#else
            _oldPosition = Transform.position;
#endif
        }

        public void Update()
        {
            this.TimeVisible += Time.deltaTime;
            for (int i = 0; i < this.Fingers.Length; i++)
            {
                this.Fingers[i].Update();
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
            _currentPosition = this.Transform.position;
            _currentRotation = this.Transform.rotation * Quaternion.Euler(OffsetRotationEuler);
#endif
        }

        #region Generate Fingers

        /// <summary>
        /// Generates fingers, axes & Hand Poses
        /// </summary>
        public void GenerateFingers()
        {
            for (int i = 0; i < this.Fingers.Length; i++)
            {
                this.Fingers[i] = new ControllerFinger();
            }
#if ENABLE_INPUT_SYSTEM
            GenerateInputAxes(this._chirality);
#else
            GenerateLegacyAxes(this._chirality);
#endif
            GenerateHandPoses(this._chirality);
        }

        /// <summary>
        /// Generates sensible default rotations for fingers
        /// </summary>
        /// <param name="chirality"></param>
        private void GenerateHandPoses(Chirality chirality)
        {
            this.Thumb.Analog = false;
            this.Thumb.Interpolate = true;
            this.Thumb.ProximalRotation = new Vector3(-5, 0, 25);
            this.Thumb.IntermediateRotation = new Vector3(-10, 0, -65);
            this.Thumb.DistalRotation = new Vector3(0, 0, -45);
            this.Thumb.Name = chirality.ToString() + " Thumb";

            this.IndexFinger.Interpolate = true;
            this.IndexFinger.ProximalRotation = new Vector3(-70, -10, 0);
            this.IndexFinger.IntermediateRotation = new Vector3(-45, 0, -5);
            this.IndexFinger.DistalRotation = new Vector3(-45, 0, 0);
            this.IndexFinger.Name = chirality.ToString() + " Index";

            this.MiddleFinger.Interpolate = true;
            this.MiddleFinger.MetacarpalRotation = new Vector3(0, -2, 0);
            this.MiddleFinger.ProximalRotation = new Vector3(-65, 1, 0);
            this.MiddleFinger.IntermediateRotation = new Vector3(-55, 0, 2);
            this.MiddleFinger.DistalRotation = new Vector3(-40, -6, 3);
            this.MiddleFinger.Name = chirality.ToString() + " Middle";

            this.RingFinger.Interpolate = true;
            this.RingFinger.ProximalRotation = new Vector3(-65, 8, 0);
            this.RingFinger.IntermediateRotation = new Vector3(-50, 12, 0);
            this.RingFinger.DistalRotation = new Vector3(-40, -35, 14);
            this.RingFinger.Name = chirality.ToString() + " Ring";

            this.PinkyFinger.Interpolate = true;
            this.PinkyFinger.ProximalRotation = new Vector3(-65, 15, 2);
            this.PinkyFinger.IntermediateRotation = new Vector3(-40, 9, 5);
            this.PinkyFinger.DistalRotation = new Vector3(-45, -20, 20);
            this.PinkyFinger.Name = chirality.ToString() + " Pinky";

            this.OffsetRotationEuler = new Vector3(-10, 260, 235);
            if (chirality == Chirality.Right)
            {
                this.OffsetRotationEuler.y *= -1;
                this.OffsetRotationEuler.z *= -1;
            }

            switch (chirality)
            {
                case Chirality.Right:
                    for (int i = 0; i < this.Fingers.Length; i++)
                    {
                        this.Fingers[i].MetacarpalRotation.y *= -1;
                        this.Fingers[i].ProximalRotation.y *= -1;
                        this.Fingers[i].IntermediateRotation.y *= -1;
                        this.Fingers[i].DistalRotation.y *= -1;
                        this.Fingers[i].MetacarpalRotation.z *= -1;
                        this.Fingers[i].ProximalRotation.z *= -1;
                        this.Fingers[i].IntermediateRotation.z *= -1;
                        this.Fingers[i].DistalRotation.z *= -1;
                    }
                    break;
            }
        }
#if ENABLE_INPUT_SYSTEM

        /// <summary>
        /// Links controller axes to fingers
        /// </summary>
        /// <param name="chirality"></param>
        private void GenerateInputAxes(Chirality chirality)
        {
            string hand = chirality.ToString();

            this.Thumb.Axes = new List<string>() { "<XRController>{" + hand + "Hand}/touchpadTouched",
                "<XRController>{" + hand + "Hand}/touchpadClicked",
                "<XRController>{" + hand + "Hand}/primaryTouched",
                "<XRController>{" + hand + "Hand}/secondaryTouched",
                "<XRController>{" + hand + "Hand}/thumbstickTouched"};

            this.IndexFinger.Axes = new List<string>() { "<XRController>{" + hand + "Hand}/trigger" };

            this.MiddleFinger.Axes = new List<string>() { "<XRController>{" + hand + "Hand}/grip" };
            this.RingFinger.Axes = new List<string>() { "<XRController>{" + hand + "Hand}/grip" };
            this.PinkyFinger.Axes = new List<string>() { "<XRController>{" + hand + "Hand}/grip" };
        }
#else
        /// <summary>
        /// Links legacy controller axes to fingers
        /// </summary>
        /// <param name="chirality"></param>
        private void GenerateLegacyAxes(Chirality chirality)
        {
            string hand = chirality.ToString();

            this.Thumb.Axes.Add("XRI_" + hand + "_Primary2DAxisTouch");
            this.Thumb.Axes.Add("XRI_" + hand + "_Primary2DAxisClick");
            this.Thumb.Axes.Add("XRI_" + hand + "_PrimaryButton");

            this.IndexFinger.Axes.Add("XRI_" + hand + "_Trigger");

            this.MiddleFinger.Axes.Add("XRI_" + hand + "_Grip");

            this.RingFinger.Axes.Add("XRI_" + hand + "_Grip");

            this.PinkyFinger.Axes.Add("XRI_" + hand + "_Grip");
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
            Hand h = TestHandFactory.MakeTestHand(_chirality == Chirality.Left, unitType: TestHandFactory.UnitType.UnityUnits);
            SetFingerSizes(ref h, 0.013f);

            // Thumb
            RotateFinger(h.Fingers[0], this.Thumb);

            // index Finger
            RotateFinger(h.Fingers[1], this.IndexFinger);

            // Grip Fingers
            RotateFinger(h.Fingers[2], this.MiddleFinger);
            RotateFinger(h.Fingers[3], this.RingFinger);
            RotateFinger(h.Fingers[4], this.PinkyFinger);

            h.Transform(_currentPosition, _currentRotation);

            // If this Time.time -_oldTime = 0 then this has been called on the same frame, so we don't want to recalc palmVelocity
            if (Time.time - _oldTime != 0)
            {
                h.PalmVelocity = ((_currentPosition - _oldPosition) / (Time.time - _oldTime)).ToVector();
            }

            _oldPosition = _currentPosition;

            h.TimeVisible = this.TimeVisible;
            h.Id = _chirality == Chirality.Left ? 0 : 1;
            this._oldTime = Time.time;
            return h;
        }

        private void SetFingerSizes(ref Hand hand, float boneSize)
        {
            for (int i = 0; i < hand.Fingers.Count; i++)
            {
                for (int j = 0; j < hand.Fingers[i].bones.Length; j++)
                {
                    hand.Fingers[i].bones[j].Width = boneSize;
                }
            }
        }
        /// <summary>
        /// Generates a Leap Finger from ControllerFinger
        /// </summary>
        /// <param name="f"></param>
        /// <param name="finger"></param>
        private void RotateFinger(Finger f, ControllerFinger finger)
        {
            RotateJoint(ref f.bones[0], ref f.bones[1], Vector3.Slerp(Vector3.zero, finger.MetacarpalRotation, finger.Value));
            if (finger.CompoundRotations)
            {
                RotateJoint(ref f.bones[1], ref f.bones[2], Vector3.Slerp(Vector3.zero, finger.MetacarpalRotation + finger.ProximalRotation, finger.Value));
                RotateJoint(ref f.bones[2], ref f.bones[3], Vector3.Slerp(Vector3.zero, finger.MetacarpalRotation + finger.ProximalRotation + finger.IntermediateRotation, finger.Value));
                RotateJoint(ref f.bones[3], ref f.bones[3], Vector3.Slerp(Vector3.zero, finger.MetacarpalRotation + finger.ProximalRotation + finger.IntermediateRotation + finger.DistalRotation, finger.Value));
            }
            else
            {
                RotateJoint(ref f.bones[1], ref f.bones[2], Vector3.Slerp(Vector3.zero, finger.ProximalRotation, finger.Value));
                RotateJoint(ref f.bones[2], ref f.bones[3], Vector3.Slerp(Vector3.zero, finger.IntermediateRotation, finger.Value));
                RotateJoint(ref f.bones[3], ref f.bones[3], Vector3.Slerp(Vector3.zero, finger.DistalRotation, finger.Value));
            }
        }

        private void RotateJoint(ref Bone b, ref Bone b2, Vector3 deg)
        {
            if (deg == Vector3.zero)
                return;

            b.NextJoint = RotatePointAroundPivot(b.NextJoint.ToVector3(), b.PrevJoint.ToVector3(), deg).ToVector();
            Vector diffPos = b.NextJoint - b2.PrevJoint;
            if (b2 != b)
            {
                b2.PrevJoint += diffPos;
                b2.NextJoint += diffPos;
            }
            b.Center = (b.PrevJoint + b.NextJoint) / 2.0f;

            if (b.Length < float.Epsilon)
            {
                b.Direction = Vector.Zero;
            }
            else
            {
                b.Direction = (b.NextJoint - b.PrevJoint) / b.Length;
            }

            b.Rotation = Quaternion.Euler(deg).ToLeapQuaternion().Multiply(b.Rotation);
        }

        public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
        {
            return Quaternion.Euler(angles) * (point - pivot) + pivot;
        }

        #endregion
    }

}