/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using UnityEngine;


namespace Leap.PhysicalHands.Examples
{
    public class HandGrabHighlighter : MonoBehaviour
    {
        [Tooltip("Automatically listen to the grab events from the Physical Hands Manager in the scene on start.")]
        public bool automaticEvents = true;

        [Tooltip("Which hand should use the grab highlighter? you will need to add one of these scripts for each hand.")]
        public Chirality chirality;
        [Tooltip("This should be set to your hand material renderer.")]
        public Renderer rendererToChange;
        [Tooltip("Which colour should the material turn to as soon as the grab is activated.")]
        public Color grabActivatedColor;
        [Tooltip("Which colour should the material lerp to over time while grab is active. This will be the colour of the hand while grabbed for longer periods.")]
        public Color grabbedColor;

        [Tooltip("How long should it take to lerp from grabActivatedColor to grabbedColor.")]
        public float grabActivatedFadeTime = 0.1f;
        [Tooltip("How long should it take to lerp from grabbedColor back to default.")]
        public float grabDeactivatedFadeTime = 0.1f;

        [Tooltip("Should fade in?")]
        public bool fadeIn = false;
        [Tooltip("Should fade out?")]
        public bool fadeOut = true;

        Color ungrabbedColor;

        bool grabbing = false;

        public AnimationCurve grabActivationEaseCurve;

        [Tooltip("Which color should change? this is used for non-standard shaders and should be '_Color' for standard shaders.")]
        public string materialColorName = "_MainColor";

        private void Awake()
        {
            ungrabbedColor = rendererToChange.material.GetColor(materialColorName);

            if (automaticEvents)
            {
#if UNITY_6000_0_OR_NEWER
                PhysicalHandsManager physManager = FindFirstObjectByType<PhysicalHandsManager>();
#else
                PhysicalHandsManager physManager = FindObjectOfType<PhysicalHandsManager>();
#endif

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
            if (!fadeIn)
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
}
