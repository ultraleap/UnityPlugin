using Leap.Unity;
using Leap.Unity.PhysicalHands;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandGrabHighlighter : MonoBehaviour
{
    [Tooltip("Automatically listen to the grab events from the Physical Hands Manager in the scene on start")]
    public bool automaticEvents = true;

    public Chirality chirality;

    public Renderer rendererToChange;

    public Color grabActivatedColor;
    public Color grabbedColor;

    public float grabActivatedFadeTime = 0.1f;
    public float grabDeactivatedFadeTime = 0.1f;

    public bool fadeIn = false;
    public bool fadeOut = true;

    Color ungrabbedColor;

    bool grabbing = false;

    public AnimationCurve grabActivationEaseCurve;

    public string materialColorName = "_MainColor";

    private void Awake()
    {
        ungrabbedColor = rendererToChange.material.GetColor(materialColorName);

        if (automaticEvents)
        {
            PhysicalHandsManager physManager = FindObjectOfType<PhysicalHandsManager>();

            if (physManager != null)
            {
                physManager.onGrab.AddListener(OnGrabBegin);
                physManager.onGrabExit.AddListener(OnGrabEnd);
            }
        }
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

            StartCoroutine(HandleGrabDeactivation());
        }
    }

    IEnumerator HandleGrabActivation()
    {
        if(!fadeIn)
        {
            rendererToChange.material.SetColor(materialColorName, grabbedColor);
            yield break;
        }

        float t = grabActivatedFadeTime;
        while (t > 0)
        {
            t -= Time.deltaTime;

            Color color = Color.Lerp(grabbedColor, grabActivatedColor, grabActivationEaseCurve.Evaluate(t / grabActivatedFadeTime));

            rendererToChange.material.SetColor(materialColorName, color);

            yield return null;
        }
    }

    IEnumerator HandleGrabDeactivation()
    {
        if (!fadeOut)
        {
            rendererToChange.material.SetColor(materialColorName, ungrabbedColor);
            yield break;
        }

        float t = grabDeactivatedFadeTime;
        while (t > 0)
        {
            t -= Time.deltaTime;

            Color color = Color.Lerp(ungrabbedColor, grabbedColor, grabActivationEaseCurve.Evaluate(t / grabDeactivatedFadeTime));

            rendererToChange.material.SetColor(materialColorName, color);

            yield return null;
        }
    }
}