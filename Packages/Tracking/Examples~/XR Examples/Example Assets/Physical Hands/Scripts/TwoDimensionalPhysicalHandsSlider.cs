/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Attributes;
using Leap.PhysicalHands;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.PhysicalHandsExamples
{
    [RequireComponent(typeof(Rigidbody))]
    public class TwoDimensionalPhysicalHandsSlider : MonoBehaviour
    {
        [SerializeField, OnEditorChange("AutoAssignConnectedButton")]
        public GameObject _slideableObject;

        public UnityEvent<Vector2> TwoDimSliderChangeEvent = new UnityEvent<Vector2>();
        public UnityEvent<Vector2> TwoDimSliderButtonPressedEvent = new UnityEvent<Vector2>();
        public UnityEvent<Vector2> TwoDimSliderButtonUnPressedEvent = new UnityEvent<Vector2>();

        /// <summary>
        /// The travel distance of the two-dimensional slider.
        /// i.e. slider center point +/- slider travel distance (or half the full travel of the slider) in both X and Y axes.
        /// </summary>
        [SerializeField]
        public Vector2 SliderTravelDistance = new Vector2(0.22f, 0.22f);

        /// <summary>
        /// Number of segments for the two-dimensional slider to use.
        /// 0 = unlimited segments.
        /// </summary>
        [SerializeField]
        public Vector2 _numberOfSegments = Vector2.zero;

        [SerializeField]
        public Vector2 _startPosition = Vector2.zero;

        [SerializeField]
        public PhysicalHandsButton _connectedButton;

        private bool _freezeIfNotActive = true;

        [SerializeField]
        private Vector2 _sliderValue = Vector2.zero;
        public Vector2 SliderValue => _sliderValue;

        private Rigidbody _slideableObjectRigidbody;
        private List<ConfigurableJoint> _configurableJoints = new List<ConfigurableJoint>();

        private float _sliderXZeroPos = 0;
        private float _sliderZZeroPos = 0;

        private Vector2 _prevSliderValue = Vector2.zero;

        private Quaternion _initialRotation;
        private Vector3 _initialPosition;

        private Vector2 _localTwoDimSliderTravelDistanceHalf;

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
            _sliderValue = CalculateSliderValue();
            // Check if the slider value has changed
            if (_prevSliderValue != _sliderValue)
            {
                // Send slider change event
                TwoDimSliderChangeEvent?.Invoke(_sliderValue);
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

            if (Mathf.Abs(_slideableObject.transform.localPosition.y - _initialPosition.y) > 0.01)
            {
                _slideableObject.transform.localPosition = new Vector3(
                    _slideableObject.transform.localPosition.x,
                    _initialPosition.y,
                    _slideableObject.transform.localPosition.z);
            }
        }

        #endregion

        #region Set Up

        /// <summary>
        /// Initialize the slider, setting up the configurable joints, setting the start position and working out slider values
        /// </summary>
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

            _slideableObject.transform.localPosition = new Vector3(0, _slideableObject.transform.localPosition.y, 0);

            ConfigureSlideableObject();

            // Set up the slider
            SetUpTwoDimSlider();

            // Update the zero position of the slider
            UpdateTwoDimensionalSliderZeroPos();

            ConfigureConnectedButton();

            // Freeze or unfreeze slider position based on the flag
            ApplySliderStartPosition();
        }

        /// <summary>
        /// Calculates local slider scales based on the slider type and direction.
        /// </summary>
        private void MakeLocalSliderScales()
        {

            _localTwoDimSliderTravelDistanceHalf.x = SliderTravelDistance.x / transform.lossyScale.x / 2;
            _localTwoDimSliderTravelDistanceHalf.y = SliderTravelDistance.y / transform.lossyScale.z / 2;
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

            slidePos.x = Utils.Map(_startPosition.x, 0, 1, 0, _localTwoDimSliderTravelDistanceHalf.x * 2) + _sliderXZeroPos;
            slidePos.z = Utils.Map(_startPosition.y, 0, 1, 0, _localTwoDimSliderTravelDistanceHalf.y * 2) + _sliderZZeroPos;

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
        /// Updates the zero position for a two-dimensional slider based on its direction.
        /// </summary>
        private void UpdateTwoDimensionalSliderZeroPos()
        {
            // Initialize offset variables
            float xOffset = _configurableJoints.ElementAt(0).anchor.x - _localTwoDimSliderTravelDistanceHalf.x;
            float zOffset = _configurableJoints.ElementAt(1).anchor.z - _localTwoDimSliderTravelDistanceHalf.y;

            // Update zero positions based on calculated offsets and current object position
            _sliderXZeroPos = xOffset + _slideableObject.transform.localPosition.x;
            _sliderZZeroPos = zOffset + _slideableObject.transform.localPosition.z;
        }

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
        /// Sets up a two-dimensional slider.
        /// </summary>
        private void SetUpTwoDimSlider()
        {
            if (_slideableObject == null)
            {
                Debug.LogWarning("There is no slideable object. Please add one to use the slider.", this.gameObject);
                return;
            }

            _configurableJoints = _slideableObject.GetComponents<ConfigurableJoint>().ToList<ConfigurableJoint>();

            while (_configurableJoints.Count < 2)
            {
                _configurableJoints.Add(_slideableObject.AddComponent<ConfigurableJoint>());
            }

            foreach (var joint in _configurableJoints)
            {
                SetUpConfigurableJoint(joint);
            }

            //Set up joint limits for separate travel distances on each axis
            SoftJointLimit linerJointLimit = new SoftJointLimit();
            linerJointLimit.limit = SliderTravelDistance.x / 2;
            _configurableJoints.ElementAt(0).linearLimit = linerJointLimit;

            linerJointLimit.limit = SliderTravelDistance.y / 2;
            _configurableJoints.ElementAt(1).linearLimit = linerJointLimit;

            _configurableJoints.ElementAt(0).xMotion = ConfigurableJointMotion.Limited;
            _configurableJoints.ElementAt(1).xMotion = ConfigurableJointMotion.Free;
            _configurableJoints.ElementAt(0).zMotion = ConfigurableJointMotion.Free;
            _configurableJoints.ElementAt(1).zMotion = ConfigurableJointMotion.Limited;
        }

        #endregion
        #endregion

        #region Update

        /// <summary>
        /// Updates the position of a slider object based on the provided value or two-dimensional value.
        /// </summary>
        /// <param name="twoDimValue">The two-dimensional value representing the position along the slider in two dimensions.</param>
        private void UpdateSliderPos(Vector2 twoDimValue)
        {
            // Get the current position of the slider object
            Vector3 slidePos = _slideableObject.transform.localPosition;

            slidePos.x = Utils.Map(twoDimValue.x, 0, 1, 0, _localTwoDimSliderTravelDistanceHalf.x * 2) + _sliderXZeroPos;
            slidePos.z = Utils.Map(twoDimValue.y, 0, 1, 0, _localTwoDimSliderTravelDistanceHalf.y * 2) + _sliderZZeroPos;

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
        private Vector2 CalculateSliderValue()
        {
            Vector2 changeFromZero = Vector2.zero;
            changeFromZero.x = _slideableObject.transform.localPosition.x - _sliderXZeroPos;
            changeFromZero.y = _slideableObject.transform.localPosition.z - _sliderZZeroPos;

            return new Vector2(
                Utils.Map(changeFromZero.x, 0, _localTwoDimSliderTravelDistanceHalf.x * 2, 0, 1),
                Utils.Map(changeFromZero.y, 0, _localTwoDimSliderTravelDistanceHalf.y * 2, 0, 1));
        }

        /// <summary>
        /// Snaps the slider to the nearest segment based on its type and direction.
        /// </summary>
        private void SnapToSegment()
        {
            // Initialize variables
            Vector2 twoDimValue = Vector2.zero;

            // Determine the slider type and direction

            // Snap to segments on X and Z axes
            twoDimValue.x = GetClosestStep(_sliderValue.x, (int)_numberOfSegments.x);
            twoDimValue.y = GetClosestStep(_sliderValue.y, (int)_numberOfSegments.y);

            // Update the slider position
            UpdateSliderPos(twoDimValue);
        }

        /// <summary>
        /// Wourk out which step is closest to the current slider value
        /// </summary>
        /// <param name="inputNumber">The currrent value of the slider.</param>
        /// <param name="numberOfSegments">How many steps should the slider be divided into.</param>
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
        void SnapSliderOnRelease()
        {
            // Snap to segment if applicable
            SnapToSegment();

            // Calculate the slider value based on the change in position
            _sliderValue = CalculateSliderValue();

            // Check if the slider value has changed
            if (_prevSliderValue != _sliderValue)
            {
                // Send slider change event
                TwoDimSliderChangeEvent?.Invoke(_sliderValue);
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

            TwoDimSliderButtonPressedEvent?.Invoke(_sliderValue);
        }

        private void ButtonUnPressed()
        {
            if (_freezeIfNotActive)
            {
                FreezeSliderPosition();
            }

            SnapSliderOnRelease();

            TwoDimSliderButtonUnPressedEvent?.Invoke(_sliderValue);
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
            Matrix4x4 m = _slideableObject.transform.localToWorldMatrix;
            m.SetTRS(this.transform.position, m.rotation, m.lossyScale);
            Gizmos.matrix = m;

            Vector2 TwoDimSliderTravelDistanceHalf = SliderTravelDistance / 2;

            Gizmos.color = Color.red; // X axis
            Gizmos.DrawLine(jointPosition + Vector3.right * (TwoDimSliderTravelDistanceHalf.x / _slideableObject.transform.lossyScale.x), jointPosition - Vector3.right * (TwoDimSliderTravelDistanceHalf.x / _slideableObject.transform.lossyScale.x));
            Gizmos.color = Color.blue; // Z axis
            Gizmos.DrawLine(jointPosition + Vector3.forward * (TwoDimSliderTravelDistanceHalf.y / _slideableObject.transform.lossyScale.z), jointPosition - Vector3.forward * (TwoDimSliderTravelDistanceHalf.y / _slideableObject.transform.lossyScale.z));
            Vector3 extents = new Vector3(SliderTravelDistance.x / _slideableObject.transform.lossyScale.x, 0, SliderTravelDistance.y / _slideableObject.transform.lossyScale.z);

            DrawBoxGizmo(jointPosition, extents, Color.cyan);
        }

        private void DrawBoxGizmo(Vector3 position, Vector3 size, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawWireCube(position, size);
        }
    }
}
