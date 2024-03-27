/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity;
using Leap.Unity.Interaction;
using UnityEngine;

namespace Leap.Unity.InteractionEngine.Examples
{
    /// <summary>
    /// This simple script changes the color of an InteractionBehaviour as
    /// a function of its distance to the palm of the closest hand that is
    /// hovering nearby.
    /// </summary>
    [AddComponentMenu("")]
    [RequireComponent(typeof(InteractionBehaviour))]
    public class SimpleInteractionGlow : MonoBehaviour
    {
        [Tooltip("If enabled, the object will lerp to its hoverColor when a hand is nearby.")]
        public bool useHover = true;

        [Tooltip("If enabled, the object will use its primaryHoverColor when the primary hover of an InteractionHand.")]
        public bool usePrimaryHover = false;

        [Header("InteractionBehaviour Colors")]
        public Color defaultColor = Color.Lerp(Color.black, Color.white, 0.1F);

        public Color suspendedColor = Color.red;
        public Color hoverColor = Color.Lerp(Color.black, Color.white, 0.7F);
        public Color primaryHoverColor = Color.Lerp(Color.black, Color.white, 0.8F);

        [Header("InteractionButton Colors")]
        [Tooltip("This color only applies if the object is an InteractionButton or InteractionSlider.")]
        public Color pressedColor = Color.white;

        private Material[] _materials;

        private InteractionBehaviour _intObj;

        [SerializeField]
        private Rend[] rends;

        [System.Serializable]
        public class Rend
        {
            public int materialID = 0;
            public Renderer renderer;
        }

        void Start()
        {
            _intObj = GetComponent<InteractionBehaviour>();

            if (rends.Length > 0)
            {
                _materials = new Material[rends.Length];

                for (int i = 0; i < rends.Length; i++)
                {
                    _materials[i] = rends[i].renderer.materials[rends[i].materialID];
                }
            }
        }

        void Update()
        {
            if (_materials != null)
            {

                // The target color for the Interaction object will be determined by various simple state checks.
                Color targetColor = defaultColor;

                // "Primary hover" is a special kind of hover state that an InteractionBehaviour can
                // only have if an InteractionHand's thumb, index, or middle finger is closer to it
                // than any other interaction object.
                if (_intObj.isPrimaryHovered && usePrimaryHover)
                {
                    targetColor = primaryHoverColor;
                }
                else
                {
                    // Of course, any number of objects can be hovered by any number of InteractionHands.
                    // InteractionBehaviour provides an API for accessing various interaction-related
                    // state information such as the closest hand that is hovering nearby, if the object
                    // is hovered at all.
                    if (_intObj.isHovered && useHover)
                    {
                        float glow = _intObj.closestHoveringControllerDistance.Map(0F, 0.2F, 1F, 0.0F);
                        targetColor = Color.Lerp(defaultColor, hoverColor, glow);
                    }
                }

                if (_intObj.isSuspended)
                {
                    // If the object is held by only one hand and that holding hand stops tracking, the
                    // object is "suspended." InteractionBehaviour provides suspension callbacks if you'd
                    // like the object to, for example, disappear, when the object is suspended.
                    // Alternatively you can check "isSuspended" at any time.
                    targetColor = suspendedColor;
                }

                // We can also check the depressed-or-not-depressed state of InteractionButton objects
                // and assign them a unique color in that case.
                if (_intObj is InteractionButton && (_intObj as InteractionButton).isPressed)
                {
                    targetColor = pressedColor;
                }

                // Lerp actual material color to the target color.
                for (int i = 0; i < _materials.Length; i++)
                {
                    _materials[i].color = Color.Lerp(_materials[i].color, targetColor, 30F * Time.deltaTime);
                }
            }
        }

    }
}