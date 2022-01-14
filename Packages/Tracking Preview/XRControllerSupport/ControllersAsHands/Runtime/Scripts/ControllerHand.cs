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

    [System.Serializable]
    public class ControllerHand
    {

        [System.Serializable]
        public class ControllerLimb
        {
            [HideInInspector]
            public string name = "";
            public bool analog = true, interpolate = true;
#if ENABLE_INPUT_SYSTEM
            private InputAction inputAction;
            private System.Type inputType = null;
#endif
            public List<string> axes = new List<string>();
            public float value = 0;
            public float interpolationTime = 0.04f;
            public bool compoundRotations = true;
            [Tooltip("Euler angles. These values are compounded by default, meaning the metacarpal value will be added to the proximal etc.")]
            public Vector3 metacarpalRotation = Vector3.zero, proximalRotation = Vector3.zero, intermediateRotation = Vector3.zero, distalRotation = Vector3.zero;

#if ENABLE_INPUT_SYSTEM
            public void CreateAction()
            {
                inputAction = new InputAction(name);
                for (int i = 0; i < axes.Count; i++)
                {
                    inputAction.AddBinding().WithPath(axes[i]);
                }
                inputAction.Enable();
            }
#endif

            public void Update()
            {
                float result = 0;
#if ENABLE_INPUT_SYSTEM
                if (inputType == null && inputAction.activeControl != null)
                {
                    inputType = inputAction.activeControl.valueType;
                }
                try
                {
                    if (inputType == typeof(Vector2))
                    {
                        result = inputAction.ReadValue<Vector2>().magnitude;
                    }
                    else if (inputType == typeof(Vector3))
                    {
                        result = inputAction.ReadValue<Vector3>().magnitude;
                    }
                    else
                    {
                        result = inputAction.ReadValue<float>();
                    }
                }
                catch
                {
                    Debug.LogError("Unable to read input of " + inputAction.name);
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
                        try
                        {

                    if (Input.GetButton(this.axes[i]))
                    {
                        result = 1;
                        break;
                    }
                        }
                        catch (System.ArgumentException e)
                        {
                            Debug.LogWarning(e.Message + "\n The controller post processor is reliant on the" +
                                " XR Legacy Input Helpers package when using the Legacy Input Module. Please add this package to your project.");
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
        public Vector3 offsetPosition;

        public Vector3 controllerPosition
        {
            get
            {
                if (!controller.added)
                {
                    return Vector3.zero;
                }
                return controller.devicePosition.ReadValue() + (controllerRotation * offsetPosition);
            }
        }

        public Quaternion controllerRotation
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
        public Hand hand { get { Hand h = new Hand(); h.CopyFrom(_hand); return h; } }

        private Vector3 _oldPosition = Vector3.zero;
        private Vector3 _currentPosition;
        private Quaternion _currentRotation;

        public Vector3 offsetRotationEuler;
        public ControllerLimb[] limbs = new ControllerLimb[5];

        private Chirality chirality = Chirality.Left;

        [HideInInspector]
        public float timeVisible = 0;
        private float _oldTime = 0;

        public ControllerHand(Chirality chirality)
        {
            this.chirality = chirality;
        }

        #region Properties
        public ControllerLimb thumb
        {
            get
            {
                return limbs[0];
            }
            set
            {
                limbs[0] = value;
            }
        }
        public ControllerLimb indexFinger
        {
            get
            {
                return limbs[1];
            }
            set
            {
                limbs[1] = value;
            }
        }
        public ControllerLimb middleFinger
        {
            get
            {
                return limbs[2];
            }
            set
            {
                limbs[2] = value;
            }
        }

        public ControllerLimb ringFinger
        {
            get
            {
                return limbs[3];
            }
            set
            {
                limbs[3] = value;
            }
        }

        public ControllerLimb pinkyFinger
        {
            get
            {
                return limbs[4];
            }
            set
            {
                limbs[4] = value;
            }
        }
        #endregion

        public void Setup(Chirality chirality)
        {
            this.chirality = chirality;
            _oldTime = Time.time;
#if ENABLE_INPUT_SYSTEM
            switch (this.chirality)
            {
                case Chirality.Left:
                    controller = XRController.leftHand;
                    break;
                case Chirality.Right:
                    controller = XRController.rightHand;
                    break;
            }
            for (int i = 0; i < limbs.Length; i++)
            {
                limbs[i].CreateAction();
            }
            if (controller != null) { _oldPosition = controllerPosition; }
#else
            _oldPosition = transform.position;
#endif
        }

        public void Update()
        {
            this.timeVisible += Time.deltaTime;
            for (int i = 0; i < this.limbs.Length; i++)
            {
                this.limbs[i].Update();
            }
            UpdateControllerVariables();
            this._hand = GenerateControllerHand();
        }

        private void UpdateControllerVariables()
        {
#if ENABLE_INPUT_SYSTEM
            _currentPosition = this.controllerPosition;
            _currentRotation = this.controllerRotation;
#else
        _currentPosition = this.transform.position;
        _currentRotation = this.transform.rotation * Quaternion.Euler(offsetRotationEuler);
#endif
        }

        #region Generate Limbs

        public void GenerateLimbs()
        {
            for (int i = 0; i < this.limbs.Length; i++)
            {
                this.limbs[i] = new ControllerLimb();
            }
#if ENABLE_INPUT_SYSTEM
            GenerateInputAxes(this.chirality);
#else
        GenerateLegacyAxes(this.chirality);
#endif
            GenerateHandPoses(this.chirality);
        }

        private void GenerateHandPoses(Chirality chirality)
        {
            this.thumb.analog = false;
            this.thumb.interpolate = true;
            this.thumb.proximalRotation = new Vector3(-5, 0, 25);
            this.thumb.intermediateRotation = new Vector3(-10, 0, -65);
            this.thumb.distalRotation = new Vector3(0, 0, -45);
            this.thumb.name = chirality.ToString() + " Thumb";

            this.indexFinger.interpolate = true;
            this.indexFinger.proximalRotation = new Vector3(-70, -10, 0);
            this.indexFinger.intermediateRotation = new Vector3(-45, 0, -5);
            this.indexFinger.distalRotation = new Vector3(-45, 0, 0);
            this.indexFinger.name = chirality.ToString() + " Index";

            this.middleFinger.interpolate = true;
            this.middleFinger.metacarpalRotation = new Vector3(0, -2, 0);
            this.middleFinger.proximalRotation = new Vector3(-65, 1, 0);
            this.middleFinger.intermediateRotation = new Vector3(-55, 0, 2);
            this.middleFinger.distalRotation = new Vector3(-40, -6, 3);
            this.middleFinger.name = chirality.ToString() + " Middle";

            this.ringFinger.interpolate = true;
            this.ringFinger.proximalRotation = new Vector3(-65, 8, 0);
            this.ringFinger.intermediateRotation = new Vector3(-50, 12, 0);
            this.ringFinger.distalRotation = new Vector3(-40, -35, 14);
            this.ringFinger.name = chirality.ToString() + " Ring";

            this.pinkyFinger.interpolate = true;
            this.pinkyFinger.proximalRotation = new Vector3(-65, 15, 2);
            this.pinkyFinger.intermediateRotation = new Vector3(-40, 9, 5);
            this.pinkyFinger.distalRotation = new Vector3(-45, -20, 20);
            this.pinkyFinger.name = chirality.ToString() + " Pinky";

            this.offsetRotationEuler = new Vector3(-10, 260, 235);
            if (chirality == Chirality.Right)
            {
                this.offsetRotationEuler.y *= -1;
                this.offsetRotationEuler.z *= -1;
            }


            switch (chirality)
            {
                case Chirality.Right:
                    for (int i = 0; i < this.limbs.Length; i++)
                    {
                        this.limbs[i].metacarpalRotation.y *= -1;
                        this.limbs[i].proximalRotation.y *= -1;
                        this.limbs[i].intermediateRotation.y *= -1;
                        this.limbs[i].distalRotation.y *= -1;
                        this.limbs[i].metacarpalRotation.z *= -1;
                        this.limbs[i].proximalRotation.z *= -1;
                        this.limbs[i].intermediateRotation.z *= -1;
                        this.limbs[i].distalRotation.z *= -1;
                    }
                    break;
            }
        }
#if ENABLE_INPUT_SYSTEM

        private void GenerateInputAxes(Chirality chirality)
        {
            string hand = chirality.ToString();

            this.thumb.axes = new List<string>() { "<XRController>{" + hand + "Hand}/touchpadTouched",
                "<XRController>{" + hand + "Hand}/touchpadClicked",
                "<XRController>{" + hand + "Hand}/primaryTouched",
                "<XRController>{" + hand + "Hand}/secondaryTouched",
                "<XRController>{" + hand + "Hand}/thumbstickTouched"};

            this.indexFinger.axes = new List<string>() { "<XRController>{" + hand + "Hand}/trigger" };

            this.middleFinger.axes = new List<string>() { "<XRController>{" + hand + "Hand}/grip" };
            this.ringFinger.axes = new List<string>() { "<XRController>{" + hand + "Hand}/grip" };
            this.pinkyFinger.axes = new List<string>() { "<XRController>{" + hand + "Hand}/grip" };
        }
#else

        private void GenerateLegacyAxes(Chirality chirality)
        {
            string hand = chirality.ToString();

            this.thumb.axes.Add("XRI_" + hand + "_Primary2DAxisTouch");
            this.thumb.axes.Add("XRI_" + hand + "_Primary2DAxisClick");
            this.thumb.axes.Add("XRI_" + hand + "_PrimaryButton");

            this.indexFinger.axes.Add("XRI_" + hand + "_Trigger");

            this.middleFinger.axes.Add("XRI_" + hand + "_Grip");

            this.ringFinger.axes.Add("XRI_" + hand + "_Grip");
            
            this.pinkyFinger.axes.Add("XRI_" + hand + "_Grip");
        }
#endif
        #endregion

        #region Generate Hands

        private Hand GenerateControllerHand()
        {
            // this could do with being changed so it doesn't generate each time.
            // annoying to do because finger positions are in world space, meaning transforms are hard to carry between frames.
            Hand h = TestHandFactory.MakeTestHand(chirality == Chirality.Left, unitType: TestHandFactory.UnitType.UnityUnits);
            SetFingerSizes(ref h, 0.013f);

            // Thumb
            RotateFinger(h.Fingers[0], this.thumb);

            // index Finger
            RotateFinger(h.Fingers[1], this.indexFinger);

            // Grip Fingers
            RotateFinger(h.Fingers[2], this.middleFinger);
            RotateFinger(h.Fingers[3], this.ringFinger);
            RotateFinger(h.Fingers[4], this.pinkyFinger);

            h.Transform(_currentPosition, _currentRotation);
            h.PalmVelocity = ((_currentPosition - _oldPosition) / Mathf.Clamp(Time.time - _oldTime, 0.000001f, Mathf.Infinity)).ToVector();

            _oldPosition = _currentPosition;

            h.TimeVisible = this.timeVisible;
            h.Id = chirality == Chirality.Left ? 0 : 1;
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

        private void RotateFinger(Finger f, ControllerLimb limb)
        {
            RotateJoint(ref f.bones[0], ref f.bones[1], Vector3.Slerp(Vector3.zero, limb.metacarpalRotation, limb.value));
            if (limb.compoundRotations)
            {
                RotateJoint(ref f.bones[1], ref f.bones[2], Vector3.Slerp(Vector3.zero, limb.metacarpalRotation + limb.proximalRotation, limb.value));
                RotateJoint(ref f.bones[2], ref f.bones[3], Vector3.Slerp(Vector3.zero, limb.metacarpalRotation + limb.proximalRotation + limb.intermediateRotation, limb.value));
                RotateJoint(ref f.bones[3], ref f.bones[3], Vector3.Slerp(Vector3.zero, limb.metacarpalRotation + limb.proximalRotation + limb.intermediateRotation + limb.distalRotation, limb.value));
            }
            else
            {
                RotateJoint(ref f.bones[1], ref f.bones[2], Vector3.Slerp(Vector3.zero, limb.proximalRotation, limb.value));
                RotateJoint(ref f.bones[2], ref f.bones[3], Vector3.Slerp(Vector3.zero, limb.intermediateRotation, limb.value));
                RotateJoint(ref f.bones[3], ref f.bones[3], Vector3.Slerp(Vector3.zero, limb.distalRotation, limb.value));
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