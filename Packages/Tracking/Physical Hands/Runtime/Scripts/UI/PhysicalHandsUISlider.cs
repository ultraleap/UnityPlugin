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

        /// <summary>
        /// The travel distance of the slider (from the central point).
        /// i.e. slider center point +/- sliderTravel distance or half the full travle of the slider.
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

        [SerializeField]
        private float _sliderXZeroPos = 0;
        private float _sliderYZeroPos = 0;
        private float _sliderZZeroPos = 0;

        private List<ConfigurableJoint> _configurableJoints = new List<ConfigurableJoint>();

        [SerializeField]
        private Vector3 _axisChangeFromZero = Vector3.zero;

        [SerializeField]
        private Vector3 _sliderPercentage = Vector3.zero;

        public UnityEvent<int> SliderChangeEvent = new UnityEvent<int>();
        public UnityEvent<int, int> TwoDimensionalSliderChangeEvent = new UnityEvent<int, int>();
        public UnityEvent<int> SliderButtonPressedEvent = new UnityEvent<int>();
        public UnityEvent<int, int> TwoDimensionalSliderButtonPressedEvent = new UnityEvent<int, int>();
        public UnityEvent<int> SliderButtonUnPressedEvent = new UnityEvent<int>();
        public UnityEvent<int, int> TwoDimensionalSliderButtonUnPressedEvent = new UnityEvent<int, int>();


        private void Awake()
        {
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

        }

        private void OnEnable()
        {
            if (_connectedButton == null)
            {
                if (TryGetComponent<PhysicalHandsButton>(out _connectedButton))
                {
                    _connectedButton.OnButtonPressed.AddListener(ButtonPressed);
                    _connectedButton.OnButtonUnPressed.AddListener(ButtonUnPressed);
                }
            }
            TryGetComponent<Rigidbody>(out _connectedRigidbody);
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

            var prevSliderPercentage = _sliderPercentage;
            _sliderPercentage = calculateSliderPercentage(_axisChangeFromZero);

            if (prevSliderPercentage != _sliderPercentage)
            {
                SendSliderEvent(SliderChangeEvent, TwoDimensionalSliderChangeEvent);
                
                prevSliderPercentage = _sliderPercentage;
            }
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
        }


        private void UnFreezeSliderPosition()
        {
            _connectedRigidbody.constraints = RigidbodyConstraints.None;
            _connectedRigidbody.isKinematic = false;
        }
        private void FreezeSliderPosition()
        {
            _connectedRigidbody.constraints = RigidbodyConstraints.FreezeAll;
            _connectedRigidbody.isKinematic = true;
        }


        private void SetUpConfigurableJoint(ConfigurableJoint joint)
        {
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;

            SoftJointLimit linerJointLimit = new SoftJointLimit();
            linerJointLimit.limit = _sliderTravelDistance;
            joint.linearLimit = linerJointLimit;

            JointDrive jointDrive = new JointDrive();
            jointDrive.positionDamper = 1;
            jointDrive.maximumForce = 2;
            joint.xDrive = jointDrive;
            joint.yDrive = jointDrive;
            joint.zDrive = jointDrive;

            joint.anchor = Vector3.zero;
        }

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

        private void UpdateSliderZeroPos()
        {
            switch (_sliderType)
            {
                case SliderType.ONE_DIMENSIONAL:
                    switch (_sliderDirection)
                    {
                        case SliderDirection.X:
                            _sliderXZeroPos = _configurableJoints.First().anchor.x + -_sliderTravelDistance;
                            break;
                        case SliderDirection.Y:
                            _sliderYZeroPos = _configurableJoints.First().anchor.y + -_sliderTravelDistance;
                            break;
                        case SliderDirection.Z:
                            _sliderZZeroPos = _configurableJoints.First().anchor.z + -_sliderTravelDistance;
                            break;
                    }
                    break;
                case SliderType.TWO_DIMENSIONAL:
                    switch (_twoDimSliderDirection)
                    {
                        case TwoDimSliderDirection.XY:
                            _sliderXZeroPos = _configurableJoints.ElementAt(0).anchor.x + -_sliderTravelDistance;
                            _sliderYZeroPos = _configurableJoints.ElementAt(1).anchor.y + -_sliderTravelDistance;
                            break;
                        case TwoDimSliderDirection.XZ:
                            _sliderXZeroPos = _configurableJoints.ElementAt(0).anchor.x + -_sliderTravelDistance;
                            _sliderZZeroPos = _configurableJoints.ElementAt(1).anchor.z + -_sliderTravelDistance;
                            break;
                        case TwoDimSliderDirection.YZ:
                            _sliderYZeroPos = _configurableJoints.ElementAt(0).anchor.y + -_sliderTravelDistance;
                            _sliderZZeroPos = _configurableJoints.ElementAt(1).anchor.z + -_sliderTravelDistance;
                            break;
                    }
                    break;
            }
        }

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


    }
}
