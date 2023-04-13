/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Interaction;
using Leap.Unity.Interaction.PhysicsHands;
using Leap.Unity.Preview.HandRays;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Preview.Locomotion
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
            InteractionManager interactionManager = FindObjectOfType<InteractionManager>();

            // Ignore any interaction objects 
            if (interactionManager != null)
            {
                // Ignore the user's hands - they are likely to be in front of the head, as it's the most common interaction zone
                _layerMask ^= interactionManager.contactBoneLayer.layerMask;
                // Ignore any grasped objects 
                _layerMask ^= interactionManager.interactionNoContactLayer.layerMask;
            }

            PhysicsProvider physicsProvider = FindObjectOfType<PhysicsProvider>();
            if (physicsProvider != null)
            {
                _layerMask ^= physicsProvider.HandsLayer.layerMask;
                _layerMask ^= physicsProvider.HandsResetLayer.layerMask;
            }

            FarFieldLayerManager farFieldLayerManager = FindObjectOfType<FarFieldLayerManager>();
            if (physicsProvider != null)
            {
                _layerMask ^= farFieldLayerManager.FarFieldObjectLayer.layerMask;
                _layerMask ^= farFieldLayerManager.FloorLayer.layerMask;
            }

            foreach (var layers in _layersToIgnore)
            {
                _layerMask ^= layers.layerMask;
            }
        }

        private void CheckIfFacingObject()
        {
            Transform head = Camera.main.transform;
            _valueThisFrame = Physics.SphereCast(new Ray(head.position, head.forward), _sphereCastRadius, out RaycastHit hit, _sphereCastMaxLength, _layerMask);
        }
    }
}