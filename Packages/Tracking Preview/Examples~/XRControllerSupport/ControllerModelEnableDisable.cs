using Leap.Unity;
using Leap.Unity.Controllers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerModelEnableDisable : MonoBehaviour
{
    public ControllerPostProcess controllerPostProcess;
    public Chirality chirality;

    // Start is called before the first frame update
    void Start()
    {
        if(controllerPostProcess == null)
        {
            controllerPostProcess = FindObjectOfType<ControllerPostProcess>();
        }
        controllerPostProcess.OnHandInputTypeChange += OnHandInputTypeChange;
    }

    private void OnHandInputTypeChange(Chirality chirality, InputMethodType inputMethodType)
    {
        if(this.chirality == chirality)
        {
            gameObject.SetActive(inputMethodType == InputMethodType.LeapHand);
        }
    }
}
