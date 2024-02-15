using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.PhysicalHands
{
    public class PhysicalHandsButtonBase: MonoBehaviour, IPhysicalHandContact
    {
        internal const float BUTTON_PRESS_EXIT_THRESHOLD = 0.09F;

        [Space(10)]
        [Header("Base Button")]
        [Space(5)]
        //[SerializeField, Tooltip("The pressable part of the button.")]
        //internal GameObject pressableObject;
        [SerializeField, Tooltip("Travel of the button (How far does the button need to move before it is activated).")]
        internal float buttonTravelDistance = 0.017f;

        [SerializeField, Tooltip("Can this button only be activated by pressing it with a hand?")]
        internal bool _shouldOnlyBePressedByHand = false;
        [SerializeField, Tooltip("Which hand should be able to press this button?")]
        ChiralitySelection _whichHandCanPressButton = ChiralitySelection.BOTH;

        internal bool _isButtonPressed = true;

        internal bool _contactHandPressing = false;
        internal List<Collider> _colliders;
        internal Transform _parent;
        internal Joint _joint;

        [SerializeField, Tooltip("This should be the starting position i.e. the position the button sits when fully un-pressed. \n" +
            "If left at (0,0,0), this will default to getting the current button position on awake. \n" +
            "This is the position where distance checks for button travel distance are performed from. \n")]
        internal Vector3 _initialButtonPosition = Vector3.zero;

        [Space(10)]
        public UnityEvent OnButtonPressed;
        public UnityEvent OnButtonUnPressed;


        internal bool _leftHandContacting = false;
        internal bool _rightHandContacting = false;



        private void Awake()
        {
            _colliders = this.transform.GetComponentsInChildren<Collider>().ToList();
            _parent = this.transform.parent;
            _joint = this.transform.GetComponent<Joint>();
            if (_initialButtonPosition == Vector3.zero)
            {
                _initialButtonPosition = this.transform.localPosition;
            }
            Debug.Log(_initialButtonPosition);
        }

        void FixedUpdate()
        {

            if ((!_isButtonPressed
                && Vector3.Distance(this.transform.localPosition, _initialButtonPosition) > buttonTravelDistance)
                && (_contactHandPressing || !_shouldOnlyBePressedByHand))
            {
                //Debug.Log("ButtonPressed"+ Vector3.Distance(this.transform.localPosition, _initialButtonPosition));
                _isButtonPressed = true;
                ButtonPressed();
            }

            if (_isButtonPressed 
                && Vector3.Distance(this.transform.localPosition, _initialButtonPosition) < buttonTravelDistance * BUTTON_PRESS_EXIT_THRESHOLD)
            {
                //Debug.Log("ButtonUnPressed" + Vector3.Distance(this.transform.localPosition, _initialButtonPosition));
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

        public void OnHandContact(ContactHand hand)
        {
            ContactHandNearbyEnter(hand);
        }

        public void ContactHandNearbyEnter(ContactHand contactHand)
        {
            if (contactHand != null)
            {
                if (contactHand.Handedness == Chirality.Left)
                {
                    _leftHandContacting = true;
                }
                else if (contactHand.Handedness == Chirality.Right)
                {
                    _rightHandContacting = true;
                }
            }

            _contactHandPressing = GetChosenHandInContact();
        }


        public void OnHandContactExit(ContactHand hand)
        {
            ContactHandNearbyExit(hand);
        }

        public void ContactHandNearbyExit(ContactHand contactHand)
        {
            if (contactHand != null)
            {
                if (contactHand.Handedness == Chirality.Left)
                {
                    _leftHandContacting = false;
                }
                else if (contactHand.Handedness == Chirality.Right)
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

        private bool GetChosenHandInContact()
        {
            switch (_whichHandCanPressButton)
            {
                case ChiralitySelection.LEFT:
                    if (_leftHandContacting)
                    {
                        return true;
                    }
                    break;
                case ChiralitySelection.RIGHT:
                    if (_rightHandContacting)
                    {
                        return true;
                    }
                    break;
                case ChiralitySelection.BOTH:
                    if (_rightHandContacting || _leftHandContacting)
                    {
                        return true;
                    }
                    break;
            }

            return false;
        }
    }
}
