using Leap.Unity;
using Leap.Unity.Controllers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ControllerModelEnableDisable checks to see whether to enable itself if LeapHands are the current 
/// input type being used by a ControllerPostProcess. This is to show controller models when hands are active
/// so that a user can easily find them again.
/// </summary>
public class ControllerModelEnableDisable : MonoBehaviour
{
    public ControllerPostProcess controllerPostProcess;
    public Chirality chirality;

    // Start is called before the first frame update
    void Start()
    {
        if (controllerPostProcess == null)
        {
            controllerPostProcess = FindAnyObjectByType<ControllerPostProcess>();
        }
        controllerPostProcess.OnHandInputTypeChange += OnHandInputTypeChange;

        OnHandInputTypeChange(chirality, controllerPostProcess.GetCurrentInputMethodTypeByChirality(chirality));
    }

    private void OnDestroy()
    {
        controllerPostProcess.OnHandInputTypeChange -= OnHandInputTypeChange;
    }

    private void OnHandInputTypeChange(Chirality chirality, InputMethodType inputMethodType)
    {
        if (this.chirality == chirality)
        {
            gameObject.SetActive(inputMethodType == InputMethodType.LeapHand);
        }
    }
}