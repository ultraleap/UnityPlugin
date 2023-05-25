/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Leap.Unity.InputModule
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class UIInputCursor : MonoBehaviour
    {
        [SerializeField] private PointerElement element;
        [SerializeField] private float interactionPointerScale = 0.6f;

        private SpriteRenderer spriteRenderer;
        private Vector3 initialScale;

        public ColorBlock colorBlock;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
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

            if (element.IsUserInteractingDirectly && !element.ShowDirectPointerCursor)
            {
                spriteRenderer.enabled = false;
            }
            else
            {
                spriteRenderer.enabled = true;
            }

            spriteRenderer.transform.localScale = hand != null
                ? Vector3.Lerp(initialScale, initialScale * interactionPointerScale, hand.PinchStrength)
                : Vector3.one;

            switch (element.AggregatePointerState)
            {
                case PointerStates.OnCanvas:
                    spriteRenderer.color = colorBlock.normalColor;
                    break;
                case PointerStates.OffCanvas:
                    spriteRenderer.color = colorBlock.disabledColor;
                    break;
                case PointerStates.OnElement:
                    spriteRenderer.color = colorBlock.highlightedColor;
                    break;
                case PointerStates.PinchingToCanvas:
                    spriteRenderer.color = colorBlock.pressedColor;
                    break;
                case PointerStates.PinchingToElement:
                    spriteRenderer.color = colorBlock.pressedColor;
                    break;
                case PointerStates.NearCanvas:
                    spriteRenderer.color = colorBlock.normalColor;
                    break;
                case PointerStates.TouchingCanvas:
                    spriteRenderer.color = colorBlock.normalColor;
                    break;
                case PointerStates.TouchingElement:
                    spriteRenderer.color = colorBlock.pressedColor;
                    break;
                default:
                    spriteRenderer.color = colorBlock.normalColor;
                    break;
            }
        }
    }
}