/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
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
                leapProvider = Hands.Provider;
            }
        }

        // Update is called once per frame
        protected virtual void Update()
        {
            Hand hand = leapProvider?.CurrentFrame?.GetHand(chirality);
            if (hand == null)
            {
                return;
            }

            handRayDirection.Hand = hand;
            handRayDirection.VisualAimPosition = CalculateVisualAimPosition();

            //Filtering using the One Euro filter reduces jitter from both positions
            handRayDirection.AimPosition = CalculateAimPosition();
            handRayDirection.RayOrigin = CalculateRayOrigin();

            handRayDirection.Direction = CalculateDirection();
        }

        protected abstract Vector3 CalculateVisualAimPosition();

        protected abstract Vector3 CalculateAimPosition();

        protected abstract Vector3 CalculateRayOrigin();

        protected abstract Vector3 CalculateDirection();

        public abstract void ResetRay();
    }
}