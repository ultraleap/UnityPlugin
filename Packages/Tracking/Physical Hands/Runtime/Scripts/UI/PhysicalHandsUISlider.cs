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
    public class PhysicalHandsUISlider : MonoBehaviour
    {

        [SerializeField]
        GameObject _slideableObject;

        #region Direction Enums

        public enum SliderType
        {
            ONE_DIMENSIONAL,
            TWO_DIMENSIONAL,
        }
        [SerializeField, Leap.Unity.Attributes.OnEditorChange("SliderTypeChanged")]
        internal SliderType _sliderType = SliderType.ONE_DIMENSIONAL;

        private void SliderTypeChanged()
        {
            Debug.Log("SliderTypeChanged");
            if(_slideableObject)
            {
                foreach (var joint in _slideableObject.GetComponents<ConfigurableJoint>())
                {
                    DestroyImmediate(joint);
                }
                
            }
        }

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

        #endregion

        public UnityEvent<Dictionary<char,float>> SliderChangeEvent = new UnityEvent<Dictionary<char, float>>();
        public UnityEvent<Dictionary<char, float>> SliderButtonPressedEvent = new UnityEvent<Dictionary<char, float>>();
        public UnityEvent<Dictionary<char, float>> SliderButtonUnPressedEvent = new UnityEvent<Dictionary<char, float>>();

        /// <summary>
        /// The travel distance of the slider (from the central point).
        /// i.e. slider center point +/- slider travel distance (or half the full travel of the slider).
        /// </summary>
        [SerializeField]
        public float SliderTravelDistance = 0.22f;

        /// <summary>
        /// The travel distance of the two-dimensional slider.
        /// i.e. slider center point +/- slider travel distance (or half the full travel of the slider) in both X and Y axes.
        /// </summary>
        [SerializeField]
        public Vector2 TwoDimSliderTravelDistance = new Vector2(0.22f, 0.22f);

        /// <summary>
        /// Number of segments for the one-dimensional slider to use.
        /// 0 = unlimited segments.
        /// </summary>
        [SerializeField]
        public int _numberOfSegments = 0;

        /// <summary>
        /// Number of segments for the two-dimensional slider to use.
        /// 0 = unlimited segments.
        /// </summary>
        [SerializeField]
        public Vector2 _twoDimNumberOfSegments = Vector2.zero;

        [SerializeField]
        public float _startPosition = 0;

        [SerializeField]
        public Vector2 _twoDimStartPosition = Vector2.zero;

        [SerializeField]
        private Vector3 _axisChangeFromZero = Vector3.zero;

        [SerializeField]
        private PhysicalHandsButtonBase _connectedButton;

        
        private bool _freezeIfNotActive = true;

        [SerializeField]
        private Vector3 _sliderValue = Vector3.zero;

        private Rigidbody _slideableObjectRigidbody;
        private List<ConfigurableJoint> _configurableJoints = new List<ConfigurableJoint>();

        private float _sliderXZeroPos = 0;
        private float _sliderYZeroPos = 0;
        private float _sliderZZeroPos = 0;

        private bool _sliderReleasedLastFrame = false;
        private Vector3 _prevSliderValue = Vector3.zero;

        private Quaternion _initialRotation;


        /// <summary>
        /// Use this to get the slider value on all axes.
        /// </summary>
        /// <returns>Vector3 ofslider values.</returns>
        public Dictionary<char, float> GetSliderValue()
        {
            switch (_sliderType)
            {
                case SliderType.ONE_DIMENSIONAL:
                    {
                        switch (_sliderDirection)
                        {
                            case SliderDirection.X:
                                return new Dictionary<char, float>() { { 'x', _sliderValue.x } };
                            case SliderDirection.Y:
                                return new Dictionary<char, float>() { { 'y', _sliderValue.y } };
                            case SliderDirection.Z:
                                return new Dictionary<char, float>() { { 'z', _sliderValue.z } };
                        }
                        break;
                    }
                case SliderType.TWO_DIMENSIONAL:
                    {
                        switch (_twoDimSliderDirection)
                        {
                            case TwoDimSliderDirection.XY:
                                return new Dictionary<char, float>() { { 'x', _sliderValue.x }, { 'y', _sliderValue.y } };
                            case TwoDimSliderDirection.XZ:
                                return new Dictionary<char, float>() { { 'x', _sliderValue.x }, { 'z', _sliderValue.y } };
                            case TwoDimSliderDirection.YZ:
                                return new Dictionary<char, float>() { { 'y', _sliderValue.x }, { 'z', _sliderValue.y } };
                        }
                        break;
                    }
            }
            return null;
        }

        #region Unity Methods

        /// <summary>
        /// Initializes the slider when the GameObject is enabled.
        /// </summary>
        private void Start()
        {
            // Check if the slideable object is assigned
            if (_slideableObject == null)
            {
                // Display a message if the slideable object is not assigned and disable the script
                Debug.LogWarning("There is no slideable object. Please add one to use the slider. \n This script has been disabled", this.gameObject);
                this.enabled = false;
                return;
            }

            // Ensure the slideable object has a PhysicalHandsUISliderHelper component
            PhysicalHandsUISliderHelper slideHelper;
            if (!_slideableObject.TryGetComponent<PhysicalHandsUISliderHelper>(out slideHelper))
            {
                slideHelper = _slideableObject.AddComponent<PhysicalHandsUISliderHelper>();
            }

            // Assign event handlers for hand interactions
            slideHelper._onHandGrab += OnHandGrab;
            slideHelper._onHandGrabExit += OnHandGrabExit;
            slideHelper._onHandContact += OnHandContact;
            slideHelper._onHandContactExit += OnHandContactExit;

            // Ensure the slideable object has a Rigidbody component
            if (!_slideableObject.TryGetComponent<Rigidbody>(out _slideableObjectRigidbody))
            {
                _slideableObjectRigidbody = _slideableObject.AddComponent<Rigidbody>();
            }

            _slideableObjectRigidbody.useGravity = false;
            this.GetComponent<Rigidbody>().isKinematic = true;
            _initialRotation = _slideableObject.transform.localRotation;


            // Set up the slider based on its type
            switch (_sliderType)
            {
                case SliderType.ONE_DIMENSIONAL:
                    SetUpSlider();
                    break;
                case SliderType.TWO_DIMENSIONAL:
                    SetUpTwoDimSlider();
                    break;
            }

            // Update the zero position of the slider
            UpdateSliderZeroPos();

            // Check if a connected button is assigned
            if (_connectedButton == null)
            {
                if (_slideableObject.TryGetComponent<PhysicalHandsButtonBase>(out _connectedButton))
                {
                    // Assign event listeners for button events
                    _connectedButton.OnButtonPressed?.AddListener(ButtonPressed);
                    _connectedButton.OnButtonUnPressed?.AddListener(ButtonUnPressed);
                }
            }
            else
            {
                // Assign event listeners for button events
                _connectedButton.OnButtonPressed?.AddListener(ButtonPressed);
                _connectedButton.OnButtonUnPressed?.AddListener(ButtonUnPressed);
            }

            // Freeze or unfreeze slider position based on the flag
            if (_freezeIfNotActive == false)
            {
                UnFreezeSliderPosition();
            }
            else
            {
                FreezeSliderPosition();
            }

            // Update the slider position to the starting position
            UpdateSliderPos(_startPosition, _twoDimStartPosition);
        }



        private void OnDisable()
        {
            if(_connectedButton != null)
            {
                _connectedButton.OnButtonPressed.RemoveListener(ButtonPressed);
                _connectedButton.OnButtonUnPressed.RemoveListener(ButtonUnPressed);
            }
        }

        private void OnEnable()
        {
            if (_connectedButton != null)
            {
                _connectedButton.OnButtonPressed.AddListener(ButtonPressed);
                _connectedButton.OnButtonUnPressed.AddListener(ButtonUnPressed);
            }
        }



        /// <summary>
        /// Update method called once per frame.
        /// </summary>
        private void Update()
        {
            // Calculate the change in position of the slider object from its zero position
            _axisChangeFromZero.x = _slideableObject.transform.localPosition.x - _sliderXZeroPos;
            _axisChangeFromZero.y = _slideableObject.transform.localPosition.y - _sliderYZeroPos;
            _axisChangeFromZero.z = _slideableObject.transform.localPosition.z - _sliderZZeroPos;

            // Calculate the slider value based on the change in position
            _sliderValue = CalculateSliderValue(_axisChangeFromZero);

            // Check if the slider value has changed
            if (_prevSliderValue != _sliderValue)
            {
                // Send slider change event
                SendSliderEvent(SliderChangeEvent);

                // Check if the slider was released last frame
                if (_sliderReleasedLastFrame)
                {
                    // Snap to segment if applicable
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
                                if (_twoDimNumberOfSegments.x != 0 || _twoDimNumberOfSegments.y != 0)
                                {
                                    SnapToSegment();
                                }
                                break;
                            }
                    }

                    // Reset flag and update previous slider value
                    _sliderReleasedLastFrame = false;
                    _prevSliderValue = _sliderValue;
                }
            }
        }

        private void FixedUpdate()
        {
            // Account for joints beimng a little weird when rotating parent rigidbodies.
            // Set the rotation to the initial rotation of the sliding object when it gets too far away. 
            // Note. This will stop the slideable object from being rotated at all at runtime
            if(Quaternion.Angle(_slideableObject.transform.localRotation, _initialRotation) > 0.5f)
            {
                Debug.Log("Rotation reset");
                _slideableObject.transform.localRotation = _initialRotation;
            }

        }

        #endregion

        #region Set Up

        /// <summary>
        /// Updates the zero position of the slider based on its type.
        /// </summary>
        private void UpdateSliderZeroPos()
        {
            // Determine the slider type and call the appropriate method
            switch (_sliderType)
            {
                case SliderType.ONE_DIMENSIONAL:
                    UpdateOneDimensionalSliderZeroPos();
                    break;
                case SliderType.TWO_DIMENSIONAL:
                    UpdateTwoDimensionalSliderZeroPos();
                    break;
            }
        }

        /// <summary>
        /// Updates the zero position for a one-dimensional slider based on its direction.
        /// </summary>
        private void UpdateOneDimensionalSliderZeroPos()
        {
            // Initialize offset variables
            float xOffset = 0;
            float yOffset = 0;
            float zOffset = 0;

            // Determine the slider direction and calculate offsets accordingly
            switch (_sliderDirection)
            {
                case SliderDirection.X:
                    xOffset = _configurableJoints.First().anchor.x -SliderTravelDistance;
                    break;
                case SliderDirection.Y:
                    yOffset = _configurableJoints.First().anchor.y -SliderTravelDistance;
                    break;
                case SliderDirection.Z:
                    zOffset = _configurableJoints.First().anchor.z -SliderTravelDistance;
                    break;
            }

            // Update zero positions based on calculated offsets and current object position
            _sliderXZeroPos = xOffset + _slideableObject.transform.localPosition.x;
            _sliderYZeroPos = yOffset + _slideableObject.transform.localPosition.y;
            _sliderZZeroPos = zOffset + _slideableObject.transform.localPosition.z;
        }

        /// <summary>
        /// Updates the zero position for a two-dimensional slider based on its direction.
        /// </summary>
        private void UpdateTwoDimensionalSliderZeroPos()
        {
            // Initialize offset variables
            float xOffset = 0;
            float yOffset = 0;
            float zOffset = 0;

            // Determine the slider direction and calculate offsets accordingly
            switch (_twoDimSliderDirection)
            {
                case TwoDimSliderDirection.XY:
                    xOffset = _configurableJoints.ElementAt(0).anchor.x -TwoDimSliderTravelDistance.x;
                    yOffset = _configurableJoints.ElementAt(1).anchor.y -TwoDimSliderTravelDistance.y;
                    break;
                case TwoDimSliderDirection.XZ:
                    xOffset = _configurableJoints.ElementAt(0).anchor.x -TwoDimSliderTravelDistance.x;
                    zOffset = _configurableJoints.ElementAt(1).anchor.z -TwoDimSliderTravelDistance.y;
                    break;
                case TwoDimSliderDirection.YZ:
                    yOffset = _configurableJoints.ElementAt(0).anchor.y -TwoDimSliderTravelDistance.x;
                    zOffset = _configurableJoints.ElementAt(1).anchor.z -TwoDimSliderTravelDistance.y;
                    break;
            }

            // Update zero positions based on calculated offsets and current object position
            _sliderXZeroPos = xOffset + _slideableObject.transform.localPosition.x;
            _sliderYZeroPos = yOffset + _slideableObject.transform.localPosition.y;
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
            ConfigurableJoint configurableJoint;
            if(_slideableObject == null)
            {
                Debug.LogWarning("There is no slideable object. Please add one to use the slider.", this.gameObject);
                return;
            }
            _configurableJoints = _slideableObject.GetComponents<ConfigurableJoint>().ToList<ConfigurableJoint>();
            _slideableObject.TryGetComponent<ConfigurableJoint>(out configurableJoint);

            if (_configurableJoints.Count < 1 && configurableJoint == null)
            {
                _configurableJoints.Add(_slideableObject.AddComponent<ConfigurableJoint>());
            }
            else
            {
                _configurableJoints.Add(configurableJoint);
            }

            foreach (ConfigurableJoint joint in _configurableJoints)
            {
                SetUpConfigurableJoint(joint);

                SoftJointLimit linearJointLimit = new SoftJointLimit();
                linearJointLimit.limit = SliderTravelDistance + 0.01f;
                joint.linearLimit = linearJointLimit;

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
            linerJointLimit.limit = TwoDimSliderTravelDistance.x + 0.01f;
            _configurableJoints.ElementAt(0).linearLimit = linerJointLimit;
            linerJointLimit.limit = TwoDimSliderTravelDistance.y + 0.01f;
            _configurableJoints.ElementAt(1).linearLimit = linerJointLimit;


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
        #endregion

        private void OnDrawGizmosSelected()
        {            
            // Set up the slider based on its type
            switch (_sliderType)
            {
                case SliderType.ONE_DIMENSIONAL:
                    SetUpSlider();
                    break;
                case SliderType.TWO_DIMENSIONAL:
                    SetUpTwoDimSlider();
                    break;
            }
            if (_configurableJoints != null)
            {
                foreach (ConfigurableJoint joint in _configurableJoints)
                {
                    DrawJointRangeGizmo(joint.transform.position);
                }
            }
        }

        /// <summary>
        /// Draws gizmos representing the range of motion for the joint based on its type and direction.
        /// </summary>
        /// <param name="jointPosition">Position of the joint.</param>
        private void DrawJointRangeGizmo(Vector3 jointPosition)
        {
            switch (_sliderType)
            {
                case SliderType.ONE_DIMENSIONAL:
                    {
                        switch (_sliderDirection)
                        {
                            case SliderDirection.X:
                                Gizmos.color = Color.red; // X axis
                                Gizmos.DrawLine(jointPosition + transform.right * SliderTravelDistance, jointPosition - transform.right * SliderTravelDistance);
                                break;
                            case SliderDirection.Y:
                                Gizmos.color = Color.green; // Y axis
                                Gizmos.DrawLine(jointPosition + transform.up * SliderTravelDistance, jointPosition - transform.up * SliderTravelDistance);
                                break;
                            case SliderDirection.Z:
                                Gizmos.color = Color.blue; // Z axis
                                Gizmos.DrawLine(jointPosition + transform.forward * SliderTravelDistance, jointPosition - transform.forward * SliderTravelDistance);
                                break;
                        }
                        break;
                    }
                case SliderType.TWO_DIMENSIONAL:
                    {
                        switch (_twoDimSliderDirection)
                        {
                            case TwoDimSliderDirection.XY:
                                Gizmos.color = Color.red; // X axis
                                Gizmos.DrawLine(jointPosition + transform.right * TwoDimSliderTravelDistance.x, jointPosition - transform.right * TwoDimSliderTravelDistance.x);
                                Gizmos.color = Color.green; // Y axis
                                Gizmos.DrawLine(jointPosition + transform.up * TwoDimSliderTravelDistance.y, jointPosition - transform.up * TwoDimSliderTravelDistance.y);
                                break;
                            case TwoDimSliderDirection.XZ:
                                Gizmos.color = Color.red; // X axis
                                Gizmos.DrawLine(jointPosition + transform.right * TwoDimSliderTravelDistance.x, jointPosition - transform.right * TwoDimSliderTravelDistance.x);
                                Gizmos.color = Color.blue; // Z axis
                                Gizmos.DrawLine(jointPosition + transform.forward * TwoDimSliderTravelDistance.y, jointPosition - transform.forward * TwoDimSliderTravelDistance.y);
                                break;
                            case TwoDimSliderDirection.YZ:
                                Gizmos.color = Color.green; // Y axis
                                Gizmos.DrawLine(jointPosition + transform.up * TwoDimSliderTravelDistance.x, jointPosition - transform.up * TwoDimSliderTravelDistance.x);
                                Gizmos.color = Color.blue; // Z axis
                                Gizmos.DrawLine(jointPosition + transform.forward * TwoDimSliderTravelDistance.y, jointPosition - transform.forward * TwoDimSliderTravelDistance.y);
                                break;
                        }
                        break;
                    }
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

            _sliderReleasedLastFrame = true;

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

            _sliderReleasedLastFrame = true;
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
            _sliderReleasedLastFrame = true;
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
        /// <param name="twoDimUnityEvent"></param>
        void SendSliderEvent(UnityEvent<Dictionary<char, float>> unityEvent)
        {
            switch (_sliderType)
            {
                case SliderType.ONE_DIMENSIONAL:
                    {
                        switch (_sliderDirection)
                        {
                            case SliderDirection.X:
                                unityEvent.Invoke(new Dictionary<char, float> { { 'x', _sliderValue.x } });
                                break;
                            case SliderDirection.Y:
                                unityEvent.Invoke(new Dictionary<char, float> { { 'y', _sliderValue.y } });
                                break;
                            case SliderDirection.Z:
                                unityEvent.Invoke(new Dictionary<char, float> { { 'z', _sliderValue.z } });
                                break;
                        }
                        break;
                    }
                case SliderType.TWO_DIMENSIONAL:
                    {
                        switch (_twoDimSliderDirection)
                        {
                            case TwoDimSliderDirection.XY:
                                unityEvent.Invoke(new Dictionary<char, float> { { 'x', _sliderValue.x }, { 'y', _sliderValue.y } });
                                break;
                            case TwoDimSliderDirection.XZ:
                                unityEvent.Invoke(new Dictionary<char, float> { { 'x', _sliderValue.x }, { 'z', _sliderValue.z } });
                                break;
                            case TwoDimSliderDirection.YZ:
                                unityEvent.Invoke(new Dictionary<char, float> { { 'y', _sliderValue.y }, { 'z', _sliderValue.z } });
                                break;
                        }
                        break;
                    }
            }
        }

        #endregion

        #region Update

        /// <summary>
        /// Updates the position of a slider object based on the provided value or two-dimensional value.
        /// </summary>
        /// <param name="value">The value representing the position along the slider.</param>
        /// <param name="twoDimValue">The two-dimensional value representing the position along the slider in two dimensions.</param>
        private void UpdateSliderPos(float value, Vector2 twoDimValue)
        {
            // Get the current position of the slider object
            Vector3 slidePos = _slideableObject.transform.localPosition;

            // Determine the type of slider and update its position accordingly
            switch (_sliderType)
            {
                case SliderType.ONE_DIMENSIONAL:
                    // Update position for one-dimensional slider based on slider direction
                    switch (_sliderDirection)
                    {
                        case SliderDirection.X:
                            slidePos.x = Utils.Map(value, 0, 1, 0, SliderTravelDistance * 2) + _sliderXZeroPos;
                            break;
                        case SliderDirection.Y:
                            slidePos.y = Utils.Map(value, 0, 1, 0, SliderTravelDistance * 2) + _sliderYZeroPos;
                            break;
                        case SliderDirection.Z:
                            slidePos.z = Utils.Map(value, 0, 1, 0, SliderTravelDistance * 2) + _sliderZZeroPos;
                            break;
                    }
                    break;
                case SliderType.TWO_DIMENSIONAL:
                    // Update position for two-dimensional slider based on slider direction
                    switch (_twoDimSliderDirection)
                    {
                        case TwoDimSliderDirection.XY:
                            slidePos.x += Utils.Map(twoDimValue.x, 0, 1, 0, TwoDimSliderTravelDistance.x * 2) + _sliderXZeroPos;
                            slidePos.y += Utils.Map(twoDimValue.y, 0, 1, 0, TwoDimSliderTravelDistance.y * 2) + _sliderYZeroPos;
                            break;
                        case TwoDimSliderDirection.XZ:
                            slidePos.x += Utils.Map(twoDimValue.x, 0, 1, 0, TwoDimSliderTravelDistance.x * 2) + _sliderXZeroPos;
                            slidePos.z += Utils.Map(twoDimValue.y, 0, 1, 0, TwoDimSliderTravelDistance.y * 2) + _sliderZZeroPos;
                            break;
                        case TwoDimSliderDirection.YZ:
                            slidePos.y += Utils.Map(twoDimValue.x, 0, 1, 0, TwoDimSliderTravelDistance.x * 2) + _sliderYZeroPos;
                            slidePos.z += Utils.Map(twoDimValue.y, 0, 1, 0, TwoDimSliderTravelDistance.y * 2) + _sliderZZeroPos;
                            break;
                    }
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
            switch (_sliderType)
            {
                case SliderType.ONE_DIMENSIONAL:
                    switch (_sliderDirection)
                    {
                        case SliderDirection.X:
                            return new Vector3(
                                Utils.Map(changeFromZero.x, 0, SliderTravelDistance * 2, 0, 1),
                                0,
                                0);
                        case SliderDirection.Y:
                            return new Vector3(
                                0,
                                Utils.Map(changeFromZero.y, 0, SliderTravelDistance * 2, 0, 1),
                                0);
                        case SliderDirection.Z:
                            return new Vector3(
                                0,
                                0,
                                Utils.Map(changeFromZero.z, 0, SliderTravelDistance * 2, 0, 1));
                    }
                    break;
                case SliderType.TWO_DIMENSIONAL:
                    switch (_twoDimSliderDirection)
                    {
                        case TwoDimSliderDirection.XY:
                            return new Vector3(
                                Utils.Map(changeFromZero.x, 0, TwoDimSliderTravelDistance.x * 2, 0, 1), 
                                Utils.Map(changeFromZero.y, 0, TwoDimSliderTravelDistance.y * 2, 0, 1), 
                                0);
                        case TwoDimSliderDirection.XZ:
                            return new Vector3(
                                Utils.Map(changeFromZero.x, 0, TwoDimSliderTravelDistance.x * 2, 0, 1),
                                0,
                                Utils.Map(changeFromZero.z, 0, TwoDimSliderTravelDistance.y * 2, 0, 1));
                        case TwoDimSliderDirection.YZ:
                            return new Vector3(
                                0,
                                Utils.Map(changeFromZero.y, 0, TwoDimSliderTravelDistance.x * 2, 0, 1),
                                Utils.Map(changeFromZero.z, 0, TwoDimSliderTravelDistance.y * 2, 0, 1));
                    }
                    break;
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
            float value = 0;
            Vector2 twoDimValue = Vector2.zero;

            // Determine the slider type and direction
            switch (_sliderType)
            {
                case SliderType.ONE_DIMENSIONAL:
                    // For one-dimensional sliders
                    switch (_sliderDirection)
                    {
                        case SliderDirection.X:
                            closestStep = GetClosestStep(_sliderValue.x, _numberOfSegments);
                            value = _sliderXZeroPos + (closestStep * (SliderTravelDistance * 2));
                            break;
                        case SliderDirection.Y:
                            closestStep = GetClosestStep(_sliderValue.y, _numberOfSegments);
                            value = _sliderYZeroPos + (closestStep * (SliderTravelDistance * 2));
                            break;
                        case SliderDirection.Z:
                            closestStep = GetClosestStep(_sliderValue.z, _numberOfSegments);
                            value = _sliderZZeroPos + (closestStep * (SliderTravelDistance * 2));
                            break;
                    }
                    break;
                case SliderType.TWO_DIMENSIONAL:
                    // For two-dimensional sliders
                    switch (_twoDimSliderDirection)
                    {
                        case TwoDimSliderDirection.XY:
                            // Snap to segments on X and Y axes
                            if (_twoDimNumberOfSegments.x != 0)
                            {
                                closestStep = GetClosestStep(_sliderValue.x, (int)_twoDimNumberOfSegments.x);
                                twoDimValue.x = _sliderXZeroPos + (closestStep * (SliderTravelDistance * 2));
                            }
                            if (_twoDimNumberOfSegments.y != 0)
                            {
                                closestStep = GetClosestStep(_sliderValue.y, (int)_twoDimNumberOfSegments.y);
                                twoDimValue.y = _sliderYZeroPos + (closestStep * (SliderTravelDistance * 2));
                            }
                            break;
                        case TwoDimSliderDirection.XZ:
                            // Snap to segments on X and Z axes
                            if (_twoDimNumberOfSegments.x != 0)
                            {
                                closestStep = GetClosestStep(_sliderValue.x, (int)_twoDimNumberOfSegments.x);
                                twoDimValue.x = _sliderXZeroPos + (closestStep * (SliderTravelDistance * 2));
                            }
                            if (_twoDimNumberOfSegments.y != 0)
                            {
                                closestStep = GetClosestStep(_sliderValue.z, (int)_twoDimNumberOfSegments.y);
                                twoDimValue.y = _sliderZZeroPos + (closestStep * (SliderTravelDistance * 2));
                            }
                            break;
                        case TwoDimSliderDirection.YZ:
                            // Snap to segments on Y and Z axes
                            if (_twoDimNumberOfSegments.x != 0)
                            {
                                closestStep = GetClosestStep(_sliderValue.y, (int)_twoDimNumberOfSegments.x);
                                twoDimValue.x = _sliderYZeroPos + (closestStep * (SliderTravelDistance * 2));
                            }
                            if (_twoDimNumberOfSegments.y != 0)
                            {
                                closestStep = GetClosestStep(_sliderValue.z, (int)_twoDimNumberOfSegments.y);
                                twoDimValue.y = _sliderZZeroPos + (closestStep * (SliderTravelDistance * 2));
                            }
                            break;
                    }
                    break;
            }

            // Update the slider position
            UpdateSliderPos(closestStep, twoDimValue);
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
