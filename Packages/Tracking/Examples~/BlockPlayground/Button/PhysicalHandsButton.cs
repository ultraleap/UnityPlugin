using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class PhysicalHandsButton : MonoBehaviour
{
    public GameObject buttonObject;
    [Tooltip("The local position which the button will be limited to and will try to return to.")]
    public float buttonHeightLimit = 0.02f;

    private const float BUTTON_PRESS_THRESHOLD = 0.01F;
    private const float BUTTON_PRESS_EXIT_THRESHOLD = 0.09F;

    [SerializeField]
    private bool _isButtonPressed = false;

    public UnityEvent OnButtonPressed;
    public UnityEvent OnButtonUnPressed;

    private List<Collider> _colliders;

    [SerializeField]
    private bool _buttonShouldDelayRebound = false;
    [SerializeField]
    private float buttonStaydownTimer = 2;


    private void Start()
    {
        _colliders = this.transform.GetComponentsInChildren<Collider>().ToList();
    }


    void FixedUpdate()
    {
        if (!_isButtonPressed && buttonObject.transform.localPosition.y <= buttonHeightLimit * BUTTON_PRESS_THRESHOLD)
        {
            Debug.Log("Button Pressed");
            _isButtonPressed = true;
            ButtonPressed();
        }

        if (_isButtonPressed && buttonObject.transform.localPosition.y >= buttonHeightLimit * BUTTON_PRESS_EXIT_THRESHOLD)
        {
            Debug.Log("Button UnPressed");
            _isButtonPressed = false;
            ButtonUnpressed();

        }
    }

    void ButtonPressed()
    {
        OnButtonPressed.Invoke();
        if (_buttonShouldDelayRebound)
        {
            StartCoroutine(ButtonCollisionReset());
        }
    }

    void ButtonUnpressed()
    {
        OnButtonUnPressed.Invoke();
    }

    IEnumerator ButtonCollisionReset()
    {
        yield return new WaitForFixedUpdate();

        buttonObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;

        foreach (var collider in _colliders)
        {
            collider.enabled = false;
        }

        yield return new WaitForSecondsRealtime(buttonStaydownTimer);

        buttonObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;

        foreach (var collider in _colliders)
        {
            collider.enabled = true;
        }

    }

}
