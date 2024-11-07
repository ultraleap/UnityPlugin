/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.PhysicalHands;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leap.Examples
{
    /// <summary>
    /// This simple script changes the color of an object as
    /// a function of its distance to the palm of the closest hand that is
    /// hovering nearby.
    /// </summary>
    public class SimpleInteractionGlow : MonoBehaviour, IPhysicalHandHover, IPhysicalHandContact, IPhysicalHandGrab, IPhysicalHandPrimaryHover
    {
        [Tooltip("If enabled, the object will lerp to its hoverColor when a hand is nearby.")]
        public bool useHover = true;

        [Tooltip("If enabled, the object will use its primaryHoverColor when the primary hover of a PhysicalHand.")]
        public bool usePrimaryHover = false;

        [Header("Interaction Colors")]
        public Color defaultColor = Color.Lerp(Color.black, Color.white, 0.1F);

        public Color suspendedColor = Color.red;
        public Color hoverColor = Color.Lerp(Color.black, Color.white, 0.7F);
        public Color primaryHoverColor = Color.Lerp(Color.black, Color.white, 0.8F);

        [Header("Button Colors")]
        [Tooltip("This color only applies if the object is a PhysicalHandsButton or PhyaicalHandsSlider.")]
        public Color pressedColor = Color.white;

        private Material[] _materials;

        [SerializeField]
        private PhysicalHandsButton _physHandButton;

        [SerializeField]
        private Rend[] rends;

        // Dictionary of contact types, Contact is first item, hover is second, grab is third
        private Dictionary<ContactHand, bool[]> _interactingHands = new Dictionary<ContactHand, bool[]>();

        bool _handHovering = false;
        bool _handGrabbing = false;
        bool _isPrimaryHovered = false;
        bool _isPrimaryHoveredLeft = false;
        bool _isPrimaryHoveredRight = false;

        Rigidbody _rigBody;

        [System.Serializable]
        public class Rend
        {
            public int materialID = 0;
            public Renderer renderer;
        }

        void Start()
        {
            if (_physHandButton == null)
            {
                this.TryGetComponent<PhysicalHandsButton>(out _physHandButton);
            }

            if (_physHandButton != null)
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

            _rigBody = GetComponent<Rigidbody>();
        }

        void Update()
        {
            if (_materials != null)
            {
                // The target color for the Interaction object will be determined by various simple state checks.
                Color targetColor = defaultColor;

                if (usePrimaryHover && _isPrimaryHovered)
                {
                    targetColor = primaryHoverColor;
                }
                else
                {
                    if (_handHovering && useHover)
                    {
                        float glow = PhysicalHandUtils.ClosestHandBoneDistance(_interactingHands.Keys.ToList(), _rigBody).Map(0F, 0.05F, 1F, 0.0F);

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

        public void OnHandPrimaryHover(ContactHand hand)
        {
            if (hand.Handedness == Chirality.Right)
            {
                _isPrimaryHoveredRight = true;
            }
            else if (hand.Handedness == Chirality.Left)
            {
                _isPrimaryHoveredLeft = true;
            }

            if (_isPrimaryHoveredLeft || _isPrimaryHoveredRight)
            {
                _isPrimaryHovered = true;
            }
        }

        public void OnHandPrimaryHoverExit(ContactHand hand)
        {
            if (hand.Handedness == Chirality.Right)
            {
                _isPrimaryHoveredRight = false;
            }
            else if (hand.Handedness == Chirality.Left)
            {
                _isPrimaryHoveredLeft = false;
            }

            if (!_isPrimaryHoveredLeft && !_isPrimaryHoveredRight)
            {
                _isPrimaryHovered = false;
            }
        }
        #endregion
    }
}