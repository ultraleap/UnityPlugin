/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Leap.Unity.InputModule
{
    /// <summary>
    /// An InputModule that supports the use of Leap Motion tracking data for manipulating Unity UI controls.
    /// </summary>
    public class LeapInputModule : BaseInputModule, IInputModuleSettings, IInputModuleEventHandler
    {
        // //Events
        EventHandler<Vector3> IInputModuleEventHandler.OnClickDown { get; set; }
        EventHandler<Vector3> IInputModuleEventHandler.OnClickUp { get; set; }
        EventHandler<Vector3> IInputModuleEventHandler.OnBeginHover { get; set; }
        EventHandler<Vector3> IInputModuleEventHandler.OnEndHover { get; set; }
        EventHandler<Vector3> IInputModuleEventHandler.OnBeginMissed { get; set; }
        EventHandler<Vector3> IInputModuleEventHandler.OnEndMissed { get; set; }
        EventHandler<Vector3> IInputModuleEventHandler.OnEnvironmentPinch { get; set; }
        
        #region Properties

        //General Interaction Parameters
        [Header(" Interaction Setup")]

        //The LeapProvider providing tracking data to the scene.
        [Tooltip("The current Leap Data Provider for the scene.")]
        [SerializeField] Camera mainCamera;


        //The LeapProvider providing tracking data to the scene.
        [Tooltip("The current Leap Data Provider for the scene.")]
        [SerializeField] LeapProvider leapDataProvider;

        //An optional component that will be used to detect pinch motions if set.
        //Primarily used for projective or hybrid interaction modes (under experimental features).
        [Tooltip("An optional alternate detector for pinching on the left hand.")]
        [SerializeField]  PinchDetector leftHandDetector;

        //An optional component that will be used to detect pinch motions if set.
        //Primarily used for projective or hybrid interaction modes (under experimental features).
        [Tooltip("An optional alternate detector for pinching on the right hand.")]
        [SerializeField] PinchDetector rightHandDetector;

        //The number of pointers to create. By default, one pointer is created for each hand.
        [Tooltip("How many hands and pointers the Input Module should allocate for.")]
        int numberOfPointers = 2;

        //Customizable Pointer Parameters
        [Header(" Pointer Setup")]

        //The sprite for the cursor.
        [Tooltip("The sprite used to represent your pointers during projective interaction.")]
        [SerializeField] Sprite pointerSprite;
        public Sprite PointerSprite => pointerSprite;

        //The cursor material.
        [Tooltip("The material to be instantiated for your pointers during projective interaction.")]
        [SerializeField] Material pointerMaterial;

        //The color for the cursor when it is not in a special state.
        [Tooltip("The color of the pointer when it is hovering over blank canvas.")]
        [SerializeField] Color standardColor = Color.white;
        public Color StandardColor => standardColor;

        //The color for the cursor when it is hovering over a control.
        [Tooltip("The color of the pointer when it is hovering over any other UI element.")]
        [SerializeField] Color hoveringColor = Color.green;
        public Color HoveringColor => hoveringColor;

        //The color for the cursor when it is actively interacting with a control.
        [Tooltip("The color of the pointer when it is triggering a UI element.")]
        [SerializeField] Color triggeringColor = Color.gray;
        public Color TriggeringColor => triggeringColor;

        //The color for the cursor when it is touching or triggering a non-active part of the UI (such as the canvas).
        [Tooltip("The color of the pointer when it is triggering blank canvas.")]
        [SerializeField] Color triggerMissedColor = Color.gray;
        public Color TriggerMissedColor => triggerMissedColor;

        //Advanced Options
        [Header(" Advanced Options")]

        [Tooltip("Whether or not to show Advanced Options in the Inspector.")]
        [SerializeField] bool showAdvancedOptions;
        public bool ShowAdvancedOptions => showAdvancedOptions;

        //The distance from the base of a UI element that tactile interaction is triggered.
        [Tooltip("The distance from the base of a UI element that tactile interaction is triggered.")]
        [SerializeField] float tactilePadding = 0.005f;
        public float TactilePadding => tactilePadding;
        
        [Tooltip("Whether or not to show unsupported Experimental Options in the Inspector.")]
        [SerializeField] bool showExperimentalOptions;
        public bool ShowExperimentalOptions => showExperimentalOptions;

        //The mode to use for interaction. The default mode is tactile. The projective mode is considered experimental.
        [Tooltip("The interaction mode that the Input Module will be restricted to.")]
        [SerializeField] InteractionCapability interactionMode = InteractionCapability.Tactile;

        public InteractionCapability InteractionMode => interactionMode;

        //The distance from the canvas at which to switch to projective mode.
        [Tooltip("The distance from the base of a UI element that interaction switches from Projective-Pointer based to Touch based.")]
        [SerializeField] float projectiveToTactileTransitionDistance = 0.4f;
        public float ProjectiveToTactileTransitionDistance => projectiveToTactileTransitionDistance;

        //The size of the pointer in world coordinates with respect to the distance between the cursor and the camera.
        [Tooltip("The size of the pointer in world coordinates with respect to the distance between the cursor and the camera.")]
        [SerializeField] AnimationCurve pointerDistanceScale = AnimationCurve.Linear(0f, 0.1f, 6f, 1f);
        public AnimationCurve PointerDistanceScale => pointerDistanceScale;

        //The size of the pointer in world coordinates with respect to the distance between the thumb and forefinger.
        [Tooltip("The size of the pointer in world coordinates with respect to the distance between the thumb and forefinger.")]
        [SerializeField] AnimationCurve pointerPinchScale = AnimationCurve.Linear(30f, 0.6f, 70f, 1.1f);
        public AnimationCurve PointerPinchScale => pointerPinchScale;

        //When not using a PinchDetector, the distance in mm that the tip of the thumb and forefinger should be to activate selection during projective interaction.
        [Tooltip("When not using a PinchDetector, the distance in mm that the tip of the thumb and forefinger should be to activate selection during projective interaction.")]
        [SerializeField] float pinchingThreshold = 30f;
        public float PinchingThreshold => pinchingThreshold;

        //Create a pointer for each finger.
        [Tooltip("Create a pointer for each finger.")]
        [SerializeField] bool perFingerPointer;
        public bool PointerPerFinger => perFingerPointer;

        //Render the pointer onto the environment.
        [Tooltip("Render the pointer onto the environment.")]
        [SerializeField] bool environmentPointer; // Rename to renderPointersOnEnvironment
        public bool RenderEnvironmentPointer => environmentPointer;

        //Render a smaller pointer inside of the main pointer.
        [Tooltip("Render a smaller pointer inside of the main pointer.")]
        [SerializeField] bool innerPointer = true;
        public bool InnerPointer => innerPointer;

        //The Opacity of the Inner Pointer relative to the Primary Pointer.
        [Tooltip("The Opacity of the Inner Pointer relative to the Primary Pointer.")]
        [SerializeField] float innerPointerOpacityScalar = 0.77f;
        public float InnerPointerOpacityScalar => innerPointerOpacityScalar;

        //Trigger a Hover Event when switching between UI elements.
        [Tooltip("Trigger a Hover Event when switching between UI elements.")]
        [SerializeField] bool triggerHoverOnElementSwitch;
        public bool TriggerHoverOnElementSwitch => triggerHoverOnElementSwitch;

        //If the ScrollView still doesn't work even after disabling RaycastTarget on the intermediate layers.
        [Tooltip("If the ScrollView still doesn't work even after disabling RaycastTarget on the intermediate layers.")]
        [SerializeField] bool overrideScrollViewClicks;
        public bool OverrideScrollViewClicks => overrideScrollViewClicks;

        //Retransform the Interaction Pointer to allow the Module to work in a non-stationary reference frame.
        [Tooltip("Retransform the Interaction Pointer to allow the Module to work in a non-stationary reference frame.")]
        [SerializeField] bool movingReferenceFrame;

        PointerElement[] _pointersCollection;

        //Values from the previous frame
        bool _prevTouchingMode;

        //Misc. Objects
        Canvas[] _canvases;
        IProjectionOriginProvider projectionOriginProvider;

        #endregion

        #region Unity methods

        /// <summary>
        /// Initialisation
        /// </summary>
        protected override void Start()
        {
            base.Start();

            ShoulderProjectionOriginProvider shoulderProjectionOriginProvider = new ShoulderProjectionOriginProvider(Camera.main);
            projectionOriginProvider = shoulderProjectionOriginProvider;

            if (leapDataProvider == null)
            {
                leapDataProvider = FindObjectOfType<LeapProvider>();
                if (leapDataProvider == null || !leapDataProvider.isActiveAndEnabled)
                {
                    Debug.LogError("Cannot use LeapImageRetriever if there is no LeapProvider!");
                    enabled = false;
                    return;
                }
            }

            _canvases = Resources.FindObjectsOfTypeAll<Canvas>();

            //Set Projective/Tactile Modes
            if (interactionMode == InteractionCapability.Projective)
            {
                projectiveToTactileTransitionDistance = -float.MaxValue;
            }
            else if (interactionMode == InteractionCapability.Tactile)
            {
                projectiveToTactileTransitionDistance = float.MaxValue;
            }

            //Initialize the Pointers for Projective Interaction
            if (perFingerPointer)
            {
                numberOfPointers = 10;
            }

            _pointersCollection = new PointerElement[numberOfPointers];
            for (int index = 0; index < numberOfPointers; index++)
            {
                var pointer = _pointersCollection[index] = new PointerElement(mainCamera, eventSystem, leapDataProvider, index, this, this, leftHandDetector, rightHandDetector);
                pointer.Initialise(index, transform, pointerSprite, pointerMaterial, innerPointer);
            }
        }

        void Update()
        {
            projectionOriginProvider?.Update();
        }

        //Process is called by UI system to process events
        public override void Process()
        {
            if (movingReferenceFrame)
            {
                var provider = leapDataProvider as LeapServiceProvider;
                if (provider != null)
                {
                    provider.RetransformFrames();
                }
            }

            // Send update events if there is a selected object
            // This is important for InputField to receive keyboard events
            SendUpdateEventToSelectedObject();

            projectionOriginProvider?.Process();

            // Begin Processing Each Hand/Pointer
            for (var pointerIndex = 0; pointerIndex < numberOfPointers; pointerIndex++)
            {
                _pointersCollection[pointerIndex].Process(projectionOriginProvider);
            }
        }

        void OnDrawGizmos()
        {
            if (projectionOriginProvider != null)
            {
                Debug.DrawRay(projectionOriginProvider.ProjectionOriginLeft, projectionOriginProvider.CurrentRotation * Vector3.forward * 5f, Color.green);
                Debug.DrawRay(projectionOriginProvider.ProjectionOriginRight, projectionOriginProvider.CurrentRotation * Vector3.forward * 5f, Color.green);

                Gizmos.DrawSphere(projectionOriginProvider.ProjectionOriginLeft, 0.1f);
                Gizmos.DrawSphere(projectionOriginProvider.ProjectionOriginRight, 0.1f);
                Gizmos.DrawSphere(mainCamera.transform.position, 0.1f);

                foreach (var pointer in _pointersCollection)
                {
                    var pointerPosition = pointer.Pointer.transform.position;
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(projectionOriginProvider.ProjectionOriginLeft, pointerPosition);
                    Gizmos.DrawLine(projectionOriginProvider.ProjectionOriginRight, pointerPosition);
                    Gizmos.color = Color.white;
                }
            }
        }

        /** Only activate the InputModule when there are hands in the scene. */
        public override bool ShouldActivateModule()
        {
            return leapDataProvider.CurrentFrame != null && leapDataProvider.CurrentFrame.Hands.Count > 0 &&
                   base.ShouldActivateModule();
        }



        /// <summary>
        ///  Delegates through to the base class method. Enables pointer class to call into this
        /// </summary>
        /// <param name="eventData"></param>
        /// <param name="newEnterTarget"></param>
        public void HandlePointerExitAndEnterProxy(PointerEventData eventData, GameObject newEnterTarget)
        {
            base.HandlePointerExitAndEnter(eventData, newEnterTarget);
        }

        /// <summary>
        /// Delegates through to the base class method. Enables pointer class to call into this
        /// </summary>
        /// <param name="candidates">Raycast candidates</param>
        public RaycastResult FindFirstRaycastProxy(List<RaycastResult> candidates)
        {
            return FindFirstRaycast(candidates);
        }

        #endregion

        #region Private methods

        void SendUpdateEventToSelectedObject()
        {
            if (eventSystem.currentSelectedGameObject == null)
            {
                return;
            }

            BaseEventData data = GetBaseEventData();
            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);
        }

        #endregion
    }
}
