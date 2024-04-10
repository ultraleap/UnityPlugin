/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/


using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.PhysicalHands
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicalHandsButtonBase : MonoBehaviour
    {
        
        [SerializeField] 
        protected GameObject _pressableObject;
        [SerializeField]
        bool _automaticTravelDistance = true;
        [SerializeField]
        private float _buttonTravelDistance = 0f;
        [SerializeField]
        bool _automaticOffsetDistance = true;
        [SerializeField]
        private float _buttonTravelOffset = 0.001f;
        [SerializeField] 
        protected bool _canBePressedByObjects = false;
        [SerializeField] 
        protected ChiralitySelection _whichHandCanPressButton = ChiralitySelection.BOTH;
        [SerializeField]
        private bool _freezeButtonTravelOnMovement = true;
        [SerializeField]
        private Vector3 _buttonVelocityThreshold = new Vector3(0.1f, 0.1f, 0.1f);

        private const float BUTTON_PRESS_EXIT_THRESHOLD = 0.5f;
        private bool _isButtonPressed = false;
        private bool _contactHandPressing = false;
        private bool _leftHandContacting = false;
        private bool _rightHandContacting = false;
        private Rigidbody _pressableObjectRB = null;
        private Rigidbody _rigidbody = null;
        private ConfigurableJoint _configurableJoint;
        private Vector3 _initialButtonPosition = Vector3.zero;
        private List<GameObject> _objectsContactingButton = new List<GameObject>();

        protected PhysicalHandsButtonHelper _buttonHelper;

        [SerializeField]
        public UnityEvent OnButtonPressed;
        [SerializeField]
        public UnityEvent OnButtonUnPressed;
        [SerializeField]
        public UnityEvent<ContactHand> OnHandContact;
        [SerializeField]
        public UnityEvent<ContactHand> OnHandContactExit;
        [SerializeField]
        public UnityEvent<ContactHand> OnHandHover;
        [SerializeField]
        public UnityEvent<ContactHand> OnHandHoverExit;


        #region public getters
        public bool IsPressed
        {
            get
            {
                return _isButtonPressed;
            }
        }

        #endregion




        public void UpdateDistanceValues()
        {
            if (_automaticOffsetDistance)
            {
                if (GetComponent<MeshFilter>())
                {
                    _buttonTravelOffset = GetComponent<MeshFilter>().sharedMesh.bounds.extents.y;
                }
#if UNITY_EDITOR
                else if (UltraleapSettings.Instance.showPhysicalHandsButtonOffsetWarning == true)
                {
                    if (EditorUtility.DisplayDialog("Could not automatically set offset distance.",
                        "Could not automatically set offset distance. \n \n Please either add mesh filters to both pressable object and physical hands button or manually set the offset.",
                        "Do Not Show Again", "Okay"))
                    {
                        UltraleapSettings.Instance.showPhysicalHandsButtonOffsetWarning = false;
                    }
                    _automaticOffsetDistance = false;
                }
                else
                {
                    _automaticOffsetDistance = false;
                }
#endif
            }
            if (_automaticTravelDistance)
            {
                // leave 1mm so the button does not clip into the base obect (assuming same thickness)
                _buttonTravelDistance = Mathf.Abs(_pressableObject.transform.localPosition.y) - (_buttonTravelOffset);
            }
        }


        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize the button.
        /// </summary>
        private void Initialize()
        {

            if (_pressableObject == null)
            {
                Debug.LogError("Pressable object not assigned. Please assign one to use the button.");
                enabled = false;
                return;
            }

            _initialButtonPosition = _pressableObject.transform.localPosition;

            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.isKinematic = true;
            _rigidbody.constraints = RigidbodyConstraints.FreezeAll;

            UpdateDistanceValues();

            SetUpPressableObject();
            SetUpSpringJoint();
            
        }

        /// <summary>
        /// Set up the pressable object.
        /// </summary>
        private void SetUpPressableObject()
        {
            // Ensure the pressable object has a Rigidbody component
            if (!_pressableObject.TryGetComponent<Rigidbody>(out _pressableObjectRB))
            {
                _pressableObjectRB = _pressableObject.AddComponent<Rigidbody>();
                _pressableObjectRB.useGravity = false;
            }



            // Ensure the pressable object has an IgnorePhysicalHands component
            if (!_pressableObject.TryGetComponent<IgnorePhysicalHands>(out IgnorePhysicalHands ignorePhysHands))
            {
                ignorePhysHands = _pressableObject.AddComponent<IgnorePhysicalHands>();
                ignorePhysHands.DisableAllGrabbing = true;
                ignorePhysHands.DisableAllHandCollisions = false;
                ignorePhysHands.DisableCollisionOnChildObjects = false;
            }

            // Ensure the pressable object has a PhysicalHandsButtonHelper component
            if (!_pressableObject.TryGetComponent<PhysicalHandsButtonHelper>(out _buttonHelper))
            {
                _buttonHelper = _pressableObject.AddComponent<PhysicalHandsButtonHelper>();
            }

            // Subscribe to events from the button helper
            _buttonHelper._onHandContact -= OnHandContactPO;
            _buttonHelper._onHandContactExit -= OnHandContactExitPO;
            _buttonHelper._onHandHover -= OnHandHoverPO;
            _buttonHelper._onHandHoverExit -= OnHandHoverExitPO;
            _buttonHelper._onCollisionEnter -= OnCollisionPO;
            _buttonHelper._onCollisionExit -= OnCollisionExitPO;

            _buttonHelper._onHandContact += OnHandContactPO;
            _buttonHelper._onHandContactExit += OnHandContactExitPO;
            _buttonHelper._onHandHover += OnHandHoverPO;
            _buttonHelper._onHandHoverExit += OnHandHoverExitPO;
            _buttonHelper._onCollisionEnter += OnCollisionPO;
            _buttonHelper._onCollisionExit += OnCollisionExitPO;
        }

        /// <summary>
        /// Set up the spring joint for the button.
        /// </summary>
        private void SetUpSpringJoint()
        {
            if (!_pressableObject.TryGetComponent<ConfigurableJoint>(out _configurableJoint))
            {
                _configurableJoint = _pressableObject.AddComponent<ConfigurableJoint>();
            }

            _configurableJoint.targetPosition = -_initialButtonPosition;

            // Connect the button to the parent object with a spring joint
            _configurableJoint.connectedBody = _rigidbody;

            // Configure spring parameters
            _configurableJoint.yDrive = new JointDrive
            {
                positionSpring = 100,
                positionDamper = 10,
                maximumForce = 1
            };

            // Lock and limit motion axes
            _configurableJoint.xMotion = ConfigurableJointMotion.Locked;
            _configurableJoint.yMotion = ConfigurableJointMotion.Limited;
            _configurableJoint.zMotion = ConfigurableJointMotion.Locked;
            _configurableJoint.angularXMotion = ConfigurableJointMotion.Locked;
            _configurableJoint.angularYMotion = ConfigurableJointMotion.Locked;
            _configurableJoint.angularZMotion = ConfigurableJointMotion.Locked;

            // Adjust anchor position for button travel distance
            _configurableJoint.anchor = Vector3.zero;
            _configurableJoint.autoConfigureConnectedAnchor = false;
            _configurableJoint.connectedAnchor = new Vector3(0,_initialButtonPosition.y - _buttonTravelOffset,0);

            // Set linear limit for button travel
            _configurableJoint.linearLimit = new SoftJointLimit
            {
                limit = (float)((_buttonTravelDistance )* transform.localScale.y) - (_buttonTravelOffset * transform.localScale.y)
            };
        }

        private void FixedUpdate()
        {
            if(_rigidbody.velocity.sqrMagnitude > _buttonVelocityThreshold.sqrMagnitude)
            {
                FreezePressableMovement();
            }
            else
            {
                UnFreezePressableMovement();
            }

            float distance = Mathf.Abs(_pressableObject.transform.localPosition.y - _initialButtonPosition.y);

            // Check if the button should be pressed
            if (!_isButtonPressed && distance >= (_buttonTravelDistance - (_buttonTravelOffset) - 0.001f) &&
                (_contactHandPressing || (_canBePressedByObjects && _objectsContactingButton.Count > 0)))
            {
                _isButtonPressed = true;
                ButtonPressed();
            }

            // Check if the button should be released
            if (_isButtonPressed && distance < _buttonTravelDistance * BUTTON_PRESS_EXIT_THRESHOLD)
            {
                _isButtonPressed = false;
                ButtonUnpressed();
            }
        }

        /// <summary>
        /// Invoke the button pressed event.
        /// </summary>
        protected virtual void ButtonPressed()
        {
            OnButtonPressed?.Invoke();
        }

        /// <summary>
        /// Invoke the button released event.
        /// </summary>
        protected virtual void ButtonUnpressed()
        {
            OnButtonUnPressed?.Invoke();
        }

        /// <summary>
        /// Handle hand contact with the pressable object.
        /// </summary>
        public virtual void OnHandContactPO(ContactHand hand)
        {
            OnHandContact?.Invoke(hand);

            if (hand != null)
            {
                if (hand.Handedness == Chirality.Left)
                {
                    _leftHandContacting = true;
                }
                else if (hand.Handedness == Chirality.Right)
                {
                    _rightHandContacting = true;
                }
            }

            _contactHandPressing = GetChosenHandInContact();
        }

        /// <summary>
        /// Handles actions when a hand exits contact with the pressable object.
        /// </summary>
        /// <param name="hand">The hand exiting contact.</param>
        public virtual void OnHandContactExitPO(ContactHand hand)
        {
            // Invoke event for hand contact exit
            OnHandContactExit?.Invoke(hand);

            // Update hand contact flags
            if (hand != null)
            {
                if (hand.Handedness == Chirality.Left)
                {
                    _leftHandContacting = false;
                }
                else if (hand.Handedness == Chirality.Right)
                {
                    _rightHandContacting = false;
                }
            }
            else
            {
                // Reset hand contact flags if hand is null
                _leftHandContacting = false;
                _rightHandContacting = false;
            }

            // Update overall hand contact flag
            _contactHandPressing = GetChosenHandInContact();
        }

        /// <summary>
        /// Handles actions when a hand exits hovering over the pressable object.
        /// </summary>
        /// <param name="hand">The hand exiting hover.</param>
        protected virtual void OnHandHoverExitPO(ContactHand hand)
        {
            // Invoke event for hand hover exit
            OnHandHoverExit?.Invoke(hand);
        }

        /// <summary>
        /// Handles actions when a hand hovers over the pressable object.
        /// </summary>
        /// <param name="hand">The hand hovering.</param>
        protected virtual void OnHandHoverPO(ContactHand hand)
        {
            // Invoke event for hand hover
            OnHandHover?.Invoke(hand);
        }

        protected virtual void OnCollisionPO(Collision collision)
        {
            if (!collision.transform.root.GetComponent<PhysicalHandsManager>())
            {
                _objectsContactingButton.Add(collision.gameObject);
            }
        }

        protected virtual void OnCollisionExitPO(Collision collision)
        {
            _objectsContactingButton.Remove(collision.gameObject);
        }

        protected virtual void OnCollisionPO(ContactHand contactHand)
        {
        }

        protected virtual void OnCollisionExitPO(ContactHand contactHand)
        {
        }

        /// <summary>
        /// Determines whether any chosen hand is in contact with the pressable object.
        /// </summary>
        /// <returns>True if a chosen hand is in contact; otherwise, false.</returns>
        protected bool GetChosenHandInContact()
        {
            switch (_whichHandCanPressButton)
            {
                case ChiralitySelection.LEFT:
                    return _leftHandContacting;
                case ChiralitySelection.RIGHT:
                    return _rightHandContacting;
                case ChiralitySelection.BOTH:
                    return _rightHandContacting || _leftHandContacting;
                default:
                    return false;
            }
        }

        public void FreezePressableMovement()
        {
            if (_pressableObjectRB.constraints != RigidbodyConstraints.FreezeAll)
            {
                _pressableObjectRB.constraints = RigidbodyConstraints.FreezeAll;
            }
        }

        public void UnFreezePressableMovement()
        {
            if (_pressableObjectRB.constraints != RigidbodyConstraints.None)
            {
                _pressableObjectRB.constraints = RigidbodyConstraints.None;
            }
        }
    }
}