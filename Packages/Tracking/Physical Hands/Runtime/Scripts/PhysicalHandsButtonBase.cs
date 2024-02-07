using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.PhysicalHands
{
    public class PhysicalHandsButtonBase: MonoBehaviour, IPhysicalHandContact
    {
        internal const float BUTTON_PRESS_THRESHOLD = 0.01F;
        internal const float BUTTON_PRESS_EXIT_THRESHOLD = 0.09F;

        [Space(10)]
        [Header("Base Button")]
        [Space(5)]
        //[SerializeField, Tooltip("The pressable part of the button.")]
        //internal GameObject pressableObject;
        [Tooltip("Travel of the button (How far does the button need to move before it is activated).")]
        internal float buttonTravelDistance = 0.02f;

        [SerializeField, Tooltip("Can this button only be activated by pressing it with a hand?")]
        internal bool _shouldOnlyBePressedByHand = false;
        [SerializeField, Tooltip("Which hand should be able to press this button?")]
        ChiralitySelection _whichHandCanPressButton = ChiralitySelection.BOTH;

        internal bool _isButtonPressed = false;

        internal bool _contactHandPressing = false;
        internal List<Collider> _colliders;
        internal Transform _parent;
        internal Joint _joint;

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
            _initialButtonPosition = this.transform.localPosition;
        }

        void FixedUpdate()
        {
            if((!_isButtonPressed
                && Vector3.Distance(this.transform.localPosition, _initialButtonPosition) > buttonTravelDistance * BUTTON_PRESS_THRESHOLD)
                && (_contactHandPressing || !_shouldOnlyBePressedByHand))
            {
                _isButtonPressed = true;
                ButtonPressed();
            }

            if (_isButtonPressed 
                && Vector3.Distance(this.transform.localPosition, _initialButtonPosition) < buttonTravelDistance * BUTTON_PRESS_EXIT_THRESHOLD)
            {
                _isButtonPressed = false;
                ButtonUnpressed();
            }
        }

        protected virtual void ButtonPressed()
        {
            OnButtonPressed?.Invoke();
            Debug.Log("Button Pressed");
        }

        protected virtual void ButtonUnpressed()
        {
            OnButtonUnPressed?.Invoke(); 
            Debug.Log("Button Unpressed");
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
