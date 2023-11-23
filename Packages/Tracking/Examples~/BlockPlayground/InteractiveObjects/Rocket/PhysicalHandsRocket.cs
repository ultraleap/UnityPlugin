using Leap.Unity.Controllers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicalHandsRocket : MonoBehaviour
{
    public GameObject buttonObject;
    public GameObject noseCone;
    [Tooltip("The local position which the button will be limited to and will try to return to.")]
    public float buttonHeightLimit = 0.02f;
    public float rocketPower = 30;

    private const float BUTTON_PRESS_THRESHOLD = 0.01F;
    private const float BUTTON_PRESS_EXIT_THRESHOLD = 0.15F;

    private bool _isButtonPressed = false;

    private Rigidbody _rigidbody;

    bool launching = false;

    void Start()
    {
        _rigidbody = this.GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (buttonObject.transform.localPosition.y <= buttonHeightLimit * BUTTON_PRESS_THRESHOLD
            && !_isButtonPressed)
        {
            _isButtonPressed = true;
            ButtonPressed();
        }

        if (_isButtonPressed && buttonObject.transform.localPosition.y >= buttonHeightLimit * BUTTON_PRESS_EXIT_THRESHOLD)
        {
            _isButtonPressed = false;
        }
    }

    void ButtonPressed()
    {
        if (launching)
            return;

        _rigidbody.isKinematic = false;
        StartCoroutine(RocketBurn());
    }

    IEnumerator RocketBurn()
    {
        launching = true;

        _rigidbody.angularDrag = 20;
        _rigidbody.useGravity = true;
        float timePassed = 0;
        while (timePassed < 3)
        {
            var heading = noseCone.transform.position - this.transform.position;
            _rigidbody.AddForceAtPosition(heading.normalized * rocketPower, transform.position, ForceMode.Acceleration);
            timePassed += Time.deltaTime;
            yield return null;
        }

        launching = false;
    }

    public void StopLaunch()
    {
        StopAllCoroutines();
        launching = false;
    }
}
