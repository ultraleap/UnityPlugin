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
using Leap.Unity.PhysicalHands;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Leap.Unity.Examples
{
    /// <summary>
    /// This simple script changes the color of an InteractionBehaviour as
    /// a function of its distance to the palm of the closest hand that is
    /// hovering nearby.
    /// </summary>
    [AddComponentMenu("")]
    //[RequireComponent(typeof(InteractionBehaviour))]
    public class SimpleInteractionGlow : MonoBehaviour, IPhysicalHandHover, IPhysicalHandContact, IPhysicalHandGrab
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

        private PhysicalHandsButton _physHandButton;

        [SerializeField]
        private Rend[] rends;

        // Dictionary of contact types, Contact is first item, hover is second, grab is third
        private Dictionary<ContactHand, bool[]> _interactingHands = new Dictionary<ContactHand, bool[]>();


        bool _handContacting = false;
        bool _handHovering = false;
        bool _handGrabbing = false;

        [System.Serializable]
        public class Rend
        {
            public int materialID = 0;
            public Renderer renderer;
        }

        private float closestHandDistance
        {
            get
            {
                return PhysicalHandUtils.ClosestHandDistance(_interactingHands.Keys.ToList(), this.gameObject);
            }
        }

        void Start()
        {
            _intObj = GetComponent<InteractionBehaviour>();
            this.TryGetComponent<PhysicalHandsButton>(out _physHandButton);

            if(_physHandButton != null )
            {
                _physHandButton.OnHandContact?.AddListener(OnHandContact);
                _physHandButton.OnHandContactExit?.AddListener(OnHandContactExit);
                _physHandButton.OnHandHover?.AddListener(OnHandHover);
                _physHandButton.OnHandHoverExit?.AddListener(OnHandHoverExit);
            }

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
                if (_intObj != null)
                {
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
                }
                else
                {
                    if (/*_intObj.isPrimaryHovered &&*/ usePrimaryHover)
                    {
                        targetColor = primaryHoverColor;
                    }
                    else
                    {
                        if (_handHovering && useHover)
                        {
                            float glow = PhysicalHandUtils.ClosestHandDistance(_interactingHands.Keys.ToList(), this.gameObject).Map(0F, 0.2F, 1F, 0.0F);
                            targetColor = Color.Lerp(defaultColor, hoverColor, glow);
                        }
                    }

                    if (_handGrabbing)
                    {
                        targetColor = suspendedColor;
                    }

                    // We can also check the depressed-or-not-depressed state of Physical Hands Button objects
                    // and assign them a unique color in that case.
                    if (_physHandButton && _physHandButton.IsPressed)
                    {
                        targetColor = pressedColor;
                    }
                }

                // Lerp actual material color to the target color.
                for (int i = 0; i < _materials.Length; i++)
                {
                    _materials[i].color = Color.Lerp(_materials[i].color, targetColor, 30F * Time.deltaTime);
                }
            }
        }

        #region Physical Hand Callbacks
        public void OnHandContact(ContactHand hand)
        {
            _interactingHands[hand] = new bool[3] { true, false, false };
        }

        public void OnHandContactExit(ContactHand hand)
        {
            if (_interactingHands.ContainsKey(hand))
            {
                _interactingHands[hand][0] = false;
            }
            _handContacting = _interactingHands.Any(kv => kv.Value[0]);
        }

        public void OnHandHover(ContactHand hand)
        {
            _interactingHands[hand] = new bool[3] { false, true, false };
            _handHovering = true;
        }

        public void OnHandHoverExit(ContactHand hand)
        {
            if (_interactingHands.ContainsKey(hand))
            {
                _interactingHands[hand][1] = false;
            }
            _handHovering = _interactingHands.Any(kv => kv.Value[1]);
        }

        public void OnHandGrab(ContactHand hand)
        {
            _interactingHands[hand] = new bool[3] { false, false, true };
            _handGrabbing = true;
        }

        public void OnHandGrabExit(ContactHand hand)
        {
            if (_interactingHands.ContainsKey(hand))
            {
                _interactingHands[hand][2] = false;
            }
            _handGrabbing = _interactingHands.Any(kv => kv.Value[2]);
        }
        #endregion
    }
}