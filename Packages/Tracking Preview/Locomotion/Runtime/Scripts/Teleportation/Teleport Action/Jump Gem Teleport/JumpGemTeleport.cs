/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 * Ultraleap proprietary and confidential.                                    *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/
using Leap.Unity.Preview.HandRays;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leap.Unity.Preview.Locomotion
{
    /// <summary>
    /// This teleport method snaps a ray to a "gem" object within your scene when said gem is activated (e.g. through a pinch).
    /// When the user releases the gem a teleport action is fired, if valid.
    /// </summary>
    public class JumpGemTeleport : TeleportActionBase
    {
        private List<JumpGem> _jumpGems = new List<JumpGem>();

        public JumpGem PinchedGem => _pinchedGem;
        private JumpGem _pinchedGem;

        [Header("Jump Gem Teleport - Interaction Setup")]
        [SerializeField, Range(0.0001f,1f), Tooltip("The distance the user has to pinch a gem before the ray will be activated.")]
        private float _minimumDistanceToActivate = 0.1f;
        /// <summary>
        /// The distance the user has to pinch a gem before the ray will be activated.
        /// </summary>
        public float DistanceToActivate => _minimumDistanceToActivate;

        /// <summary>
        /// Called when the gem is pinched or unpinched - passes true if pinched, or false if released
        /// </summary>
        public Action<bool> OnGemPinched;

        private void Awake()
        {
            _jumpGems = FindObjectsOfType<JumpGem>(true).ToList();
            for (int i = 0; i < _jumpGems.Count; i++)
            {
                int j = i;
                _jumpGems[i].OnPinch += (val) => { OnJewelPinch(val, j); };
                _jumpGems[i].OnRelease += OnJewelRelease;
            }
        }

        private void Update()
        {
            ChangeVisibleJewels();
            ProcessPinchedJewel();
        }

        /// <summary>
        /// Hides all non-active gems.
        /// </summary>
        private void ChangeVisibleJewels()
        {
            bool anyPinched = _jumpGems.Any(x => x.IsPinched);
            _pinchedGem = null;
            foreach (var jewel in _jumpGems)
            {
                if (anyPinched)
                {
                    if (jewel.IsPinched)
                    {
                        _pinchedGem = jewel;
                        jewel.ChangeHidden(false);
                    }
                    else
                    {
                        jewel.ChangeHidden(true);
                    }
                }
                else
                {
                    jewel.ChangeHidden(false);
                }
            }
        }

        /// <summary>
        /// Checks the gem to ensure that the ray should be activated.
        /// </summary>
        private void ProcessPinchedJewel()
        {
            if (_pinchedGem != null)
            {
                if (_pinchedGem.DistanceFromPoint() > _minimumDistanceToActivate && _pinchedGem.HasPinchedEnough && !IsSelected)
                {
                    SelectTeleport(true);
                    _pinchedGem = null;
                }
                else if (_pinchedGem.DistanceFromPoint() < _minimumDistanceToActivate && IsSelected)
                {
                    SelectTeleport(false);
                    _pinchedGem = null;
                }
            }
        }

        /// <summary>
        /// When the jewel is pinched, we set the ray to the correct hand and hide other gems.
        /// </summary>
        private void OnJewelPinch(Chirality hand, int index)
        {
            handRayInteractor.handRay.chirality = hand;

            if (handRayInteractor.handRay is TransformWristShoulderHandRay && _jumpGems[index].PinchItem != null)
            {
                ((TransformWristShoulderHandRay)handRayInteractor.handRay).transformToFollow = _jumpGems[index].PinchItem;
            }
            OnGemPinched?.Invoke(true);
        }

        /// <summary>
        /// When the gem is released, we either teleport if pre-requisites have been met, or fail if they have not.
        /// </summary>
        private void OnJewelRelease(bool enough)
        {
            if (handRayInteractor.handRay is TransformWristShoulderHandRay)
            {
                ((TransformWristShoulderHandRay)handRayInteractor.handRay).transformToFollow = null;
            }

            if (IsValid && IsSelected && enough)
            {
                ActivateTeleport(false);
            }
            else
            {
                SelectTeleport(false);
            }
            OnGemPinched?.Invoke(false);
        }
    }
}