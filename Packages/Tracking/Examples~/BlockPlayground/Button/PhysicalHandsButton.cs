using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PhysicalHandsButton : MonoBehaviour
{
    public GameObject buttonObject;
    [Tooltip("The local position which the button will be limited to and will try to return to.")]
    public float buttonHeightLimit = 0.02f;

    private const float BUTTON_PRESS_THRESHOLD = 0.01F;
    private const float BUTTON_PRESS_EXIT_THRESHOLD = 0.15F;

    private bool _isButtonPressed = false;

    public UnityEvent OnButtonPressed;
    public UnityEvent OnButtonUnPressed;




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
            ButtonUnpressed();

        }
    }

    void ButtonPressed()
    {
        OnButtonPressed.Invoke();
    }

    void ButtonUnpressed()
    {
        OnButtonUnPressed.Invoke();
    }

    public void CallDebug()
    {
        Debug.Log("LogCalled");
    }


}
