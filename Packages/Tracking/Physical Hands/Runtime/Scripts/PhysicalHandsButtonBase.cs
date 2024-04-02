using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.PhysicalHands
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicalHandsButtonBase : MonoBehaviour
    {
        
        [SerializeField, Tooltip("The GameObject representing the button that can be pressed.")] 
        protected GameObject _pressableObject;
        [SerializeField, Tooltip("The distance the button can travel when pressed. " +
            "\n If left a 0, the travel distance will be the y distance from the parent object.")] 
        private float _buttonTravelDistance = 0f;
        [SerializeField, Tooltip("Determines whether the button should only be pressed by a hand.")] 
        protected bool _shouldOnlyBePressedByHand = false;
        [SerializeField, Tooltip("Specifies which hand(s) can press the button.")] 
        protected ChiralitySelection _whichHandCanPressButton = ChiralitySelection.BOTH;
        [SerializeField, Tooltip("Freeze button travel when base object is moving.")]
        private bool _freezeButtonTravelOnMovement = true;
        [SerializeField, Tooltip("How fast can the button move before we consider it moving for the sake of freezing button travel on movement")]
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

        [Space(20)]
        [Header("Button Events")]
        [Space(3)]
        [Header("Button Press")]
        [Space(3)]
        public UnityEvent OnButtonPressed;
        public UnityEvent OnButtonUnPressed;
        [Header("Button Contact")]
        [Space(3)]
        public UnityEvent<ContactHand> OnHandContact;
        public UnityEvent<ContactHand> OnHandContactExit;
        [Header("Button Hover")]
        [Space(3)]
        public UnityEvent<ContactHand> OnHandHover;
        public UnityEvent<ContactHand> OnHandHoverExit;

        private void OnEnable()
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

            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.isKinematic = true;
            _rigidbody.constraints = RigidbodyConstraints.FreezeAll;

            // Set default travel distance if one is not set
            if (_buttonTravelDistance == 0)
            {
                // leave 1mm so the button does not clip into the base obect (assuming same thickness)
                _buttonTravelDistance = Mathf.Abs(_pressableObject.transform.localPosition.y) - 0.001f; 
            }

            SetUpPressableObject();
            SetUpSpringJoint();
            _initialButtonPosition = _pressableObject.transform.localPosition;
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
            _configurableJoint.anchor = new Vector3(
                _configurableJoint.anchor.x,
                _configurableJoint.anchor.y - (_buttonTravelDistance / 2),
                _configurableJoint.anchor.z);

            _configurableJoint.connectedAnchor = new Vector3(
                _configurableJoint.connectedAnchor.x,
                _configurableJoint.connectedAnchor.y - (_buttonTravelDistance / 2),
                _configurableJoint.connectedAnchor.z);

            // Set linear limit for button travel
            _configurableJoint.linearLimit = new SoftJointLimit
            {
                limit = _buttonTravelDistance
            };

            _configurableJoint.targetPosition = _initialButtonPosition;
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
            if (!_isButtonPressed && distance >= _buttonTravelDistance &&
                (_contactHandPressing || (!_shouldOnlyBePressedByHand && _objectsContactingButton.Count > 0)))
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
        public void OnHandContactPO(ContactHand hand)
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
        public void OnHandContactExitPO(ContactHand hand)
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
        private void OnHandHoverExitPO(ContactHand hand)
        {
            // Invoke event for hand hover exit
            OnHandHoverExit?.Invoke(hand);
        }

        /// <summary>
        /// Handles actions when a hand hovers over the pressable object.
        /// </summary>
        /// <param name="hand">The hand hovering.</param>
        private void OnHandHoverPO(ContactHand hand)
        {
            // Invoke event for hand hover
            OnHandHover?.Invoke(hand);
        }

        protected void OnCollisionPO(Collision collision)
        {
            if (!collision.transform.root.GetComponent<PhysicalHandsManager>())
            {
                _objectsContactingButton.Add(collision.gameObject);
            }
        }

        protected void OnCollisionExitPO(Collision collision)
        {
            _objectsContactingButton.Remove(collision.gameObject);
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