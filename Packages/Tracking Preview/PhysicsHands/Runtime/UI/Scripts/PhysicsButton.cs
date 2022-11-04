/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;
using UnityEngine.Events;

// Mostly ported from IE button
namespace Leap.Unity.Interaction.PhysicsHands
{
    ///<summary>
    /// A physics-enabled button. Activated by physically pressing the button, with events
    /// for hover, contact, press and all relevant opposites.
    ///</summary>
    public class PhysicsButton : MonoBehaviour
    {

        #region Inspector

        [SerializeField]
        private Rigidbody _rigidbody = null;

        [SerializeField]
        private bool _controlEnabled = true;
        private bool _controlChangeCalled = true;
        public bool controlEnabled
        {
            get { return _controlEnabled; }
            set { if (_controlEnabled == value) return; _controlChangeCalled = true; _controlEnabled = value; }
        }

        [Header("Motion Configuration")]
        /// <summary>
        /// The minimum and maximum heights the button can exist at.
        /// </summary>
        [Tooltip("The minimum and maximum heights the button can exist at.")]
        public Vector2 minMaxHeight = new Vector2(0f, 0.02f);

        /// <summary>
        /// The height that this button rests at; this value is a lerp in between the min and
        /// max height.
        /// </summary>
        [Tooltip("The height that this button rests at; this value is a lerp in between the min and max height.")]
        [Range(0f, 1f)]
        public float restingHeight = 0.5f;

        /// <summary>
        /// The spring force appied to the button to return it to its resting height.
        /// </summary>
        [Range(0, 1)]
        [SerializeField]
        private float _springForce = 0.1f;
        public float springForce
        {
            get
            {
                return _springForce;
            }
            set
            {
                _springForce = value;
            }
        }

        #endregion

        #region Events

        [SerializeField, Header("Events")]
        private UnityEvent _OnHover = new UnityEvent();

        [SerializeField]
        private UnityEvent _OnUnhover = new UnityEvent();

        [SerializeField]
        private UnityEvent _OnContact = new UnityEvent();

        [SerializeField]
        private UnityEvent _OnUncontact = new UnityEvent();

        [SerializeField]
        private UnityEvent _OnPress = new UnityEvent();

        [SerializeField]
        private UnityEvent _OnUnpress = new UnityEvent();

        [SerializeField]
        private UnityEvent<float> _OnPressedAmountChanged = new UnityEvent<float>();

        public Action OnPress = () => { };
        public Action OnUnpress = () => { };

        public Action OnHover = () => { };
        public Action OnUnhover = () => { };

        public Action OnContact = () => { };
        public Action OnUncontact = () => { };

        public Action<float> OnPressedAmountChanged = (value) => { };

        #endregion

        #region State

        protected bool _isPressed = false;
        /// <summary>Gets whether the button is currently held down.</summary>
        public bool isPressed => _isPressed;

        private bool _hovered = false;
        /// <summary>Gets whether the button is currently being hovered.</summary>
        public bool isHovered => _hovered;

        private bool _contact = false;
        /// <summary>Gets whether the button is currently being touched.</summary>
        public bool isContacted => _contact;
        // This gets set when contact happens to prevent rogue instantaneous taps
        private int _contactReleaseTime = 0;

        protected bool _pressedThisFrame = false;
        /// <summary>Gets whether the button was pressed during this Update frame.</summary>
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
            set
            {
                if (value != _pressedAmount)
                {
                    _pressedAmount = value;
                    OnPressedAmountChanged?.Invoke(value);
                }
            }
            get { return _pressedAmount; }
        }

        /// <summary>
        /// The physical position of this element in local space; may diverge from the
        /// graphical position.
        /// </summary>
        protected Vector3 localPhysicsPosition;

        /// <summary>
        /// The physical position of this element in local space, constrained on the local z axis 
        /// by the minMaxHeight of the button.
        /// </summary>
        protected Vector3 localPhysicsPositionConstrained;

        /// <summary>
        /// The physical position of this element in world space; may diverge from the
        /// graphical position.
        /// </summary>
        protected Vector3 physicsPosition = Vector3.zero;

        /// <summary>
        /// Returns the local position of this button when it is able to relax into its target
        /// position.
        /// </summary>
        public virtual Vector3 RelaxedLocalPosition
        {
            get
            {
                return new Vector3(0, RelaxedLocalY, 0);
            }
        }

        public float RelaxedLocalY
        {
            get
            {
                return Mathf.Lerp(minMaxHeight.x, minMaxHeight.y, restingHeight);
            }
        }

        private PhysicsProvider _provider;

        private int _originalLayer = -1;

        private Vector3 _localDepressorPosition;
        private Vector3 _physicsVelocity = Vector3.zero;
        private bool _physicsOccurred;

        [SerializeField, HideInInspector]
        private PhysicsButtonElement _buttonElement = null;
        private PhysicsHand _currentHand = null;
        private int _currentFinger = -1;

        #endregion

        #region Unity Events

        void Reset()
        {
            _rigidbody = GetComponentInChildren<Rigidbody>(true);
            if (_rigidbody != null)
            {
                SetupRigidbody();
            }
        }

        private void Awake()
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponentInChildren<Rigidbody>(true);
            }
        }

        protected void OnDisable()
        {
            if (isPressed)
            {
                _unpressedThisFrame = true;
                OnUnpress?.Invoke();
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
            if (!_rigidbody.transform.IsChildOf(transform))
            {
                Debug.LogError("The assigned rigidbody is not a child of the PhysicsButton. The PhysicsButton has been disabled.", this);
                enabled = false;
            }

            SetupRigidbody();

            localPhysicsPosition = _rigidbody.transform.localPosition;
            localPhysicsPositionConstrained = localPhysicsPosition;
            physicsPosition = transform.position;

            _originalLayer = _rigidbody.gameObject.layer;

            _provider = FindObjectOfType<PhysicsProvider>(true);

            OnPress += _OnPress.Invoke;
            OnUnpress += _OnUnpress.Invoke;
            OnHover += _OnHover.Invoke;
            OnUnhover += _OnUnhover.Invoke;
            OnContact += _OnContact.Invoke;
            OnUncontact += _OnUncontact.Invoke;
            OnPressedAmountChanged += _OnPressedAmountChanged.Invoke;
        }

        protected void FixedUpdate()
        {
            if (!_physicsOccurred)
            {
                _physicsOccurred = true;

                if (_provider != null)
                {
                    ProcessPhysicsEvents();
                }

                if (!_rigidbody.IsSleeping())
                {

                    float localPhysicsDisplacementPercentage
                      = Mathf.InverseLerp(minMaxHeight.x, minMaxHeight.y, localPhysicsPosition.y);

                    // Sleep the rigidbody if it's not really moving.
                    if (_rigidbody.position == physicsPosition
                        && _physicsVelocity == Vector3.zero
                        && Mathf.Abs(localPhysicsDisplacementPercentage - restingHeight) < 0.01F)
                    {
                        _rigidbody.Sleep();
                    }
                    else
                    {
                        // Otherwise reset the body's position to where it was last time PhysX
                        // looked at it.
                        if (_physicsVelocity.ContainsNaN())
                        {
                            _physicsVelocity = Vector3.zero;
                        }

                        _rigidbody.position = transform.TransformPoint(localPhysicsPositionConstrained);
                        _rigidbody.velocity = _physicsVelocity;
                    }
                }
            }
        }

        private void ProcessPhysicsEvents()
        {
            if (_provider.IsObjectHovered(_rigidbody))
            {
                if (!_hovered)
                {
                    _hovered = true;
                    OnHover?.Invoke();
                }

                if (_provider.GetObjectState(_rigidbody, out var state))
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
        }

        private const float FRICTION_COEFFICIENT = 30F;
        private const float DRAG_COEFFICIENT = 60F;
        protected void Update()
        {

            // Reset our convenience state variables.
            _pressedThisFrame = false;
            _unpressedThisFrame = false;

            // Disable collision on this button if it is not the primary hover.
            if (_controlChangeCalled)
            {
                UpdatePhysicsLayers();
            }

            // Enforce local rotation (if button is child of non-kinematic rigidbody,
            // this is necessary).
            _rigidbody.transform.localRotation = Quaternion.identity;

            // Apply physical corrections only if PhysX has modified our positions.
            if (_physicsOccurred)
            {
                _physicsOccurred = false;

                // Record and enforce the sliding state from the previous frame.
                if (isPressed)
                {
                    localPhysicsPosition
                      = constrainDepressedLocalPosition(new Vector3(0, _rigidbody.transform.localPosition.y, 0));
                }
                else
                {
                    localPhysicsPosition = new Vector3(0, _rigidbody.transform.localPosition.y, 0);
                }

                // Calculate the physical kinematics of the button in local space
                Vector3 localPhysicsVelocity = transform.InverseTransformVector(_rigidbody.velocity);

                Vector3 tipPos = Vector3.zero, origTipPos = Vector3.zero, closestPoint = Vector3.zero;

                // We need to ensure that the tip position is still close to the button on release
                // If we don't then it suddenly pings back
                if (isPressed && _currentHand != null)
                {
                    tipPos = _currentHand.GetPhysicsHand().GetTipPosition(_currentFinger);
                    origTipPos = _rigidbody.transform.TransformPoint(_localDepressorPosition);
                    closestPoint = _rigidbody.ClosestPointOnBounds(tipPos);
                }

                if (isPressed && _currentHand != null && Vector3.Distance(closestPoint, tipPos) < 0.001f)
                {
                    Vector3 curLocalDepressorPos = transform.InverseTransformPoint(tipPos);
                    Vector3 origLocalDepressorPos = transform.InverseTransformPoint(origTipPos);
                    localPhysicsVelocity = Vector3.up * 0.0005f;
                    localPhysicsPosition = constrainDepressedLocalPosition(curLocalDepressorPos - origLocalDepressorPos);
                }
                else
                {
                    Vector3 originalLocalVelocity = localPhysicsVelocity;

                    // Spring force
                    localPhysicsVelocity +=
                      Mathf.Clamp(_springForce * 10000F
                                  * -(Mathf.Lerp(minMaxHeight.x, minMaxHeight.y, restingHeight)
                                     - localPhysicsPosition.y),
                                 -100f / transform.lossyScale.x,
                                  100f / transform.lossyScale.x)
                      * Time.fixedDeltaTime * Vector3.down;

                    // Friction & Drag
                    float velMag = originalLocalVelocity.magnitude;
                    var frictionDragVelocityChangeAmt = 0f;
                    if (velMag > 0F)
                    {
                        // Friction force
                        var frictionForceAmt = velMag * FRICTION_COEFFICIENT;
                        frictionDragVelocityChangeAmt
                          += Time.fixedDeltaTime * transform.lossyScale.x * frictionForceAmt;

                        // Drag force
                        float velSqrMag = velMag * velMag;
                        var dragForceAmt = velSqrMag * DRAG_COEFFICIENT;
                        frictionDragVelocityChangeAmt
                          += Time.fixedDeltaTime * transform.lossyScale.x * dragForceAmt;

                        // Apply velocity change, but don't let friction or drag let velocity
                        // magnitude cross zero.
                        var newVelMag = Mathf.Max(0, velMag - frictionDragVelocityChangeAmt);
                        localPhysicsVelocity = localPhysicsVelocity / velMag * newVelMag;
                    }

                }

                // Transform the local physics back into world space
                physicsPosition = transform.TransformPoint(localPhysicsPosition);
                _physicsVelocity = transform.TransformVector(localPhysicsVelocity);

                // Calculate the Depression State of the Button from its Physical Position
                // Set its Graphical Position to be Constrained Physically
                bool oldDepressed = isPressed;

                // Normalized depression amount.
                _pressedAmount = localPhysicsPosition.y.Map(minMaxHeight.x, RelaxedLocalY,
                  1F, 0F);

                // If the button is depressed past its limit...
                if (localPhysicsPosition.y < minMaxHeight.x)
                {
                    _rigidbody.transform.localPosition = new Vector3(0, minMaxHeight.x, 0);
                    if (_currentHand != null && _currentHand.GetLeapHand() != null)
                    {
                        _isPressed = true;
                    }
                    else
                    {
                        physicsPosition = transform.TransformPoint(new Vector3(0, minMaxHeight.x, 0));
                        _isPressed = false;
                        ClearDepressor();
                    }
                }
                // Else if the button is extended past its limit...
                else if (localPhysicsPosition.y > minMaxHeight.y)
                {
                    _rigidbody.transform.localPosition = new Vector3(0, minMaxHeight.y, 0);
                    physicsPosition = _rigidbody.position;
                    _isPressed = false;
                    ClearDepressor();
                }
                else
                {
                    // Else, just make the physical and graphical motion of the button match
                    _rigidbody.transform.localPosition = localPhysicsPosition;

                    // Allow some hysteresis before setting isDepressed to false.
                    if (!isPressed || (localPhysicsPosition.y < Mathf.Lerp(minMaxHeight.x, minMaxHeight.y, 0.1F)))
                    {
                        _isPressed = false;
                        ClearDepressor();
                    }
                }

                // If our depression state has changed since last time...
                if (isPressed && !oldDepressed)
                {
                    OnPress?.Invoke();
                    _pressedThisFrame = true;

                }
                else if (!isPressed && oldDepressed)
                {
                    _unpressedThisFrame = true;
                    OnUnpress?.Invoke();

                    ClearDepressor();
                }

                localPhysicsPositionConstrained = transform.InverseTransformPoint(physicsPosition);
            }
        }

        private void UpdatePhysicsLayers()
        {
            if (_provider == null)
            {
                return;
            }
            _rigidbody.gameObject.layer = _controlEnabled ? _originalLayer : _provider.NoContactLayers[0].layerIndex;
        }

        /// <summary>
        /// Clamps the input local-space position to the bounds allowed by this UI element,
        /// without clamping along the button depression axis. For buttons, this is locks the
        /// element in local-XZ space, but not along the pressing axis (Y axis).
        /// </summary>
        protected virtual Vector3 constrainDepressedLocalPosition(Vector3 localPosition)
        {
            // Buttons are only allowed to move along their Y axis.
            return new Vector3(
              0,
              localPhysicsPosition.y + localPosition.y,
              0);
        }

        // Try grabbing the offset between the fingertip and this object...
        public void TrySetDepressor(Collider collider)
        {
            if (collider.attachedArticulationBody != null && _currentHand == null && (localPhysicsPosition.y > minMaxHeight.x)
              && collider.TryGetComponent<PhysicsBone>(out var bone))
            {
                if (bone.Joint == 2)
                {
                    _currentHand = bone.Hand;
                    _currentFinger = bone.Finger;
                    _localDepressorPosition = _rigidbody.transform.InverseTransformPoint(_currentHand.GetPhysicsHand().GetTipPosition(_currentFinger));
                }
            }
        }

        private void ClearDepressor()
        {
            _currentHand = null;
            _currentFinger = -1;
        }

        private void SetupRigidbody()
        {
            _rigidbody.mass = 1e-07f;
            _rigidbody.drag = 0;
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = false;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        #endregion

        #region Gizmos

        protected virtual void OnDrawGizmosSelected()
        {
            if (transform != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Vector2 heights = minMaxHeight;
                Vector3 originPosition = Application.isPlaying ? Vector3.zero : _rigidbody.transform.localPosition;
                if (!Application.isPlaying)
                {
                    originPosition = _rigidbody.transform.localPosition + Vector3.down * RelaxedLocalY;
                }

                Gizmos.color = Color.red;
                Gizmos.DrawLine(originPosition + (Vector3.up * heights.x), originPosition + (Vector3.up * heights.y));
                Gizmos.color = Color.green;
                Gizmos.DrawLine(originPosition + (Vector3.up * heights.x), originPosition + (Vector3.up * Mathf.Lerp(heights.x, heights.y, restingHeight)));
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the minimum height (x component) of the minMaxHeight property. The minimum
        /// height can't be set larger than the maximum height with this method (it will be
        /// clamped if necessary).
        /// </summary>
        public void SetMinHeight(float minHeight)
        {
            minMaxHeight = new Vector2(Mathf.Min(minMaxHeight.y, minHeight), minMaxHeight.y);
        }

        /// <summary>
        /// Sets the maximum height (y component) of the minMaxHeight property. The maximum
        /// height can't be set smaller than the minimum height with this method (it will be
        /// clamped if necessary).
        /// </summary>
        public void SetMaxHeight(float maxHeight)
        {
            minMaxHeight = new Vector2(minMaxHeight.x, Mathf.Max(minMaxHeight.x, maxHeight));
        }

        #endregion

        #region Unity Editor

        private void OnValidate()
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponentInChildren<Rigidbody>(true);
            }

            if (_rigidbody != null)
            {
                _rigidbody.transform.localPosition = RelaxedLocalPosition;
                SetupRigidbody();
            }

            if (_buttonElement == null && _rigidbody != null)
            {
                _buttonElement = _rigidbody.GetComponent<PhysicsButtonElement>();
                if (_buttonElement == null)
                {
                    _buttonElement = _rigidbody.gameObject.AddComponent<PhysicsButtonElement>();
                }
            }
        }
        #endregion
    }
}