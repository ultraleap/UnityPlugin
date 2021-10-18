/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;

namespace Leap.Unity.InputModule
{
    public class UIInputCursor : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private PointerElement element;
        [SerializeField] private  float interactionPointerScale = 0.6f;
        
        private Vector3 initialScale;

        private void Awake()
        {
            initialScale = spriteRenderer.transform.localScale;
        }

        private void OnEnable()
        {
            if (element != null)
            {
                element.OnPointerStateChanged += OnPointerStateChanged;
            }
        }

        private void OnDisable()
        {
            if (element != null)
            {
                element.OnPointerStateChanged -= OnPointerStateChanged;
            }
        }

        private void OnPointerStateChanged(PointerElement element, Hand hand)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.transform.localScale = hand != null
                ? Vector3.Lerp(initialScale, initialScale * interactionPointerScale, hand.PinchStrength)
                : Vector3.one;

            switch(element.PointerState) {
                case PointerStates.OnCanvas:
                    spriteRenderer.color = Color.white;
                    break;
                case PointerStates.OffCanvas:
                    spriteRenderer.color = Color.clear;
                    break;
                case PointerStates.OnElement:
                    spriteRenderer.color = Color.green;
                    break;
                case PointerStates.PinchingToCanvas:
                    spriteRenderer.color = Color.green;
                    break;
                case PointerStates.PinchingToElement:
                    spriteRenderer.color = Color.green;
                    break;
                case PointerStates.NearCanvas:
                    spriteRenderer.color = Color.clear;
                    break;
                case PointerStates.TouchingCanvas:
                    spriteRenderer.color = Color.white;
                    break;
                case PointerStates.TouchingElement:
                    spriteRenderer.color = Color.green;
                    break;
                default:
                    spriteRenderer.color = Color.white;
                    break;
            }
        }
    }
}
