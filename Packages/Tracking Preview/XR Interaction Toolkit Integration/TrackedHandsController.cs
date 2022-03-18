/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity;
using Leap.Unity.Preview.FarFieldInteractions;
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
        // The hand model base that this controller gets its input data from
        public HandModelBase HandModel = null;

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

        Transform RayOrigin;

        Camera mainCamera;
        Quaternion cameraRotation;

        FarFieldDirection rayDirection;

        bool lastPinchState;
        bool lastGrabState;
        float lastPinchTime;
        float lastGrabTime;

        float lengthGrabPinchTime = 1;



        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            // if using a ray interactor, set everything up for computing the ray
            if (GetComponent<XRRayInteractor>() != null)
            {
                RayOrigin = GetComponent<XRRayInteractor>().rayOriginTransform;
                rayDirection = FindObjectOfType<FarFieldDirection>();
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

            if (HandModel.GetLeapHand() == null) return;

            controllerState.position = HandModel.GetLeapHand().GetPredictedPinchPosition();
            controllerState.inputTrackingState |= InputTrackingState.Position;

            controllerState.rotation = HandModel.GetLeapHand().Rotation.ToQuaternion();
            controllerState.inputTrackingState |= InputTrackingState.Rotation;

            // set rotation for the ray if a xr ray interactor is attached
            if (GetComponent<XRRayInteractor>() != null)
            {
                if (RayOrigin == null) RayOrigin = GetComponent<XRRayInteractor>().rayOriginTransform;
                if (RayOrigin != null)
                {
                    int handIndex = HandModel.Handedness == Chirality.Left ? 0 : 1;
                    RayOrigin.rotation = Quaternion.LookRotation(rayDirection.FarFieldRays[handIndex].Direction);
                }
            }
        }

        /// <inheritdoc />
        protected override void UpdateInput(XRControllerState controllerState)
        {
            base.UpdateInput(controllerState);
            if (controllerState == null)
                return;


            controllerState.ResetFrameDependentStates();


            if (HandModel.GetLeapHand() == null) return;


            controllerState.selectInteractionState.SetFrameState(getInteractionState(selectInteractionPose));
            controllerState.activateInteractionState.SetFrameState(getInteractionState(activateInteractionPose));
            controllerState.uiPressInteractionState.SetFrameState(getInteractionState(uiPressInteractionPose));
        }


        bool getInteractionState(InteractionPose pose)
        {
            switch (pose)
            {
                case InteractionPose.Pinch:
                    return HandModel.GetLeapHand().IsPinching();

                case InteractionPose.Grab:
                    return HandModel.GetLeapHand().GrabStrength > 0.7f;

                case InteractionPose.LongPinch:
                    bool longPinch = false;
                    if (HandModel.GetLeapHand().IsPinching() && lastPinchState && Time.time - lastPinchTime > lengthGrabPinchTime)
                        longPinch = true;

                    else if (!HandModel.GetLeapHand().IsPinching() && lastPinchState)
                        lastPinchState = false;

                    else if (HandModel.GetLeapHand().IsPinching() && !lastPinchState)
                    {
                        lastPinchState = true;
                        lastPinchTime = Time.time;
                    }
                    return longPinch;

                case InteractionPose.LongGrab:
                    bool longGrab = false;

                    if (HandModel.GetLeapHand().GrabStrength > 0.7f && lastGrabState && Time.time - lastGrabTime > lengthGrabPinchTime)
                        longPinch = true;

                    else if (!(HandModel.GetLeapHand().GrabStrength > 0.7f) && lastGrabState)
                        lastGrabState = false;

                    else if (HandModel.GetLeapHand().GrabStrength > 0.7f && !lastGrabState)
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