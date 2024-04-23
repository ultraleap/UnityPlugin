using Leap.Unity.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.PhysicalHands
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicalHandsSlider : MonoBehaviour
    {
        [SerializeField, OnEditorChange("AutoAssignConnectedButton")]
        GameObject _slideableObject;

        #region Direction Enums

        public enum SliderDirection
        {
            X,
            Z
        }
        [SerializeField]
        internal SliderDirection _sliderDirection = SliderDirection.X;

        #endregion

        public UnityEvent<float> SliderChangeEvent = new UnityEvent<float>();
        public UnityEvent<float> SliderButtonPressedEvent = new UnityEvent<float>();
        public UnityEvent<float> SliderButtonUnPressedEvent = new UnityEvent<float>();

        /// <summary>
        /// The travel distance of the slider (from the central point).
        /// i.e. slider center point +/- slider travel distance (or half the full travel of the slider).
        /// </summary>
        [SerializeField]
        public float SliderTravelDistance = 0.22f;

        /// <summary>
        /// Number of segments for the one-dimensional slider to use.
        /// 0 = unlimited segments.
        /// </summary>
        [SerializeField]
        public int _numberOfSegments = 0;

        [SerializeField]
        public float _startPosition = 0;

        [SerializeField]
        private Vector3 _axisChangeFromZero = Vector3.zero;

        [SerializeField]
        private PhysicalHandsButton _connectedButton;

        private bool _freezeIfNotActive = true;

        [SerializeField]
        private Vector3 _sliderValue = Vector3.zero;

        private Rigidbody _slideableObjectRigidbody;
        private ConfigurableJoint _configurableJoint;

        private float _sliderXZeroPos = 0;
        private float _sliderZZeroPos = 0;

        private Vector3 _prevSliderValue = Vector3.zero;

        private Quaternion _initialRotation;

        private float localSliderTravelDistanceHalf;

        /// <summary>
        /// Use this to get the slider value on all axes.
        /// </summary>
        /// <returns>Vector3 ofslider values.</returns>
        public float GetSliderValue()
        {
            switch (_sliderDirection)
            {
                case SliderDirection.X:
                    return _sliderValue.x;
                case SliderDirection.Z:
                    return _sliderValue.z;
            }
            return 0;
        }

        private void AutoAssignConnectedButton()
        {
            if(_slideableObject != null)
            {
                _slideableObject.TryGetComponent<PhysicalHandsButton>(out PhysicalHandsButton physicalHandsButton);
                if(_connectedButton == null)
                {
                    _connectedButton = physicalHandsButton;
                }
            }
        }

        #region Unity Methods

        /// <summary>
        /// Initializes the slider when the GameObject is enabled.
        /// </summary>
        private void Start()
        {
            Initialize();
        }

        private void OnDisable()
        {
            if(_connectedButton != null)
            {
                _connectedButton.OnButtonPressed?.RemoveListener(ButtonPressed);
                _connectedButton.OnButtonUnPressed?.RemoveListener(ButtonUnPressed);
            }
        }

        private void OnEnable()
        {
            if (_connectedButton != null)
            {
                _connectedButton.OnButtonPressed?.RemoveListener(ButtonPressed);
                _connectedButton.OnButtonUnPressed?.RemoveListener(ButtonUnPressed);
                _connectedButton.OnButtonPressed?.AddListener(ButtonPressed);
                _connectedButton.OnButtonUnPressed?.AddListener(ButtonUnPressed);
            }
        }

        /// <summary>
        /// Update method called once per frame.
        /// </summary>
        private void Update()
        {
            // Calculate the change in position of the slider object from its zero position
            _axisChangeFromZero.x = _slideableObject.transform.localPosition.x - _sliderXZeroPos;
            _axisChangeFromZero.z = _slideableObject.transform.localPosition.z - _sliderZZeroPos;
            // Calculate the slider value based on the change in position
            _sliderValue = CalculateSliderValue(_axisChangeFromZero);

            // Check if the slider value has changed
            if (_prevSliderValue != _sliderValue)
            {
                // Send slider change event
                SendSliderEvent(SliderChangeEvent);
            }

            _prevSliderValue = _sliderValue;
        }

        private void FixedUpdate()
        {
            // Account for joints being a little weird when rotating parent rigidbodies.
            // Set the rotation to the initial rotation of the sliding object when it gets too far away. 
            // Note. This will stop the slideable object from being rotated at all at runtime
            if(Quaternion.Angle(_slideableObject.transform.localRotation, _initialRotation) > 0.5f)
            {
                _slideableObject.transform.localRotation = _initialRotation;
            }
        }

        #endregion

        #region Set Up

        private void Initialize()
        {
            MakeLocalSliderScales();

            // Check if the slideable object is assigned
            if (_slideableObject == null)
            {
                Debug.LogWarning("There is no slideable object. Please add one to use the slider. \n This script has been disabled", this.gameObject);
                this.enabled = false;
                return;
            }

            ConfigureSlideableObject();

            // Set up the slider based on its type
            SetUpSlider();


            UpdateSliderZeroPos();


            ConfigureConnectedButton();

            // Freeze or unfreeze slider position based on the flag
            UpdateSliderPosition();
        }

        /// <summary>
        /// Calculates local slider scales based on the slider type and direction.
        /// </summary>
        private void MakeLocalSliderScales()
        {
            switch (_sliderDirection)
            {
                case SliderDirection.X:
                    localSliderTravelDistanceHalf = SliderTravelDistance / transform.lossyScale.x / 2;
                    break;
                case SliderDirection.Z:
                    localSliderTravelDistanceHalf = SliderTravelDistance / transform.lossyScale.z / 2;
                    break;
            }
        }

        /// <summary>
        /// Configures the slideable object with necessary components and event handlers.
        /// </summary>
        private void ConfigureSlideableObject()
        {
            PhysicalHandsSlideHelper slideHelper;
            if (!_slideableObject.TryGetComponent<PhysicalHandsSlideHelper>(out slideHelper))
            {
                slideHelper = _slideableObject.AddComponent<PhysicalHandsSlideHelper>();
            }

            slideHelper._onHandGrab += OnHandGrab;
            slideHelper._onHandGrabExit += OnHandGrabExit;
            slideHelper._onHandContact += OnHandContact;
            slideHelper._onHandContactExit += OnHandContactExit;

            if (!_slideableObject.TryGetComponent<Rigidbody>(out _slideableObjectRigidbody))
            {
                _slideableObjectRigidbody = _slideableObject.AddComponent<Rigidbody>();
            }

            _slideableObjectRigidbody.useGravity = false;
            this.GetComponent<Rigidbody>().isKinematic = true;
            _initialRotation = _slideableObject.transform.localRotation;
        }

        /// <summary>
        /// Configures the connected button with event listeners.
        /// </summary>
        private void ConfigureConnectedButton()
        {
            if (_connectedButton == null)
            {
                if (_slideableObject.TryGetComponent<PhysicalHandsButton>(out _connectedButton))
                {
                    _connectedButton.OnButtonPressed?.RemoveListener(ButtonPressed);
                    _connectedButton.OnButtonUnPressed?.RemoveListener(ButtonUnPressed);
                    _connectedButton.OnButtonPressed?.AddListener(ButtonPressed);
                    _connectedButton.OnButtonUnPressed?.AddListener(ButtonUnPressed);
                }
            }
            else
            {
                _connectedButton.OnButtonPressed?.RemoveListener(ButtonPressed);
                _connectedButton.OnButtonUnPressed?.RemoveListener(ButtonUnPressed);
                _connectedButton.OnButtonPressed?.AddListener(ButtonPressed);
                _connectedButton.OnButtonUnPressed?.AddListener(ButtonUnPressed);
            }
        }

        /// <summary>
        /// Updates the slider position based on freezing conditions and starting positions.
        /// </summary>
        private void UpdateSliderPosition()
        {
            if (_freezeIfNotActive == false)
            {
                UnFreezeSliderPosition();
            }
            else
            {
                FreezeSliderPosition();
            }

            UpdateSliderPos(_startPosition);
        }

        /// <summary>
        /// Updates the zero position for a one-dimensional slider based on its direction.
        /// </summary>
        private void UpdateSliderZeroPos()
        {
            // Initialize offset variables
            float xOffset = 0;
            float zOffset = 0;

            // Determine the slider direction and calculate offsets accordingly
            switch (_sliderDirection)
            {
                case SliderDirection.X:
                    xOffset = _configurableJoint.anchor.x -localSliderTravelDistanceHalf;
                    break;
                case SliderDirection.Z:
                    zOffset = _configurableJoint.anchor.z -localSliderTravelDistanceHalf;
                    break;
            }

            // Update zero positions based on calculated offsets and current object position
            _sliderXZeroPos = xOffset + _slideableObject.transform.localPosition.x;
            _sliderZZeroPos = zOffset + _slideableObject.transform.localPosition.z;
        }

        #region Set Up Joints
        /// <summary>
        /// Sets up the configurable joint for the slider.
        /// </summary>
        /// <param name="joint">The configurable joint to set up.</
        private void SetUpConfigurableJoint(ConfigurableJoint joint)
        {
            if(joint == null)
            {
                return;
            }

            joint.connectedBody = this.GetComponent<Rigidbody>();
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;

            JointDrive jointDrive = new JointDrive();
            jointDrive.positionDamper = 10;
            jointDrive.maximumForce = 2;
            joint.xDrive = jointDrive;
            joint.yDrive = jointDrive;
            joint.zDrive = jointDrive;

            joint.projectionMode = JointProjectionMode.PositionAndRotation;

            joint.anchor = Vector3.zero;
        }

        /// <summary>
        /// Sets up a one-dimensional slider.
        /// </summary>
        private void SetUpSlider()
        {
            if(_slideableObject == null)
            {
                Debug.LogWarning("There is no slideable object. Please add one to use the slider.", this.gameObject);
                return;
            }

            foreach (var item in _slideableObject.GetComponents<ConfigurableJoint>())
            {
                Destroy(item);
            }

            _configurableJoint = _slideableObject.AddComponent<ConfigurableJoint>();


            SetUpConfigurableJoint(_configurableJoint);

            SoftJointLimit linearJointLimit = new SoftJointLimit();
            linearJointLimit.limit = SliderTravelDistance / 2;
            _configurableJoint.linearLimit = linearJointLimit;

            switch (_sliderDirection)
            {
                case SliderDirection.X:
                _configurableJoint.xMotion = ConfigurableJointMotion.Limited;
                    break;
                case SliderDirection.Z:
                _configurableJoint.zMotion = ConfigurableJointMotion.Limited;
                    break;
            }
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            MakeLocalSliderScales();
            DrawJointRangeGizmo();
        }

        /// <summary>
        /// Draws gizmos representing the range of motion for the joint based on its type and direction.
        /// </summary>
        private void DrawJointRangeGizmo()
        {
            Vector3 jointPosition = _slideableObject.transform.localPosition;
            Matrix4x4 m = _slideableObject.transform.localToWorldMatrix;
            m.SetTRS(this.transform.position, m.rotation, m.lossyScale);
            Gizmos.matrix = m;

            float SliderTravelDistanceHalf = SliderTravelDistance / 2;

            switch (_sliderDirection)
            {
                case SliderDirection.X:
                    Gizmos.color = Color.red; // X axis
                    Gizmos.DrawLine(jointPosition + (Vector3.right * (SliderTravelDistanceHalf / _slideableObject.transform.lossyScale.x)), jointPosition - (Vector3.right * (SliderTravelDistanceHalf / _slideableObject.transform.lossyScale.x)));
                    break;
                case SliderDirection.Z:
                    Gizmos.color = Color.blue; // Z axis
                    Gizmos.DrawLine(jointPosition + (Vector3.forward * (SliderTravelDistanceHalf / _slideableObject.transform.lossyScale.z)), jointPosition - (Vector3.forward * (SliderTravelDistanceHalf / _slideableObject.transform.lossyScale.z)));
                    break;
            }
        }

        #endregion

        #region Events
        private void ButtonPressed()
        {
            UnFreezeSliderPosition();

            SendSliderEvent(SliderButtonPressedEvent);
        }

        private void ButtonUnPressed()
        {
            if (_freezeIfNotActive)
            {
                FreezeSliderPosition();
            }

            SnapSliderOnRelease();

            SendSliderEvent(SliderButtonUnPressedEvent);
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

            SnapSliderOnRelease();
        }

        public void OnHandContact(ContactHand hand)
        {
            UnFreezeSliderPosition();
        }

        public void OnHandContactExit(ContactHand hand)
        {
            if (_freezeIfNotActive)
            {
                FreezeSliderPosition();
            }

            SnapSliderOnRelease();
        }

        void SnapSliderOnRelease()
        {
            // Snap to segment if applicable

            if (_numberOfSegments != 0)
            {
                SnapToSegment();
            }

            // Calculate the change in position of the slider object from its zero position
            _axisChangeFromZero.x = _slideableObject.transform.localPosition.x - _sliderXZeroPos;
            _axisChangeFromZero.z = _slideableObject.transform.localPosition.z - _sliderZZeroPos;
            // Calculate the slider value based on the change in position
            _sliderValue = CalculateSliderValue(_axisChangeFromZero);

            // Check if the slider value has changed
            if (_prevSliderValue != _sliderValue)
            {
                // Send slider change event
                SendSliderEvent(SliderChangeEvent);
            }

            _prevSliderValue = _sliderValue;
        }

        /// <summary>
        /// Unfreezes the slider position, allowing it to move freely.
        /// </summary>
        private void UnFreezeSliderPosition()
        {
            _slideableObjectRigidbody.constraints = RigidbodyConstraints.None;
            _slideableObjectRigidbody.freezeRotation = true;
            _slideableObjectRigidbody.isKinematic = false;
        }
        /// <summary>
        /// Freezes the slider position, preventing it from moving.
        /// </summary>
        private void FreezeSliderPosition()
        {
            _slideableObjectRigidbody.constraints = RigidbodyConstraints.FreezeAll;
            _slideableObjectRigidbody.isKinematic = true;
        }

        /// <summary>
        /// Standardised event to send any sort of value events. 
        /// If you know which event will be used then only send the relevant event with the other being null.
        /// Invoke by passing the correct type of event.
        /// </summary>
        /// <param name="unityEvent"></param>
        void SendSliderEvent(UnityEvent<float> curEvent)
        {
            switch (_sliderDirection)
            {
                case SliderDirection.X:
                    curEvent?.Invoke(_sliderValue.x);
                    break;
                case SliderDirection.Z:
                    curEvent?.Invoke(_sliderValue.z);
                    break;
            }
        }

        #endregion

        #region Update

        /// <summary>
        /// Updates the position of a slider object based on the provided value or two-dimensional value.
        /// </summary>
        /// <param name="value">The value representing the position along the slider.</param>
        private void UpdateSliderPos(float value)
        {
            // Get the current position of the slider object
            Vector3 slidePos = _slideableObject.transform.localPosition;

            // Update position for one-dimensional slider based on slider direction
            switch (_sliderDirection)
            {
                case SliderDirection.X:
                    slidePos.x = Utils.Map(value, 0, 1, 0, localSliderTravelDistanceHalf * 2) + _sliderXZeroPos;
                    break;
                case SliderDirection.Z:
                    slidePos.z = Utils.Map(value, 0, 1, 0, localSliderTravelDistanceHalf * 2) + _sliderZZeroPos;
                    break;
            }

            // Reset velocity to zero and update the position of the slider object
            _slideableObjectRigidbody.velocity = Vector3.zero;
            _slideableObjectRigidbody.transform.localPosition = slidePos;
        }

        /// <summary>
        /// Calculates the value of slider movement based on the change from zero position.
        /// </summary>
        /// <param name="changeFromZero">Change in position from zero position.</param>
        /// <returns>The calculated slider value.</returns>
        private Vector3 CalculateSliderValue(in Vector3 changeFromZero)
        {
            switch (_sliderDirection)
            {
                case SliderDirection.X:
                    return new Vector3(
                        Utils.Map(changeFromZero.x, 0, localSliderTravelDistanceHalf * 2, 0, 1),
                        0,
                        0);
                case SliderDirection.Z:
                    return new Vector3(
                        0,
                        0,
                        Utils.Map(changeFromZero.z, 0, localSliderTravelDistanceHalf * 2, 0, 1));
            }

            return Vector3.negativeInfinity;
        }

        /// <summary>
        /// Snaps the slider to the nearest segment based on its type and direction.
        /// </summary>
        private void SnapToSegment()
        {
            // Initialize variables
            float closestStep = 0;

            // For one-dimensional sliders
            switch (_sliderDirection)
            {
                case SliderDirection.X:
                    closestStep = GetClosestStep(_sliderValue.x, _numberOfSegments);
                    break;
                case SliderDirection.Z:
                    closestStep = GetClosestStep(_sliderValue.z, _numberOfSegments);
                    break;
            }

            // Update the slider position
            UpdateSliderPos(closestStep);
        }

        private float GetClosestStep(float inputNumber, int numberOfSteps)
        {
            if (numberOfSteps == 0)
            {
                return (float)inputNumber;
            }

            // Calculate the step size
            float stepSize = 1.0f / numberOfSteps;

            // Calculate the closest step index
            int closestStepIndex = (int)Math.Round(inputNumber / stepSize);

            // Calculate the closest step value
            float closestStepValue = closestStepIndex * stepSize;

            return closestStepValue;
        }

        #endregion
    }
}