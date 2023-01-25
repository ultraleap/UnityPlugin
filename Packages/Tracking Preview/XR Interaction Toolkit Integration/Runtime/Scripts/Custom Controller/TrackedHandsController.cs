/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Preview.HandRays;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace Leap.Unity.Preview.XRInteractionToolkit
{
    /// <summary>
    /// Integrating hand tracking into the XR Interaction Engine.
    /// This should be used instead of the XRController component and together with either a ray interactor or a direct interactor.
    /// Note that when using a ray interactor, you should also have a FarFieldDirection component somewhere in the scene (use FarFieldDirection Prefab)
    /// </summary>
    public class TrackedHandsController : XRBaseController
    {
        public LeapProvider leapProvider;
        public Chirality chirality;
        public HandRay handRay;

        // Different Interaction poses that can be used for select, activate and UI press interactions
        public enum InteractionPose
        {
            Pinch,
            Grab,
            LongPinch,
            LongGrab
        }
        // select interaction pose is used to grab objects and teleport
        public InteractionPose selectInteractionPose;
        // activate interaction pose is used to activate a held object
        public InteractionPose activateInteractionPose;
        // ui press interaction pose is used to interact with UI
        public InteractionPose uiPressInteractionPose;

        private Transform RayOrigin;

        private bool lastPinchState;
        private bool lastGrabState;
        private float lastPinchTime;
        private float lastGrabTime;

        private float lengthGrabPinchTime = 1;

        private XRRayInteractor xrRayInteractor;

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            if (leapProvider == null)
            {
                leapProvider = FindObjectOfType<LeapServiceProvider>();
                if (leapProvider == null)
                {
                    leapProvider = FindObjectOfType<LeapProvider>();
                    if (leapProvider == null)
                    {
                        Debug.LogWarning("No leap provider in scene - TrackedHandsController is dependent on one.");
                    }
                }
            }

            updateTrackingType = UpdateType.UpdateAndBeforeRender;

            // if using a ray interactor, set everything up for computing the ray
            xrRayInteractor = GetComponent<XRRayInteractor>();
            if (xrRayInteractor != null)
            {
                RayOrigin = GetComponent<XRRayInteractor>().rayOriginTransform;

                if (handRay == null)
                {
                    handRay = FindObjectsOfType<WristShoulderHandRay>().FirstOrDefault(ray => ray.chirality == chirality);
                }
            }

            lastPinchTime = Time.time;
            lastGrabTime = Time.time;
        }

        /// <inheritdoc />
        protected override void OnDisable()
        {
            base.OnDisable();
        }

        /// <inheritdoc />
        protected override void UpdateTrackingInput(XRControllerState controllerState)
        {
            if (controllerState == null)
                return;

            controllerState.inputTrackingState = InputTrackingState.None;

            Hand hand = leapProvider.CurrentFrame.GetHand(chirality);
            if (hand == null)
            {
                return;
            }

            controllerState.position = hand.GetPredictedPinchPosition();
            controllerState.inputTrackingState |= InputTrackingState.Position;

            controllerState.rotation = hand.Rotation;
            controllerState.inputTrackingState |= InputTrackingState.Rotation;
            if (xrRayInteractor != null)
            {
                // Note: this doesn't work too well - XRI processes the positions and
                // adds a fair amount of latency to them.
                // This is particularly noticable if using a LineType.ProjectileCurve on your XRRayInteractor,
                // which  will result in a very noodly & unresponsive line.
                // We can 'fix' this by setting the updateTrackingType of the TrackedHandsController to UpdateType.Update,
                // but this XRI then makes our ray direction very jittery.
                // This will be fixed in the future by better integration with XRI -
                // potentially through our own implementation of XRRayInteractor

                if (RayOrigin == null) RayOrigin = xrRayInteractor.rayOriginTransform;
                if (RayOrigin != null)
                {
                    RayOrigin.position = handRay.handRayDirection.VisualAimPosition;
                    RayOrigin.rotation = Quaternion.LookRotation(handRay.handRayDirection.Direction);
                }
            }
        }

        /// <inheritdoc />
        protected override void UpdateInput(XRControllerState controllerState)
        {
            base.UpdateInput(controllerState);
            if (controllerState == null)
            {
                return;
            }

            controllerState.ResetFrameDependentStates();

            if (xrRayInteractor != null)
            {
                if (xrRayInteractor.isActiveAndEnabled && !handRay.HandRayEnabled)
                {
                    xrRayInteractor.enabled = false;
                }
                else if (!xrRayInteractor.isActiveAndEnabled && handRay.HandRayEnabled)
                {
                    xrRayInteractor.enabled = true;
                }
            }

            Hand hand = leapProvider.CurrentFrame.GetHand(chirality);
            if (hand == null)
            {
                return;
            }

            controllerState.selectInteractionState.SetFrameState(getInteractionState(selectInteractionPose, hand));
            controllerState.activateInteractionState.SetFrameState(getInteractionState(activateInteractionPose, hand));
            controllerState.uiPressInteractionState.SetFrameState(getInteractionState(uiPressInteractionPose, hand));
        }


        private bool getInteractionState(InteractionPose pose, Hand hand)
        {
            switch (pose)
            {
                case InteractionPose.Pinch:
                    return hand.IsPinching();

                case InteractionPose.Grab:
                    return hand.GrabStrength > 0.7f;

                case InteractionPose.LongPinch:
                    bool longPinch = false;
                    if (hand.IsPinching() && lastPinchState && Time.time - lastPinchTime > lengthGrabPinchTime)
                        longPinch = true;

                    else if (!hand.IsPinching() && lastPinchState)
                        lastPinchState = false;

                    else if (hand.IsPinching() && !lastPinchState)
                    {
                        lastPinchState = true;
                        lastPinchTime = Time.time;
                    }
                    return longPinch;

                case InteractionPose.LongGrab:
                    bool longGrab = false;

                    if (hand.GrabStrength > 0.7f && lastGrabState && Time.time - lastGrabTime > lengthGrabPinchTime)
                        longPinch = true;

                    else if (!(hand.GrabStrength > 0.7f) && lastGrabState)
                        lastGrabState = false;

                    else if (hand.GrabStrength > 0.7f && !lastGrabState)
                    {
                        lastGrabState = true;
                        lastGrabTime = Time.time;
                    }
                    return longGrab;
            }
            return false;
        }
    }
}