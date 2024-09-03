/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Leap.InputModule
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class UIInputCursor : MonoBehaviour
    {
        [SerializeField] private PointerElement element;

        [Tooltip("The minimum scale of the pointer when reacting to the user's interaction proximity (either pinch or distance, depending on mode)")]
        [Range(0, 1)] [SerializeField] private float interactionPointerScale = 0.4f;
        [Tooltip("The range of user interaction distance from the canvas at which to scale the pointer, when in direct mode")]
        [Range(0, 1)][SerializeField] private float interactionPointerRange = 0.2f;
        [Tooltip("If directly interacting, this will fade the cursor at the distance range specified above")]
        [SerializeField] private bool fadeCursorAtDistance = true;

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

            if (element.IsUserInteractingDirectly)
            {
                spriteRenderer.transform.localScale = hand != null
                    ? Vector3.Lerp(initialScale * interactionPointerScale, initialScale, element.DistanceOfTipToPointer(hand).Map(0.0f, interactionPointerRange, 0.0f, 1.0f))
                    : Vector3.one;
            }
            else
            {
                spriteRenderer.transform.localScale = hand != null
                    ? Vector3.Lerp(initialScale, initialScale * interactionPointerScale, hand.PinchStrength)
                    : Vector3.one;
            }

            switch (element.AggregatePointerState)
            {
                case PointerStates.OffCanvas:
                    spriteRenderer.color = colorBlock.disabledColor;
                    return;
                case PointerStates.OnCanvas:
                    spriteRenderer.color = colorBlock.normalColor;
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

            if (element.IsUserInteractingDirectly && fadeCursorAtDistance)
            {
                spriteRenderer.color = new Color(
                    spriteRenderer.color.r, 
                    spriteRenderer.color.g, 
                    spriteRenderer.color.b, 
                    spriteRenderer.transform.localScale.x.Map(
                        initialScale.x, 
                        initialScale.x * interactionPointerScale, 
                        0.0f,
                        1.0f));
            }
        }
    }
}