/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;

namespace Leap.Unity.Preview.HandRays
{
    /// <summary>
    /// Abstract base class for HandRay Interactors
    /// Use this class as a way to define how your hand ray gets cast out
    /// </summary>
    public abstract class HandRayInteractor : MonoBehaviour
    {
        public HandRay handRay => _handRay;
        [SerializeField] private HandRay _handRay;

        [Header("Layer Logic")]
        public FarFieldLayerManager farFieldLayerManager;

        [Tooltip("Note that the far field object layer will be added automatically from the FarFieldLayerManager")]
        public LayerMask layerMask = 0;

        [Tooltip("If true, on start the floor layer referenced in FarFieldLayerManager will get added to the layer mask")]
        public bool autoIncludeFloorLayer = false;

        /// <summary>
        /// Called everytime a raycast is fired
        /// </summary>
        public Action<RaycastHit[], RaycastHit> OnRaycastUpdate;

        /// <summary>
        /// The points returned by the raycast, which can be used by a HandRayRenderer
        /// </summary>
        [HideInInspector] public Vector3[] linePoints;
        /// <summary>
        /// The number of points returned by the raycast
        /// </summary>
        [HideInInspector] public int numPoints;

        protected virtual void Start()
        {
            if (farFieldLayerManager == null)
            {
                farFieldLayerManager = FindObjectOfType<FarFieldLayerManager>();
            }

            layerMask |= farFieldLayerManager.FarFieldObjectLayer.layerMask;
            if (autoIncludeFloorLayer)
            {
                layerMask |= farFieldLayerManager.FloorLayer.layerMask;
            }
        }

        protected virtual void OnValidate()
        {
            if (farFieldLayerManager == null)
            {
                farFieldLayerManager = FindObjectOfType<FarFieldLayerManager>();
            }
        }

        protected virtual void OnEnable()
        {
            if (_handRay == null)
            {
                _handRay = FindObjectOfType<WristShoulderHandRay>();
                if (_handRay == null)
                {
                    Debug.LogWarning("HandRayInteractor needs a HandRay");
                    return;
                }
            }
            _handRay.OnHandRayFrame += UpdateRayInteractor;
        }

        protected virtual void OnDisable()
        {
            if (_handRay == null)
            {
                return;
            }

            _handRay.OnHandRayFrame -= UpdateRayInteractor;
        }

        private void UpdateRayInteractor(HandRayDirection handRayDirection)
        {
            UpdateRayInteractorLogic(handRayDirection, out RaycastHit[] allHits, out RaycastHit primaryHit);
            OnRaycastUpdate?.Invoke(allHits, primaryHit);
        }

        /// <summary>
        /// Contains the main logic of the ray interactor - takes in a handRayDirection and returns every point it hit,
        /// and a primary point it hit. 
        /// </summary>
        /// <param name="handRayDirection">The hand ray direction generated by a HandRay</param>
        /// <param name="allHits">Every valid object intersected by the Ray Interactor. This may be more than one when using techniques like spherecasting.</param>
        /// <param name="primaryHit">The main object intersected by the Ray Interactor.</param>
        /// <returns>The number of hits</returns>
        protected abstract int UpdateRayInteractorLogic(HandRayDirection handRayDirection, out RaycastHit[] allHits, out RaycastHit primaryHit);
    }
}