/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.Interaction.PhysicsHands
{
    ///<summary>
    /// A physics-enabled button using joints. Activated by physically pressing the button, with events
    /// for hover, contact, press and all relevant opposites.
    ///</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicsButton : MonoBehaviour
    {
        // Consts used to modify thresholds of buttons pressed
        private const float FIVE_PERCENT_MULTIPLIER = 0.05F;
        private const float ONE_PERCENT_MULTIPLIER = 0.01F;

        #region Inspector

        [SerializeField, Tooltip("The rigidbody to use as spring anchor.")]
        private Rigidbody _rigidbody = null;
        [SerializeField, Tooltip("The element used to hold button specific components.")]
        private PhysicsButtonElement _buttonElement = null;

        [SerializeField, Tooltip("Tells the PhysicsButtonElement to ignore all collision between itself and objects in the scene that are not on the hand layers.")]
        private bool _handsOnly = false;
        public bool HandsOnly => _handsOnly;

        [Header("Motion Configuration")]
        /// <summary>
        /// The local position which the button will be limited to and will try to return to.
        /// </summary>
        [Tooltip("The local position which the button will be limited to and will try to return to.")]
        public float buttonHeightLimit = 0.02f;

        /// <summary>
        /// The spring force applied to the button to return it to its resting height.
        /// </summary>
        [SerializeField, Tooltip("The spring force applied to the button to return it to its resting height. " +
            "The higher the number, the faster the button will return to the original position.")]
        private float _springForce = 100f;

        /// <summary>
        /// The dampening amount to slow down the button when returning to its resting height.
        /// </summary>
        [SerializeField, Tooltip("The dampening amount to slow down the button when returning to its resting height.")]
        private float _springDampening = 5f;

        [Header("Toggle Configuration")]
        /// <summary>
        /// Enables the button to be used as a toggle. No toggle events will fire unless this is set to true.
        /// </summary>
        [Tooltip("Enables the button to be used as a toggle. No toggle events will fire unless this is set to true.")]
        public bool isToggleable = false;
        /// <summary>
        /// The local position which the button will be limited to and will try to return to when toggled.
        /// </summary>
        [Tooltip("The local position which the button will be limited to and will try to return to when toggled.")]
        public float buttonToggleHeightLimit = 0.01f;

        #endregion

        #region Events

        [SerializeField, Header("Events"), Tooltip("This event will only take into account hand presence and will not fire if only an object is used to hover it.")]
        private UnityEvent _OnHover = new UnityEvent();

        [SerializeField, Tooltip("This event will only take into account hand presence and will not fire if only an object is used to hover it.")]
        private UnityEvent _OnUnhover = new UnityEvent();

        [SerializeField, Tooltip("This event will only take into account hand presence and will not fire if only an object is used to hover it.")]
        private UnityEvent _OnContact = new UnityEvent();

        [SerializeField, Tooltip("This event will only take into account hand presence and will not fire if only an object is used to hover it.")]
        private UnityEvent _OnUncontact = new UnityEvent();

        [SerializeField]
        private UnityEvent _OnPress = new UnityEvent();

        [SerializeField]
        private UnityEvent _OnUnpress = new UnityEvent();

        [SerializeField]
        private UnityEvent<float> _OnPressedAmountChanged = new UnityEvent<float>();

        [SerializeField]
        private UnityEvent<bool> _OnToggle = new UnityEvent<bool>();

        public Action OnPress = () => { };
        public Action OnUnpress = () => { };

        /// <summary>
        /// This event will only take into account hand presence and will not fire if only an object is used to hover it.
        /// </summary>
        public Action OnHover = () => { };
        /// <summary>
        /// This event will only take into account hand presence and will not fire if only an object is used to hover it.
        /// </summary>
        public Action OnUnhover = () => { };

        /// <summary>
        /// This event will only take into account hand presence and will not fire if only an object is used to hover it.
        /// </summary>
        public Action OnContact = () => { };
        /// <summary>
        /// This event will only take into account hand presence and will not fire if only an object is used to hover it.
        /// </summary>
        public Action OnUncontact = () => { };

        public Action<float> OnPressedAmountChanged = (value) => { };

        public Action<bool> OnToggle = (value) => { };

        #endregion

        #region State

        protected bool _isPressed = false;
        /// <summary>
        /// Gets whether the button is currently held down.
        /// </summary>
        public bool isPressed => _isPressed;

        private bool _hovered = false;
        /// <summary>
        /// Gets whether the button is currently being hovered.
        /// </summary>
        public bool isHovered => _hovered;

        private bool _contact = false;
        /// <summary>
        /// Gets whether the button is currently being touched.
        /// </summary>
        public bool isContacted => _contact;
        private int _contactReleaseTime = 0;

        protected bool _pressedThisFrame = false;
        /// <summary>
        /// Gets whether the button was pressed during this Update frame.
        /// </summary>
        public bool pressedThisFrame => _pressedThisFrame;

        protected bool _unpressedThisFrame = false;
        /// <summary>
        /// Gets whether the button was unpressed this frame.
        /// </summary>
        public bool unpressedThisFrame => _unpressedThisFrame;

        private float _pressedAmount = 0F;
        /// <summary>
        /// Gets a normalized value between 0 and 1 based on how depressed the button
        /// currently is relative to its maximum depression. 0 represents a button fully at
        /// rest or pulled out beyond its resting position; 1 represents a fully-pressed
        /// button.
        /// </summary>
        public float pressedAmount
        {
            private set
            {
                if (value != _pressedAmount)
                {
                    _pressedAmount = value;
                    OnPressedAmountChanged?.Invoke(value);
                }
            }
            get { return _pressedAmount; }
        }

        private bool _isToggled = false;
        public bool IsToggled => _isToggled;

        private PhysicsProvider _provider;
        public PhysicsProvider Provider => _provider;

        #endregion

        #region Unity Events

        void Reset()
        {
            _buttonElement = GetComponentInChildren<PhysicsButtonElement>(true);
            if (_buttonElement != null)
            {
                SetupButton();
            }
        }

        private void Awake()
        {
            if (_buttonElement == null)
            {
                _buttonElement = GetComponentInChildren<PhysicsButtonElement>(true);
            }

            if (_buttonElement == null)
            {
                Debug.LogError("The PhysicsButton is missing a PhysicsButtonElement and has been disabled. Please ensure it has been added and assigned.", this);
                enabled = false;
            }
        }

        protected void OnDisable()
        {
            if (isPressed)
            {
                _unpressedThisFrame = true;
                OnUnpress?.Invoke();
                if (isToggleable)
                {
                    _isToggled = !_isToggled;
                    UpdateToggleJoint();
                    OnToggle?.Invoke(_isToggled);
                }
            }
            if (_hovered)
            {
                _hovered = false;
                OnUnhover?.Invoke();
            }
            if (_contact)
            {
                _contact = false;
                OnUncontact?.Invoke();
            }
        }

        protected void Start()
        {
            if (!_buttonElement.transform.IsChildOf(transform))
            {
                Debug.LogError("The assigned rigidbody is not a child of the PhysicsButton. The PhysicsButton has been disabled.", this);
                enabled = false;
            }

            SetupButton();

            _provider = FindObjectOfType<PhysicsProvider>(true);

            OnPress += _OnPress.Invoke;
            OnUnpress += _OnUnpress.Invoke;
            OnHover += _OnHover.Invoke;
            OnUnhover += _OnUnhover.Invoke;
            OnContact += _OnContact.Invoke;
            OnUncontact += _OnUncontact.Invoke;
            OnPressedAmountChanged += _OnPressedAmountChanged.Invoke;
            OnToggle += _OnToggle.Invoke;
        }

        protected void FixedUpdate()
        {
            if (_provider != null)
            {
                ProcessPhysicsEvents();
            }
        }

        private void ProcessPhysicsEvents()
        {
            if (_provider.IsObjectHovered(_buttonElement.Rigid))
            {
                if (!_hovered)
                {
                    _hovered = true;
                    OnHover?.Invoke();
                }

                if (_provider.GetObjectState(_buttonElement.Rigid, out var state))
                {
                    switch (state)
                    {
                        case PhysicsGraspHelper.State.Hover:
                            _contactReleaseTime = Mathf.Clamp(--_contactReleaseTime, 0, 100);
                            if (_contact && _contactReleaseTime == 0)
                            {
                                _contact = false;
                                OnUncontact?.Invoke();
                            }
                            break;
                        case PhysicsGraspHelper.State.Contact:
                        case PhysicsGraspHelper.State.Grasp:
                            if (!_contact)
                            {
                                _contact = true;
                                OnContact?.Invoke();
                                // Small wait to reduce erroneous uncontact events
                                _contactReleaseTime = 10;
                            }
                            // Ensure that we only allow hands to press the button
                            if (_handsOnly)
                            {
                                PressValidation();
                            }
                            break;
                    }
                }
            }
            else
            {
                if (_hovered)
                {
                    _hovered = false;
                    OnUnhover?.Invoke();
                }
                if (_contact)
                {
                    _contact = false;
                    OnUncontact?.Invoke();
                }
            }

            // If we want all physics objects to press it then go wild
            if (!_handsOnly)
            {
                PressValidation();
            }

            // Check if the button is at least 5% unpressed
            if (_isPressed && _buttonElement.transform.localPosition.y >= buttonHeightLimit * FIVE_PERCENT_MULTIPLIER)
            {
                _isPressed = false;
                _unpressedThisFrame = true;
                OnUnpress?.Invoke();
            }

            // Lerps up to 99% pressed to account for physics wobble
            pressedAmount = Mathf.InverseLerp(buttonHeightLimit * 0.99f, buttonHeightLimit * ONE_PERCENT_MULTIPLIER, _buttonElement.transform.localPosition.y);

            if (_pressedThisFrame && isToggleable)
            {
                _isToggled = !_isToggled;
                UpdateToggleJoint();
                OnToggle?.Invoke(_isToggled);
            }
        }

        private void PressValidation()
        {
            // Check if the button is more than 99% pressed to account for physics wobble
            if (!_isPressed && _buttonElement.transform.localPosition.y <= buttonHeightLimit * ONE_PERCENT_MULTIPLIER)
            {
                _isPressed = true;
                _pressedThisFrame = true;
                OnPress?.Invoke();
            }
        }

        private void LateUpdate()
        {
            if (_pressedThisFrame)
            {
                _pressedThisFrame = false;
            }
            if (_unpressedThisFrame)
            {
                _unpressedThisFrame = false;
            }
        }

        private void SetupButton()
        {
            if (_buttonElement == null)
                return;

            _rigidbody.mass = 1f;
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            _buttonElement.Rigid.mass = 1f;
            _buttonElement.Rigid.drag = 0;
            _buttonElement.Rigid.useGravity = false;
            _buttonElement.Rigid.isKinematic = false;
            _buttonElement.Rigid.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            SetupJoint(_buttonElement.Joint);
        }

        private void SetupJoint(ConfigurableJoint joint)
        {
            joint.connectedBody = _rigidbody;
            joint.anchor = Vector3.up * 0.5f;
            joint.axis = Vector3.right;
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = Vector3.up * buttonHeightLimit;

            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;

            SoftJointLimit softLimit = joint.linearLimit;
            softLimit.limit = buttonHeightLimit / 2f;
            softLimit.bounciness = 0f;
            softLimit.contactDistance = 0f;
            joint.linearLimit = softLimit;

            joint.xDrive = new JointDrive();
            joint.zDrive = new JointDrive();

            JointDrive yDrive = joint.yDrive;
            yDrive.positionSpring = _springForce;
            yDrive.positionDamper = _springDampening;
            yDrive.maximumForce = float.MaxValue;
            joint.yDrive = yDrive;

            joint.targetPosition = Vector3.down * buttonHeightLimit;
            joint.targetAngularVelocity = Vector3.zero;

            joint.configuredInWorldSpace = false;
            joint.swapBodies = false;
            joint.breakForce = float.PositiveInfinity;
            joint.breakTorque = float.PositiveInfinity;

            joint.enableCollision = false;
            joint.enablePreprocessing = true;
            joint.massScale = 1;
            joint.connectedMassScale = 1;
        }

        private void UpdateToggleJoint()
        {
            float heightLimit = _isToggled ? buttonToggleHeightLimit : buttonHeightLimit;

            SoftJointLimit softLimit = _buttonElement.Joint.linearLimit;
            softLimit.limit = heightLimit / 2f;
            _buttonElement.Joint.linearLimit = softLimit;

            _buttonElement.Joint.connectedAnchor = Vector3.up * heightLimit;
            _buttonElement.Joint.targetPosition = Vector3.down * heightLimit;
        }

        #endregion

        #region Public Functions

        public void ForceToggleState(bool toggled)
        {
            _isToggled = toggled;
            OnToggle?.Invoke(_isToggled);
            UpdateToggleJoint();
        }

        #endregion

        #region Gizmos

        protected virtual void OnDrawGizmosSelected()
        {
            if (transform != null)
            {
                Gizmos.color = Color.green;
                if (isToggleable)
                {
                    Gizmos.DrawLine(transform.position + (transform.up * buttonToggleHeightLimit), transform.position + (transform.up * buttonHeightLimit));
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(transform.position, transform.position + (transform.up * buttonToggleHeightLimit));
                }
                else
                {
                    Gizmos.DrawLine(transform.position, transform.position + (transform.up * buttonHeightLimit));
                }
            }
        }

        #endregion

        #region Unity Editor

        private void OnValidate()
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
                if (_rigidbody == null)
                {
                    _rigidbody = gameObject.AddComponent<Rigidbody>();
                }
            }

            if (_buttonElement == null)
            {
                _buttonElement = GetComponentInChildren<PhysicsButtonElement>(true);
            }

            if (buttonToggleHeightLimit > buttonHeightLimit)
            {
                buttonHeightLimit = buttonToggleHeightLimit;
            }

            if (buttonHeightLimit < buttonToggleHeightLimit)
            {
                buttonToggleHeightLimit = buttonHeightLimit;
            }

            if (_buttonElement != null)
            {
                _buttonElement.transform.localPosition = new Vector3(0, buttonHeightLimit, 0);
                SetupButton();
            }
        }
        #endregion
    }
}