using Leap.Unity;
using Leap.Unity.PhysicalHands;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandGrabHighlighter : MonoBehaviour
{
    public Chirality chirality;

    public Renderer rendererToChange;

    public Color grabbedColor;

    Color ungrabbedColor;

    bool grabbing = false;

    private void Awake()
    {
        ungrabbedColor = rendererToChange.material.GetColor("_MainColor");
    }

    public void OnGrabBegin(ContactHand contacthand, Rigidbody rbody)
    {
        if (contacthand.Handedness == chirality)
        {
            grabbing = true;
        }

        UpdateMaterial();
    }

    public void OnGrabEnd(ContactHand contacthand, Rigidbody rbody)
    {
        if (contacthand.Handedness == chirality)
        {
            grabbing = false;
        }

        UpdateMaterial();
    }

    void UpdateMaterial()
    {
        rendererToChange.material.SetColor("_MainColor", grabbing ? grabbedColor : ungrabbedColor);
    }
}