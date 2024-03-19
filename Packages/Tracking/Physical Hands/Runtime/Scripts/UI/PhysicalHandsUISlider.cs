using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.PhysicalHands
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicalHandsUISlider : MonoBehaviour, IPhysicalHandGrab
    {
        
        public enum SliderType
        {
            ONE_DIMENSIONAL,
            TWO_DIMENSIONAL,
        }
        [SerializeField]
        internal SliderType _sliderType = SliderType.ONE_DIMENSIONAL;

        public enum SliderDirection
        {
            X,
            Y,
            Z
        }
        [SerializeField]
        internal SliderDirection _sliderDirection = SliderDirection.X;

        public enum TwoDimSliderDirection
        {
            XY,
            XZ,
            YZ,
        }
        [SerializeField]
        internal TwoDimSliderDirection _twoDimSliderDirection = TwoDimSliderDirection.XY;

        [SerializeField]
        private PhysicalHandsButton _connectedButton;

        private Rigidbody _connectedRigidbody;

        [SerializeField]
        private bool _freezeIfNotActive = false;

        bool _sliderReleasedLastFrame = false;


        /// <summary>
        /// The travel distance of the slider (from the central point).
        /// i.e. slider center point +/- slider travel distance (or half the full travel of the slider).
        /// </summary>
        [SerializeField]
        private float _sliderTravelDistance = 0.22f;
        public float SliderTravelDistance 
        {
            get
            {
                return _sliderTravelDistance;
            } 
            set
            {
                _sliderTravelDistance = value;
                UpdateSliderZeroPos();

            }
        }

        /// <summary>
        /// Number of segments for the slider to use.
        /// 0 = unlimited segments
        /// </summary>
        [SerializeField, Range(0,100)]
        public int _numberOfSegments = 0;

        /// <summary>
        /// Number of segments for the slider to use.
        /// 0 = unlimited segments
        /// </summary>
        [SerializeField]
        public Vector2 _twoDimNumberOfSegments = Vector2.zero;


        [SerializeField]
        private float _sliderXZeroPos = 0;
        private float _sliderYZeroPos = 0;
        private float _sliderZZeroPos = 0;

        private Vector3 prevSliderPercentage = Vector3.zero;

        private List<ConfigurableJoint> _configurableJoints = new List<ConfigurableJoint>();

        [SerializeField]
        private Vector3 _axisChangeFromZero = Vector3.zero;

        [SerializeField]
        private Vector3 _sliderPercentage = Vector3.zero;

        [SerializeField, Range(0,100)]
        public int _startPercentage = 0;
        [SerializeField]
        public Vector2 _twoDimStartPercentage = Vector2.zero;

        public UnityEvent<int> SliderChangeEvent = new UnityEvent<int>();
        public UnityEvent<int, int> TwoDimensionalSliderChangeEvent = new UnityEvent<int, int>();
        public UnityEvent<int> SliderButtonPressedEvent = new UnityEvent<int>();
        public UnityEvent<int, int> TwoDimensionalSliderButtonPressedEvent = new UnityEvent<int, int>();
        public UnityEvent<int> SliderButtonUnPressedEvent = new UnityEvent<int>();
        public UnityEvent<int, int> TwoDimensionalSliderButtonUnPressedEvent = new UnityEvent<int, int>();


        /// <summary>
        /// Use this to get the slider percentage on all axis.
        /// </summary>
        /// <returns>Vector3 ofslider percentages.</returns>
        public Vector3 GetSliderPercentage()
        {
            return _sliderPercentage;
        }


        private void Awake()
        {

        }

        private void OnEnable()
        {
            TryGetComponent<Rigidbody>(out _connectedRigidbody);
            
            switch (_sliderType)
            {
                case SliderType.ONE_DIMENSIONAL:
                    SetUpSlider();
                    break;
                case SliderType.TWO_DIMENSIONAL:
                    SetUpTwoDimSlider();
                    break;
            }

            UpdateSliderZeroPos();

            if (_connectedButton == null)
            {
                if (TryGetComponent<PhysicalHandsButton>(out _connectedButton))
                {
                    _connectedButton.OnButtonPressed.AddListener(ButtonPressed);
                    _connectedButton.OnButtonUnPressed.AddListener(ButtonUnPressed);
                }
            }
            if(_freezeIfNotActive == false)
            {
                UnFreezeSliderPosition();
            }
            else
            {
                FreezeSliderPosition();
            }

            UpdateSliderPos(_startPercentage, _twoDimStartPercentage);
        }



        private void OnDisable()
        {
            if(_connectedButton != null)
            {
                _connectedButton.OnButtonPressed.RemoveListener(ButtonPressed);
                _connectedButton.OnButtonUnPressed.RemoveListener(ButtonUnPressed);
            }
            
        }

        private void Update()
        {
            _axisChangeFromZero.x = this.transform.localPosition.x - _sliderXZeroPos;
            _axisChangeFromZero.y = this.transform.localPosition.y - _sliderYZeroPos;
            _axisChangeFromZero.z = this.transform.localPosition.z - _sliderZZeroPos;

            _sliderPercentage = calculateSliderPercentage(_axisChangeFromZero);

            if (prevSliderPercentage != _sliderPercentage)
            {
                SendSliderEvent(SliderChangeEvent, TwoDimensionalSliderChangeEvent);
                if (_sliderReleasedLastFrame)
                {
                    switch (_sliderType)
                    {
                        case SliderType.ONE_DIMENSIONAL:
                        {
                            if (_numberOfSegments != 0)
                            {
                                SnapToSegment();
                            }
                            break;
                        }
                        case SliderType.TWO_DIMENSIONAL:
                        {
                            if (_twoDimNumberOfSegments.x != 0 && _twoDimNumberOfSegments.y != 0)
                            {
                                SnapToSegment();
                            }
                            else if (_twoDimNumberOfSegments.x != 0 && _twoDimNumberOfSegments.y == 0)
                            {
                                _twoDimNumberOfSegments.y = 100;
                                SnapToSegment();
                            }
                            else if (_twoDimNumberOfSegments.x == 0 && _twoDimNumberOfSegments.y != 0)
                            {
                                _twoDimNumberOfSegments.x = 100;
                                SnapToSegment();
                            }
                            break;
                        }
                    }
                    _sliderReleasedLastFrame = false;
                    prevSliderPercentage = _sliderPercentage;
                }

            }
        }

        void SnapToSegment()
        {
            int percentage = 0;
            Vector2 twoDimPercentage = Vector2.zero;

            switch (_sliderType)
            {
                case SliderType.ONE_DIMENSIONAL:
                    switch (_sliderDirection)
                    {
                        case SliderDirection.X:
                            percentage = GetClosestStep(_sliderPercentage.x, _numberOfSegments);
                            break;
                        case SliderDirection.Y:
                            percentage = GetClosestStep(_sliderPercentage.y, _numberOfSegments);
                            break;
                        case SliderDirection.Z:
                            percentage = GetClosestStep(_sliderPercentage.z, _numberOfSegments);
                            break;
                    }
                    break;
                case SliderType.TWO_DIMENSIONAL:
                    switch (_twoDimSliderDirection)
                    {
                        case TwoDimSliderDirection.XY:
                            twoDimPercentage.x = GetClosestStep(_sliderPercentage.x, _numberOfSegments);
                            twoDimPercentage.y = GetClosestStep(_sliderPercentage.y, _numberOfSegments);
                            break;
                        case TwoDimSliderDirection.XZ:
                            twoDimPercentage.x = GetClosestStep(_sliderPercentage.x, _numberOfSegments);
                            twoDimPercentage.y = GetClosestStep(_sliderPercentage.z, _numberOfSegments);
                            break;
                        case TwoDimSliderDirection.YZ:
                            twoDimPercentage.x = GetClosestStep(_sliderPercentage.y, _numberOfSegments);
                            twoDimPercentage.y = GetClosestStep(_sliderPercentage.z, _numberOfSegments);

                            break;
                    }
                    break;
            }

            UpdateSliderPos(percentage, twoDimPercentage);
        }

        private int GetClosestStep(double inputNumber, int numberOfSteps)
        {
            // Calculate the step size
            double stepSize = 100.0 / numberOfSteps;

            // Calculate the closest step index
            int closestStepIndex = (int)Math.Round(inputNumber / stepSize);

            // Ensure the index stays within bounds
            closestStepIndex = Math.Max(0, Math.Min(closestStepIndex, numberOfSteps - 1));

            // Calculate the closest step value
            int closestStepValue = (int)(closestStepIndex * stepSize);

            return closestStepValue;
        }


        /// <summary>
        /// Standardised event to send any sort of percentage events. 
        /// If you know which event will be used then only send the relevant event with the other being null.
        /// Invoke by passing the correct type of event.
        /// </summary>
        /// <param name="unityEvent"></param>
        /// <param name="twoDimUnityEvent"></param>
        void SendSliderEvent(UnityEvent<int> unityEvent, UnityEvent<int, int> twoDimUnityEvent)
        {
            switch (_sliderType)
            {
                case SliderType.ONE_DIMENSIONAL:
                    switch (_sliderDirection)
                    {
                        case SliderDirection.X:
                            unityEvent.Invoke((int)_sliderPercentage.x);
                            break;
                        case SliderDirection.Y:
                            unityEvent.Invoke((int)_sliderPercentage.y);
                            break;
                        case SliderDirection.Z:
                            unityEvent.Invoke((int)_sliderPercentage.z);
                            break;
                    }
                    break;
                case SliderType.TWO_DIMENSIONAL:
                    switch (_twoDimSliderDirection)
                    {
                        case TwoDimSliderDirection.XY:
                            twoDimUnityEvent.Invoke((int)_sliderPercentage.x, (int)_sliderPercentage.y);
                            break;
                        case TwoDimSliderDirection.XZ:
                            twoDimUnityEvent.Invoke((int)_sliderPercentage.x, (int)_sliderPercentage.z);
                            break;
                        case TwoDimSliderDirection.YZ:
                            twoDimUnityEvent.Invoke((int)_sliderPercentage.y, (int)_sliderPercentage.z);
                            break;
                    }
                    break;
            }
        }

        private void ButtonPressed()
        {
            UnFreezeSliderPosition();

            SendSliderEvent(SliderButtonPressedEvent, TwoDimensionalSliderButtonPressedEvent);
        }
        private void ButtonUnPressed()
        {
            if (_freezeIfNotActive)
            {
                FreezeSliderPosition();
            }

            _sliderReleasedLastFrame = true;

            SendSliderEvent(SliderButtonUnPressedEvent, TwoDimensionalSliderButtonUnPressedEvent);
        }

        public void OnHandGrab(ContactHand hand)
        {
            UnFreezeSliderPosition();
        }

        public void OnHandGrabExit(ContactHand hand)
        {
            if (_freezeIfNotActive)
            {
                FreezeSliderPosition();
            }

            _sliderReleasedLastFrame = true;
        }

        /// <summary>
        /// Unfreezes the slider position, allowing it to move freely.
        /// </summary>
        private void UnFreezeSliderPosition()
        {
            _connectedRigidbody.constraints = RigidbodyConstraints.None;
            _connectedRigidbody.isKinematic = false;
        }
        /// <summary>
        /// Freezes the slider position, preventing it from moving.
        /// </summary>
        private void FreezeSliderPosition()
        {
            _connectedRigidbody.constraints = RigidbodyConstraints.FreezeAll;
            _connectedRigidbody.isKinematic = true;
        }

        private void UpdateSliderPos(int percentage, Vector2 twoDimPercentage)
        {
            Vector3 slidePos = this.transform.localPosition;

            switch (_sliderType)
            {
                case SliderType.ONE_DIMENSIONAL:
                    switch (_sliderDirection)
                    {
                        case SliderDirection.X:
                            slidePos.x = Utils.Map(percentage, 0, 100, 0, SliderTravelDistance * 2) + _sliderXZeroPos;
                            break;
                        case SliderDirection.Y:
                            slidePos.y = Utils.Map(percentage, 0, 100, 0, SliderTravelDistance * 2) + _sliderYZeroPos;
                            break;
                        case SliderDirection.Z:
                            slidePos.z = Utils.Map(percentage, 0, 100, 0, SliderTravelDistance * 2) + _sliderZZeroPos;
                            break;
                    }
                    break;
                case SliderType.TWO_DIMENSIONAL:
                    switch (_twoDimSliderDirection)
                    {
                        case TwoDimSliderDirection.XY:
                            slidePos.x += Utils.Map(twoDimPercentage.x, 0, 100, 0, SliderTravelDistance * 2) + _sliderXZeroPos;
                            slidePos.y += Utils.Map(twoDimPercentage.y, 0, 100, 0, SliderTravelDistance * 2) + _sliderYZeroPos;
                            break;
                        case TwoDimSliderDirection.XZ:
                            slidePos.x += Utils.Map(twoDimPercentage.x, 0, 100, 0, SliderTravelDistance * 2) + _sliderXZeroPos;
                            slidePos.z += Utils.Map(twoDimPercentage.y, 0, 100, 0, SliderTravelDistance * 2) + _sliderZZeroPos;
                            break;
                        case TwoDimSliderDirection.YZ:
                            slidePos.y += Utils.Map(twoDimPercentage.x, 0, 100, 0, SliderTravelDistance * 2) + _sliderYZeroPos;
                            slidePos.z += Utils.Map(twoDimPercentage.y, 0, 100, 0, SliderTravelDistance * 2) + _sliderZZeroPos;
                            break;
                    }
                    break;
            }

            _connectedRigidbody.velocity = Vector3.zero;
            _connectedRigidbody.transform.localPosition = slidePos;
            //this.transform.localPosition = slidePos;
            
        }

        /// <summary>
        /// Sets up the configurable joint for the slider.
        /// </summary>
        /// <param name="joint">The configurable joint to set up.</
        private void SetUpConfigurableJoint(ConfigurableJoint joint)
        {
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;

            SoftJointLimit linerJointLimit = new SoftJointLimit();
            linerJointLimit.limit = _sliderTravelDistance +0.01f;
            joint.linearLimit = linerJointLimit;

            JointDrive jointDrive = new JointDrive();
            jointDrive.positionDamper = 10;
            jointDrive.maximumForce = 2;
            joint.xDrive = jointDrive;
            joint.yDrive = jointDrive;
            joint.zDrive = jointDrive;

            joint.anchor = Vector3.zero;
        }

        /// <summary>
        /// Sets up a one-dimensional slider.
        /// </summary>
        private void SetUpSlider()
        {
            ConfigurableJoint configurableJoint;
            TryGetComponent<ConfigurableJoint>(out configurableJoint);

            if (_configurableJoints.Count < 1 && configurableJoint == null)
            {
                _configurableJoints.Add(gameObject.AddComponent<ConfigurableJoint>());
            }
            else
            {
                _configurableJoints.Add(configurableJoint);
            }

            foreach (ConfigurableJoint joint in _configurableJoints)
            {
                SetUpConfigurableJoint(joint);

                switch (_sliderDirection)
                {
                    case SliderDirection.X:
                        joint.xMotion = ConfigurableJointMotion.Limited;
                        break;
                    case SliderDirection.Y:
                        joint.yMotion = ConfigurableJointMotion.Limited;
                        break;
                    case SliderDirection.Z:
                        joint.zMotion = ConfigurableJointMotion.Limited;
                        break;
                }
            }
        }

        /// <summary>
        /// Sets up a two-dimensional slider.
        /// </summary>
        private void SetUpTwoDimSlider()
        {
            _configurableJoints = GetComponents<ConfigurableJoint>().ToList<ConfigurableJoint>();
            for (int i = 0; i < 2; i++)
            {
                if (_configurableJoints.Count != 2)
                {
                    _configurableJoints.Add(gameObject.AddComponent<ConfigurableJoint>());
                }
            }

            foreach (ConfigurableJoint joint in _configurableJoints)
            {
                SetUpConfigurableJoint(joint);
            }

                switch (_twoDimSliderDirection)
                {
                    case TwoDimSliderDirection.XY:
                    {
                        _configurableJoints.ElementAt(0).xMotion = ConfigurableJointMotion.Limited;
                        _configurableJoints.ElementAt(1).xMotion = ConfigurableJointMotion.Free;
                        _configurableJoints.ElementAt(0).yMotion = ConfigurableJointMotion.Free;
                        _configurableJoints.ElementAt(1).yMotion = ConfigurableJointMotion.Limited;
                        break;
                    }
                    case TwoDimSliderDirection.XZ:
                    {
                        _configurableJoints.ElementAt(0).xMotion = ConfigurableJointMotion.Limited;
                        _configurableJoints.ElementAt(1).xMotion = ConfigurableJointMotion.Free;
                        _configurableJoints.ElementAt(0).zMotion = ConfigurableJointMotion.Free;
                        _configurableJoints.ElementAt(1).zMotion = ConfigurableJointMotion.Limited;
                        break;
                    }
                    case TwoDimSliderDirection.YZ:
                    {
                        _configurableJoints.ElementAt(0).yMotion = ConfigurableJointMotion.Limited;
                        _configurableJoints.ElementAt(1).yMotion = ConfigurableJointMotion.Free;
                        _configurableJoints.ElementAt(0).zMotion = ConfigurableJointMotion.Free;
                        _configurableJoints.ElementAt(1).zMotion = ConfigurableJointMotion.Limited;
                        break;
                    }
                }
        }

        /// <summary>
        /// Updates the zero position of the slider based on its type and direction.
        /// </summary>
        private void UpdateSliderZeroPos()
        {


            switch (_sliderType)
            {
                case SliderType.ONE_DIMENSIONAL:
                    switch (_sliderDirection)
                    {
                        case SliderDirection.X:
                            _sliderXZeroPos = (_configurableJoints.First().anchor.x + -_sliderTravelDistance) + this.transform.localPosition.x;
                            break;
                        case SliderDirection.Y:
                            _sliderYZeroPos = (_configurableJoints.First().anchor.y + -_sliderTravelDistance) + this.transform.localPosition.y;
                            break;
                        case SliderDirection.Z:
                            _sliderZZeroPos = (_configurableJoints.First().anchor.z + -_sliderTravelDistance) + this.transform.localPosition.z;
                            break;
                    }
                    break;
                case SliderType.TWO_DIMENSIONAL:
                    switch (_twoDimSliderDirection)
                    {
                        case TwoDimSliderDirection.XY:
                            _sliderXZeroPos = (_configurableJoints.ElementAt(0).anchor.x + -_sliderTravelDistance) + this.transform.localPosition.x;
                            _sliderYZeroPos = (_configurableJoints.ElementAt(1).anchor.y + -_sliderTravelDistance) + this.transform.localPosition.y;
                            break;
                        case TwoDimSliderDirection.XZ:
                            _sliderXZeroPos = (_configurableJoints.ElementAt(0).anchor.x + -_sliderTravelDistance) + this.transform.localPosition.x;
                            _sliderZZeroPos = (_configurableJoints.ElementAt(1).anchor.z + -_sliderTravelDistance) + this.transform.localPosition.z;
                            break;
                        case TwoDimSliderDirection.YZ:
                            _sliderYZeroPos = (_configurableJoints.ElementAt(0).anchor.y + -_sliderTravelDistance) + this.transform.localPosition.y;
                            _sliderZZeroPos = (_configurableJoints.ElementAt(1).anchor.z + -_sliderTravelDistance) + this.transform.localPosition.z;
                            break;
                    }
                    break;
            }
        }

        /// <summary>
        /// Calculates the percentage of slider movement based on the change from zero position.
        /// </summary>
        /// <param name="changeFromZero">Change in position from zero position.</param>
        /// <returns>The calculated slider percentage.</returns>
        private Vector3 calculateSliderPercentage(in Vector3 changeFromZero)
        {
            switch (_sliderType)
            {
                case SliderType.ONE_DIMENSIONAL:
                    switch (_sliderDirection)
                    {
                        case SliderDirection.X:
                            return new Vector3(Mathf.RoundToInt(Utils.Map(changeFromZero.x, 0, _sliderTravelDistance * 2, 0, 100)), 0, 0);
                        case SliderDirection.Y:
                            return new Vector3(Mathf.RoundToInt(Utils.Map(changeFromZero.y, 0, _sliderTravelDistance * 2, 0, 100)), 0, 0);
                        case SliderDirection.Z:
                            return new Vector3(Mathf.RoundToInt(Utils.Map(changeFromZero.z, 0, _sliderTravelDistance * 2, 0, 100)), 0, 0);
                    }
                    break;
                case SliderType.TWO_DIMENSIONAL:
                    switch (_twoDimSliderDirection)
                    {
                        case TwoDimSliderDirection.XY:
                            return new Vector3(
                                Mathf.RoundToInt(Utils.Map(changeFromZero.x, 0, _sliderTravelDistance * 2, 0, 100)), 
                                Mathf.RoundToInt(Utils.Map(changeFromZero.y, 0, _sliderTravelDistance * 2, 0, 100)), 
                                0);
                        case TwoDimSliderDirection.XZ:
                            return new Vector3(
                                Mathf.RoundToInt(Utils.Map(changeFromZero.x, 0, _sliderTravelDistance * 2, 0, 100)),
                                0,
                                Mathf.RoundToInt(Utils.Map(changeFromZero.z, 0, _sliderTravelDistance * 2, 0, 100)));
                        case TwoDimSliderDirection.YZ:
                            return new Vector3(
                                0,
                                Mathf.RoundToInt(Utils.Map(changeFromZero.y, 0, _sliderTravelDistance * 2, 0, 100)),
                                Mathf.RoundToInt(Utils.Map(changeFromZero.z, 0, _sliderTravelDistance * 2, 0, 100)));
                    }
                    break;
            }
            return Vector3.negativeInfinity;
        }

        private void SnapToNearestSegment()
        {

        }


    }
}
