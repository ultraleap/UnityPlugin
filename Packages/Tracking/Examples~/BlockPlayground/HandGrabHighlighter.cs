using Leap.Unity;
using Leap.Unity.PhysicalHands;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandGrabHighlighter : MonoBehaviour
{
    public Chirality chirality;

    public Renderer rendererToChange;

    public Color grabActivatedColor;
    public Color grabbedColor;

    public float grabActivatedTime = 0.1f;

    Color ungrabbedColor;

    bool grabbing = false;

    public AnimationCurve grabActivationEaseCurve;

    private void Awake()
    {
        ungrabbedColor = rendererToChange.material.GetColor("_MainColor");
    }

    public void OnGrabBegin(ContactHand contacthand, Rigidbody rbody)
    {
        if (!grabbing && contacthand.Handedness == chirality)
        {
            StopAllCoroutines();

            grabbing = true;

            StartCoroutine(HandleGrabActivation());
        }
    }

    public void OnGrabEnd(ContactHand contacthand, Rigidbody rbody)
    {
        if (grabbing && contacthand.Handedness == chirality)
        {
            StopAllCoroutines();

            grabbing = false;
            rendererToChange.material.SetColor("_MainColor", ungrabbedColor);

        }
    }

    IEnumerator HandleGrabActivation()
    {
        float t = grabActivatedTime;
        while (t > 0)
        {
            t -= Time.deltaTime;

            Color color = Color.Lerp(grabbedColor, grabActivatedColor, grabActivationEaseCurve.Evaluate(t / grabActivatedTime));

            rendererToChange.material.SetColor("_MainColor", color);

            yield return null;
        }
    }
}