/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Attributes;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.PhysicalHands
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicalHandsSlider : MonoBehaviour
    {
        [SerializeField, OnEditorChange("AutoAssignConnectedButton")]
        public GameObject _slideableObject;

        public enum SliderDirection
        {
            X,
            Z
        }
        [SerializeField]
        internal SliderDirection _sliderDirection = SliderDirection.X;

        public UnityEvent<float> SliderChangeEvent = new UnityEvent<float>();
        public UnityEvent<float> SliderButtonPressedEvent = new UnityEvent<float>();
        public UnityEvent<float> SliderButtonUnPressedEvent = new UnityEvent<float>();

        /// <summary>
        /// The travel distance of the slider (from the central point).
        /// i.e. slider center point +/- slider travel distance (or half the full travel of the slider).
        /// </summary>
        [SerializeField]
        public float _sliderTravelDistance = 0.22f;

        /// <summary>
        /// Number of segments for the one-dimensional slider to use.
        /// 0 = unlimited segments.
        /// </summary>
        [SerializeField]
        public int _numberOfSegments = 0;
        [SerializeField]
        public float _startPosition = 0;

        [SerializeField]
        private float _sliderValue = 0;
        public float SliderValue => _sliderValue;

        private float _prevSliderValue = 0;
        private Quaternion _initialRotation;
        private Vector3 _initialPosition;

        [SerializeField]
        public PhysicalHandsButton _connectedButton;
        private Rigidbody _slideableObjectRigidbody;
        private ConfigurableJoint _configurableJoint;

        private float _localSliderTravelDistanceHalf;
        private float _sliderXZeroPos = 0;
        private float _sliderZZeroPos = 0;
        private bool _freezeIfNotActive = true;

        #region Unity Methods

        /// <summary>
        /// Initializes the slider when the GameObject is enabled.
        /// </summary>
        private void Start()
        {
            Initialize();
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

        private void OnDisable()
        {
            if (_connectedButton != null)
            {
                _connectedButton.OnButtonPressed?.RemoveListener(ButtonPressed);
                _connectedButton.OnButtonUnPressed?.RemoveListener(ButtonUnPressed);
            }
        }

        /// <summary>
        /// Update method called once per frame.
        /// </summary>
        private void Update()
        {
            // Calculate the slider value based on the change in position
            _sliderValue = CalculateSliderValue();

            // Check if the slider value has changed
            if (_prevSliderValue != _sliderValue)
            {
                // Send slider change event
                SliderChangeEvent?.Invoke(_sliderValue);
            }

            _prevSliderValue = _sliderValue;
        }

        private void FixedUpdate()
        {
            // Account for joints being a little weird when rotating parent rigidbodies.
            // Set the rotation to the initial rotation of the sliding object when it gets too far away. 
            // Note. This will stop the slideable object from being rotated at all at runtime
            if (Quaternion.Angle(_slideableObject.transform.localRotation, _initialRotation) > 0.5f)
            {
                _slideableObject.transform.localRotation = _initialRotation;
            }


            //If you position of the sliders on the 2 unused axes is different from the 
            switch (_sliderDirection)
            {
                case SliderDirection.X:
                    if (Mathf.Abs(_slideableObject.transform.localPosition.y - _initialPosition.y) > 0.01)
                    {
                        _slideableObject.transform.localPosition = new Vector3(
                            _slideableObject.transform.localPosition.x,
                            _initialPosition.y,
                            _slideableObject.transform.localPosition.z);
                    }
                    if (Mathf.Abs(_slideableObject.transform.localPosition.z - _initialPosition.z) > 0.01)
                    {
                        _slideableObject.transform.localPosition = new Vector3(_slideableObject.transform.localPosition.x,
                            _slideableObject.transform.localPosition.y,
                            _initialPosition.z);
                    }
                    break;
                case SliderDirection.Z:
                    if (Mathf.Abs(_slideableObject.transform.localPosition.y - _initialPosition.y) > 0.01)
                    {
                        _slideableObject.transform.localPosition = new Vector3(
                            _slideableObject.transform.localPosition.x,
                            _initialPosition.y,
                            _slideableObject.transform.localPosition.z);
                    }
                    if (Mathf.Abs(_slideableObject.transform.localPosition.x - _initialPosition.x) > 0.01)
                    {
                        _slideableObject.transform.localPosition = new Vector3(
                            _initialPosition.z,
                            _slideableObject.transform.localPosition.y,
                            _slideableObject.transform.localPosition.z);
                    }
                    break;
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
            if (_slideableObject.transform.localRotation != Quaternion.identity)
            {
                Debug.LogWarning("Warning! Slideable object cannot be rotated. This will cause unexpected behaviour. \n " +
                    "Please rotate the slider instead, leaving slideable object rotation 0,0,0", _slideableObject);
            }


            ConfigureSlideableObject();

            // Set up the slider based on its type
            SetUpSlider();


            UpdateSliderZeroPos();


            ConfigureConnectedButton();

            // Freeze or unfreeze slider position based on the flag
            ApplySliderStartPosition();

        }

        /// <summary>
        /// Calculates local slider scales based on the slider type and direction.
        /// </summary>
        private void MakeLocalSliderScales()
        {
            switch (_sliderDirection)
            {
                case SliderDirection.X:
                    _localSliderTravelDistanceHalf = _sliderTravelDistance / transform.lossyScale.x / 2;
                    break;
                case SliderDirection.Z:
                    _localSliderTravelDistanceHalf = _sliderTravelDistance / transform.lossyScale.z / 2;
                    break;
            }
        }

        /// <summary>
        /// Configures the slideable object with necessary components and event handlers.
        /// </summary>
        private void ConfigureSlideableObject()
        {
            PhysicalHandsSliderHelper slideHelper;
            if (!_slideableObject.TryGetComponent<PhysicalHandsSliderHelper>(out slideHelper))
            {
                slideHelper = _slideableObject.AddComponent<PhysicalHandsSliderHelper>();
            }

            switch (_sliderDirection)
            {
                case SliderDirection.X:
                    _slideableObject.transform.localPosition = new Vector3(0, _slideableObject.transform.localPosition.y, _slideableObject.transform.localPosition.z);
                    break;
                case SliderDirection.Z:
                    _slideableObject.transform.localPosition = new Vector3(_slideableObject.transform.localPosition.x, _slideableObject.transform.localPosition.y, 0);
                    break;
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
            _initialPosition = _slideableObject.transform.localPosition;
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
        private void ApplySliderStartPosition()
        {
            if (_freezeIfNotActive == false)
            {
                UnFreezeSliderPosition();
            }
            else
            {
                FreezeSliderPosition();
            }

            // Get the current position of the slider object
            Vector3 slidePos = _slideableObject.transform.localPosition;

            // Update position for one-dimensional slider based on slider direction
            switch (_sliderDirection)
            {
                case SliderDirection.X:
                    slidePos.x = Utils.Map(_startPosition, 0, 1, 0, _localSliderTravelDistanceHalf * 2) + _sliderXZeroPos;
                    break;
                case SliderDirection.Z:
                    slidePos.z = Utils.Map(_startPosition, 0, 1, 0, _localSliderTravelDistanceHalf * 2) + _sliderZZeroPos;
                    break;
            }

            slidePos = _slideableObject.transform.localRotation * slidePos;

            // Reset velocity to zero and update the position of the slider object
#if UNITY_6000_0_OR_NEWER
            _slideableObjectRigidbody.linearVelocity = Vector3.zero;
#else
            _slideableObjectRigidbody.velocity = Vector3.zero;
#endif 
            _slideableObjectRigidbody.transform.localPosition = slidePos;

            // Calculate the slider value based on the change in position
            _sliderValue = CalculateSliderValue();

            SnapToSegment();
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
                    xOffset = _configurableJoint.anchor.x - _localSliderTravelDistanceHalf;
                    break;
                case SliderDirection.Z:
                    zOffset = _configurableJoint.anchor.z - _localSliderTravelDistanceHalf;
                    break;
            }

            // Update zero positions based on calculated offsets and current object position
            _sliderXZeroPos = xOffset + _slideableObject.transform.localPosition.x;
            _sliderZZeroPos = zOffset + _slideableObject.transform.localPosition.z;
        }

        /// <summary>
        /// Automatically sets the connected button if one is added as the slideable object
        /// </summary>
        private void AutoAssignConnectedButton()
        {
            if (_slideableObject != null)
            {
                _slideableObject.TryGetComponent<PhysicalHandsButton>(out PhysicalHandsButton physicalHandsButton);
                if (_connectedButton == null)
                {
                    _connectedButton = physicalHandsButton;
                }
            }
        }

        #region Set Up Joints
        /// <summary>
        /// Sets up the configurable joint for the slider.
        /// </summary>
        /// <param name="joint">The configurable joint to set up.</
        private void SetUpConfigurableJoint(ConfigurableJoint joint)
        {
            if (joint == null)
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
            if (_slideableObject == null)
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
            linearJointLimit.limit = _sliderTravelDistance / 2;
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
                    slidePos.x = Utils.Map(value, 0, 1, 0, _localSliderTravelDistanceHalf * 2) + _sliderXZeroPos;
                    break;
                case SliderDirection.Z:
                    slidePos.z = Utils.Map(value, 0, 1, 0, _localSliderTravelDistanceHalf * 2) + _sliderZZeroPos;
                    break;
            }

            // Reset velocity to zero and update the position of the slider object
#if UNITY_6000_0_OR_NEWER
            _slideableObjectRigidbody.linearVelocity = Vector3.zero;
#else
            _slideableObjectRigidbody.velocity = Vector3.zero;
#endif
            _slideableObjectRigidbody.transform.localPosition = slidePos;
        }

        /// <summary>
        /// Calculates the value of slider movement based on the change from zero position.
        /// </summary>
        /// <param name="changeFromZero">Change in position from zero position.</param>
        /// <returns>The calculated slider value.</returns>
        private float CalculateSliderValue()
        {
            float changeFromZero = 0;

            switch (_sliderDirection)
            {
                case SliderDirection.X:
                    changeFromZero = _slideableObject.transform.localPosition.x - _sliderXZeroPos;
                    break;
                case SliderDirection.Z:
                    changeFromZero = _slideableObject.transform.localPosition.z - _sliderZZeroPos;
                    break;
            }

            return Utils.Map(changeFromZero, 0, _localSliderTravelDistanceHalf * 2, 0, 1);
        }

        /// <summary>
        /// Snaps the slider to the nearest segment based on its type and direction.
        /// </summary>
        private void SnapToSegment()
        {
            // Initialize variables
            float closestStep = GetClosestStep(_sliderValue, (int)_numberOfSegments);

            // Update the slider position
            UpdateSliderPos(closestStep);
        }

        /// <summary>
        /// Wourk out which step is closest to the current slider value
        /// </summary>
        /// <param name="inputNumber">The currrent value of the slider.</param>
        /// <param name="numberOfSegments">How many steps should the slider be divided into.</param>
        /// <returns></returns>
        private float GetClosestStep(float inputNumber, int numberOfSegments)
        {
            if (numberOfSegments == 0)
            {
                return (float)inputNumber;
            }
            else if (numberOfSegments == 1)
            {
                return 0.5f;
            }

            numberOfSegments -= 1;

            // Calculate the step size
            float stepSize = 1.0f / numberOfSegments;

            // Calculate the closest step index
            int closestStepIndex = (int)Math.Round(inputNumber / stepSize);

            // Calculate the closest step value
            float closestStepValue = closestStepIndex * stepSize;

            return closestStepValue;
        }

        /// <summary>
        /// Snap the slider to the correct location whenb released if there are segments set
        /// </summary>
        private void SnapSliderOnRelease()
        {
            // Snap to segment if applicable
            SnapToSegment();

            // Calculate the slider value based on the change in position
            _sliderValue = CalculateSliderValue();

            // Check if the slider value has changed
            if (_prevSliderValue != _sliderValue)
            {
                // Send slider change event
                SliderChangeEvent?.Invoke(_sliderValue);
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

        #endregion

        #region Events
        private void ButtonPressed()
        {
            UnFreezeSliderPosition();

            SliderButtonPressedEvent?.Invoke(_sliderValue);
        }

        private void ButtonUnPressed()
        {
            if (_freezeIfNotActive)
            {
                FreezeSliderPosition();
            }

            SnapSliderOnRelease();

            SliderButtonPressedEvent?.Invoke(_sliderValue);
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

        #endregion

        private void OnDrawGizmosSelected()
        {
            if (_slideableObject == null)
            {
                return;
            }

            MakeLocalSliderScales();
            DrawJointRangeGizmo();
        }

        /// <summary>
        /// Draws gizmos representing the range of motion for the joint based on its type and direction.
        /// </summary>
        private void DrawJointRangeGizmo()
        {
            Vector3 jointPosition = _slideableObject.transform.localPosition;
            jointPosition.y = 0;


            Matrix4x4 m = _slideableObject.transform.localToWorldMatrix;
            m.SetTRS(this.transform.position, m.rotation, m.lossyScale);
            Gizmos.matrix = m;

            float SliderTravelDistanceHalf = _sliderTravelDistance / 2;

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
    }
}