/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.PhysicalHands;
using Leap.Preview.HandRays;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Preview.Locomotion
{
    /// <summary>
    /// Small script that detects whether the user is facing an object this frame.
    /// Useful for contextual activation of interactions.
    /// Spherecasts out through the user's gaze
    /// </summary>
    public class IsFacingObject : MonoBehaviour
    {
        [Tooltip("Note that the Interaction Engine, Physics Hands and FarFieldObject layers will be ignored automatically.")]
        [Header("Layer Logic")]
        [SerializeField] private List<SingleLayer> _layersToIgnore = new List<SingleLayer>();

        protected LayerMask _layerMask;

        protected bool hasCastThisFrame = false;

        protected bool _valueThisFrame = false;

        [Tooltip("Size of the spherecast radius to cast")]
        [SerializeField] private float _sphereCastRadius = 0.3f;
        [Tooltip("Max distance to spherecast")]
        [SerializeField] private float _sphereCastMaxLength = 0.5f;

        public bool ValueThisFrame
        {
            get
            {
                if (!hasCastThisFrame)
                {
                    CheckIfFacingObject();
                }
                return _valueThisFrame;
            }
        }

        void Start()
        {
            SetLayerMask();
        }

        private void SetLayerMask()
        {
            _layerMask = -1;
            
            PhysicalHandsManager _physicalHandsManager = FindAnyObjectByType<PhysicalHandsManager>();
            if (_physicalHandsManager != null)
            {
                _layerMask ^= _physicalHandsManager.HandsLayer.layerMask;
                _layerMask ^= _physicalHandsManager.HandsResetLayer.layerMask;
            }
            
            FarFieldLayerManager _farFieldLayerManager = FindAnyObjectByType<FarFieldLayerManager>();
            if (_farFieldLayerManager != null)
            {
                _layerMask ^= _farFieldLayerManager.FarFieldObjectLayer.layerMask;
                _layerMask ^= _farFieldLayerManager.FloorLayer.layerMask;
            }

            foreach (var _layers in _layersToIgnore)
            {
                _layerMask ^= _layers.layerMask;
            }
        }

        private void CheckIfFacingObject()
        {
            Transform _head = Camera.main.transform;
            _valueThisFrame = Physics.SphereCast(new Ray(_head.position, _head.forward), _sphereCastRadius, out RaycastHit _hit, _sphereCastMaxLength, _layerMask);
        }
    }
}