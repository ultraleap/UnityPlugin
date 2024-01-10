using Leap.Unity.PhysicalHands;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Leap.Unity;

public class PhysicalHandsButton : MonoBehaviour
{
    internal const float BUTTON_PRESS_THRESHOLD = 0.01F;
    internal const float BUTTON_PRESS_EXIT_THRESHOLD = 0.09F;

    [SerializeField]
    internal GameObject buttonObject;
    [Tooltip("The local position which the button will be limited to and will try to return to.")]
    internal float buttonHeightLimit = 0.02f;


    [SerializeField]
    internal bool _buttonShouldDelayRebound = false;
    [SerializeField]
    internal float _buttonStaydownTimer = 2;
    [SerializeField]
    internal bool _shouldOnlyBePressedByHand = false;
    [SerializeField]
    ChiralitySelection _whichHandCanPressButton = ChiralitySelection.BOTH;

    internal bool _isButtonPressed = false;

    internal bool _contactHandPressing = false;
    internal List<Collider> _colliders;

    [Space(10)]
    internal UnityEvent OnButtonPressed;
    internal UnityEvent OnButtonUnPressed;

    internal bool _leftHandContacting = false;
    internal bool _rightHandContacting = false;

    private void Start()
    {
        _colliders = this.transform.GetComponentsInChildren<Collider>().ToList();
    }


    void FixedUpdate()
    {
        if ((!_isButtonPressed && buttonObject.transform.localPosition.y <= buttonHeightLimit * BUTTON_PRESS_THRESHOLD)
            && (_contactHandPressing || !_shouldOnlyBePressedByHand))
        {
            _isButtonPressed = true;
            ButtonPressed();
        }

        if (_isButtonPressed && buttonObject.transform.localPosition.y >= buttonHeightLimit * BUTTON_PRESS_EXIT_THRESHOLD)
        {
            _isButtonPressed = false;
            ButtonUnpressed();

        }
    }

    void ButtonPressed()
    {
        OnButtonPressed?.Invoke();
        if (_buttonShouldDelayRebound)
        {
            StartCoroutine(ButtonCollisionReset());
        }
    }

    void ButtonUnpressed()
    {
        OnButtonUnPressed?.Invoke();
    }

    IEnumerator ButtonCollisionReset()
    {
        yield return new WaitForFixedUpdate();

        buttonObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;

        foreach (var collider in _colliders)
        {
            collider.enabled = false;
        }

        yield return new WaitForSecondsRealtime(_buttonStaydownTimer);

        buttonObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;

        foreach (var collider in _colliders)
        {
            collider.enabled = true;
        }
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
