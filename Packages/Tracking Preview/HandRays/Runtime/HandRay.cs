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
    /// a HandRayDirection holds data about the hand and the corresponding ray.
    /// Including the ray's origin, aim position and direction.
    /// </summary>
    public struct HandRayDirection
    {
        public Hand Hand;
        public Vector3 RayOrigin;
        public Vector3 AimPosition;
        public Vector3 VisualAimPosition;
        public Vector3 Direction;
    }

    public abstract class HandRay : MonoBehaviour
    {
        /// <summary>
        /// Called when a far field ray is calculated.
        /// Subscribe to it to cast rays with the ray direction it provides
        /// </summary>
        public event Action<HandRayDirection> OnHandRayFrame;
        /// <summary>
        /// Called on the frame the ray is enabled
        /// </summary>
        public event Action<HandRayDirection> OnHandRayEnable;
        /// <summary>
        /// Called on the frame the ray is disabled
        /// </summary>
        public event Action<HandRayDirection> OnHandRayDisable;

        /// <summary>
        /// True if the ray should be enabled, false if it should be disabled
        /// </summary>
        public bool HandRayEnabled { get; protected set; }

        /// <summary>
        /// The leap provider provides the hand data from which we calculate the far field ray directions 
        /// </summary>
        public LeapProvider leapProvider;

        /// <summary>
        /// The hand this ray is generated for
        /// </summary>
        public Chirality chirality;

        /// <summary>
        /// The most recently calculated hand ray direction
        /// </summary>
        [HideInInspector] public HandRayDirection handRayDirection = new HandRayDirection();

        // Start is called before the first frame update
        protected virtual void Start()
        {

            if (leapProvider == null)
            {
                leapProvider = FindObjectOfType<LeapServiceProvider>();
                if (leapProvider == null)
                {
                    leapProvider = FindObjectOfType<LeapProvider>();
                    if (leapProvider == null)
                    {
                        Debug.LogWarning("No leap provider in scene - HandRay is dependent on one.");
                    }
                }
            }
        }

        // Update is called once per frame
        protected virtual void Update()
        {
            if (leapProvider == null || leapProvider.CurrentFrame == null)
            {
                return;
            }

            if (HandRayEnabled)
            {
                if (!ShouldEnableRay())
                {
                    HandRayEnabled = false;
                    InvokeOnHandRayDisable(handRayDirection);
                }
                else
                {
                    CalculateRayDirection();
                }
            }
            else
            {
                if (ShouldEnableRay())
                {
                    HandRayEnabled = true;
                    CalculateRayDirection();
                    InvokeOnHandRayEnable(handRayDirection);
                }
            }
        }

        /// <summary>
        /// Calculates the Ray Direction
        /// </summary>
        protected virtual void CalculateRayDirection()
        {
        }

        /// <summary>
        /// Calculates whether the hand ray should be enabled
        /// </summary>
        /// <returns></returns>
        protected virtual bool ShouldEnableRay()
        {
            if (leapProvider.CurrentFrame.GetHand(chirality) == null)
            {
                return false;
            }
            return true;
        }

        protected void InvokeOnHandRayFrame(HandRayDirection handRayDirection)
        {
            OnHandRayFrame?.Invoke(handRayDirection);
        }

        protected void InvokeOnHandRayEnable(HandRayDirection handRayDirection)
        {
            OnHandRayEnable?.Invoke(handRayDirection);
        }

        protected void InvokeOnHandRayDisable(HandRayDirection handRayDirection)
        {
            OnHandRayDisable?.Invoke(handRayDirection);
        }
    }
}