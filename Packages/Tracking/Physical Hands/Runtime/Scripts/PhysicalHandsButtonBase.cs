using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.PhysicalHands
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicalHandsButtonBase : MonoBehaviour
    {
        [Header("Base Button")]
        [SerializeField] 
        private GameObject _pressableObject;
        [SerializeField] 
        private float _buttonTravelDistance = 0.017f;
        [SerializeField] 
        private bool _shouldOnlyBePressedByHand = false;
        [SerializeField] 
        private ChiralitySelection _whichHandCanPressButton = ChiralitySelection.BOTH;
        [SerializeField] 
        private Vector3 _initialButtonPosition = Vector3.zero;

        private const float BUTTON_PRESS_EXIT_THRESHOLD = 0.09f;
        private bool _isButtonPressed = false;
        private bool _contactHandPressing = false;
        private bool _leftHandContacting = false;
        private bool _rightHandContacting = false;
        private ConfigurableJoint _configurableJoint;

        public UnityEvent OnButtonPressed;
        public UnityEvent OnButtonUnPressed;
        public UnityEvent<ContactHand> OnHandContact;
        public UnityEvent<ContactHand> OnHandContactExit;
        public UnityEvent<ContactHand> OnHandHover;
        public UnityEvent<ContactHand> OnHandHoverExit;

        private void OnEnable()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_pressableObject == null)
            {
                Debug.LogError("Pressable object not assigned. Please assign one to use the button.");
                enabled = false;
                return;
            }

            SetUpPressableObject();
            SetUpSpringJoint();
            _initialButtonPosition = _pressableObject.transform.localPosition;
        }

        private void SetUpPressableObject()
        {
            if (!_pressableObject.TryGetComponent<Rigidbody>(out Rigidbody rigidbody))
            {
                rigidbody = _pressableObject.AddComponent<Rigidbody>();
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
            }

            if (!_pressableObject.TryGetComponent<IgnorePhysicalHands>(out IgnorePhysicalHands ignorePhysHands))
            {
                ignorePhysHands = _pressableObject.AddComponent<IgnorePhysicalHands>();
                ignorePhysHands.DisableAllGrabbing = true;
                ignorePhysHands.DisableAllHandCollisions = false;
                ignorePhysHands.DisableCollisionOnChildObjects = false;
            }

            if (!_pressableObject.TryGetComponent<PhysicalHandsButtonHelper>(out PhysicalHandsButtonHelper buttonHelper))
            {
                buttonHelper = _pressableObject.AddComponent<PhysicalHandsButtonHelper>();
            }

            buttonHelper._onHandContact += OnHandContactPO;
            buttonHelper._onHandContactExit += OnHandContactExitPO;
            buttonHelper._onHandHover += OnHandHoverPO;
            buttonHelper._onHandHoverExit += OnHandHoverExitPO;
        }

        private void SetUpSpringJoint()
        {
            if (!_pressableObject.TryGetComponent<ConfigurableJoint>(out _configurableJoint))
            {
                _configurableJoint = _pressableObject.AddComponent<ConfigurableJoint>();
            }

            _configurableJoint.connectedBody = GetComponent<Rigidbody>();

            _configurableJoint.yDrive = new JointDrive
            {
                positionSpring = 100,
                positionDamper = 10,
                maximumForce = 1
            };

            _configurableJoint.xMotion = ConfigurableJointMotion.Locked;
            _configurableJoint.yMotion = ConfigurableJointMotion.Limited;
            _configurableJoint.zMotion = ConfigurableJointMotion.Locked;
            _configurableJoint.angularXMotion = ConfigurableJointMotion.Locked;
            _configurableJoint.angularYMotion = ConfigurableJointMotion.Locked;
            _configurableJoint.angularZMotion = ConfigurableJointMotion.Locked;


            _configurableJoint.anchor = new Vector3(
                _configurableJoint.anchor.x,
                _configurableJoint.anchor.y + (_buttonTravelDistance / 2),
                _configurableJoint.anchor.z);

            _configurableJoint.linearLimit = new SoftJointLimit
            {
                limit = _buttonTravelDistance / 2
            };
        }

        private void FixedUpdate()
        {
            float distance = Mathf.Abs(transform.position.y - _initialButtonPosition.y);

            if (!_isButtonPressed && distance >= _buttonTravelDistance && (_contactHandPressing || !_shouldOnlyBePressedByHand))
            {
                _isButtonPressed = true;
                ButtonPressed();
            }

            if (_isButtonPressed && distance < _buttonTravelDistance * BUTTON_PRESS_EXIT_THRESHOLD)
            {
                _isButtonPressed = false;
                ButtonUnpressed();
            }
        }

        protected virtual void ButtonPressed()
        {
            OnButtonPressed?.Invoke();
        }

        protected virtual void ButtonUnpressed()
        {
            OnButtonUnPressed?.Invoke();
        }

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

        public void OnHandContactExitPO(ContactHand hand)
        {
            OnHandContactExit?.Invoke(hand);

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
                _leftHandContacting = false;
                _rightHandContacting = false;
            }

            _contactHandPressing = GetChosenHandInContact();
        }

        private void OnHandHoverExitPO(ContactHand hand)
        {
            OnHandHoverExit?.Invoke(hand);
        }

        private void OnHandHoverPO(ContactHand hand)
        {
            OnHandHover?.Invoke(hand);
        }

        private bool GetChosenHandInContact()
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
    }
}