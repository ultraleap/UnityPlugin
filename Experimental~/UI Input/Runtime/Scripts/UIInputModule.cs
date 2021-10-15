/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Leap.Unity.InputModule
{
    /// <summary>
    /// An InputModule that supports the use of Leap Motion tracking data for manipulating Unity UI controls.
    /// </summary>
    public class UIInputModule : BaseInputModule, IInputModuleSettings, IInputModuleEventHandler
    {
        // //Events
        EventHandler<Vector3> IInputModuleEventHandler.OnClickDown { get; set; }
        EventHandler<Vector3> IInputModuleEventHandler.OnClickUp { get; set; }
        EventHandler<Vector3> IInputModuleEventHandler.OnBeginHover { get; set; }
        EventHandler<Vector3> IInputModuleEventHandler.OnEndHover { get; set; }
        EventHandler<Vector3> IInputModuleEventHandler.OnBeginMissed { get; set; }
        EventHandler<Vector3> IInputModuleEventHandler.OnEndMissed { get; set; }
        
        #region Properties

        //General Interaction Parameters
        [Header("Interaction Setup")]

        //The mode to use for interaction. The default mode is tactile. The projective mode is considered experimental.
        [Tooltip("The interaction mode that the Input Module will be restricted to.")]
        [SerializeField]
        private InteractionCapability interactionMode = InteractionCapability.Both;

        public InteractionCapability InteractionMode => interactionMode;

        //The LeapProvider providing tracking data to the scene.
        [Tooltip("The main camera used for calculating interactions.")]
        [SerializeField]
        private Camera mainCamera;

        //The LeapProvider providing tracking data to the scene.
        [Tooltip("The current Leap Data Provider for the scene.")]
        [SerializeField]
        private LeapProvider leapDataProvider;

        //An optional component that will be used to detect pinch motions if set.
        //Primarily used for projective or hybrid interaction modes (under experimental features).
        [Tooltip("An optional alternate detector for pinching on the left hand.")]
        [SerializeField]
        private PinchDetector leftHandDetector;

        //An optional component that will be used to detect pinch motions if set.
        //Primarily used for projective or hybrid interaction modes (under experimental features).
        [Tooltip("An optional alternate detector for pinching on the right hand.")]
        [SerializeField]
        private PinchDetector rightHandDetector;

        //Calibration Setup

        //When not using a PinchDetector, the distance in mm that the tip of the thumb and forefinger should be to activate selection during projective interaction.
        [Tooltip("When not using a PinchDetector, the distance in mm that the tip of the thumb and forefinger should be to activate selection during projective interaction.")]
        [SerializeField] private float pinchingThreshold = 30f;
        public float PinchingThreshold => pinchingThreshold;

        //The distance from the base of a UI element that tactile interaction is triggered.
        [Tooltip("The distance from the base of a UI element that tactile interaction is triggered.")]
        [SerializeField]
        private float tactilePadding = 0.005f;
        public float TactilePadding => tactilePadding;

        //The distance from the canvas at which to switch to projective mode.
        [Tooltip("The distance from the base of a UI element that interaction switches from Projective-Pointer based to Touch based.")]
        [SerializeField] private float projectiveToTactileTransitionDistance = 0.4f;
        public float ProjectiveToTactileTransitionDistance => projectiveToTactileTransitionDistance;
        
        //Trigger a Hover Event when switching between UI elements.
        [Tooltip("Trigger a Hover Event when switching between UI elements.")]
        [SerializeField] private bool triggerHoverOnElementSwitch;
        public bool TriggerHoverOnElementSwitch => triggerHoverOnElementSwitch;

        //Transform the Interaction Pointer to allow the Module to work in a non-stationary reference frame.
        [Tooltip("Transform the Interaction Pointer to allow the Module to work in a non-stationary reference frame.")]
        [SerializeField] private bool movingReferenceFrame;
        
        //Pointer Setup
        
        //The sprite for the cursor.
        [Tooltip("The sprite used to represent your pointers during projective interaction.")]
        [SerializeField]
        private Sprite pointerSprite;
        public Sprite PointerSprite => pointerSprite;

        //The cursor material.
        [Tooltip("The material to be instantiated for your pointers during projective interaction.")]
        [SerializeField]
        private Material pointerMaterial;
        
        //The size of the pointer in world coordinates with respect to the distance between the cursor and the camera.
        [Tooltip("The size of the pointer in world coordinates with respect to the distance between the cursor and the camera.")]
        [SerializeField] private AnimationCurve pointerDistanceScale = AnimationCurve.Linear(0f, 0.1f, 6f, 1f);
        public AnimationCurve PointerDistanceScale => pointerDistanceScale;

        //The size of the pointer in world coordinates with respect to the distance between the thumb and forefinger.
        [Tooltip("The size of the pointer in world coordinates with respect to the distance between the thumb and forefinger.")]
        [SerializeField] private AnimationCurve pointerPinchScale = AnimationCurve.Linear(30f, 0.6f, 70f, 1.1f);
        public AnimationCurve PointerPinchScale => pointerPinchScale;

        //The color for the cursor when it is not in a special state.
        [Tooltip("The color of the pointer when it is hovering over blank canvas.")]
        [SerializeField]
        private Color standardColor = Color.white;
        public Color StandardColor => standardColor;

        //The color for the cursor when it is hovering over a control.
        [Tooltip("The color of the pointer when it is hovering over any other UI element.")]
        [SerializeField]
        private Color hoveringColor = Color.green;
        public Color HoveringColor => hoveringColor;

        //The color for the cursor when it is actively interacting with a control.
        [Tooltip("The color of the pointer when it is triggering a UI element.")]
        [SerializeField]
        private Color triggeringColor = Color.gray;
        public Color TriggeringColor => triggeringColor;

        //The color for the cursor when it is touching or triggering a non-active part of the UI (such as the canvas).
        [Tooltip("The color of the pointer when it is triggering blank canvas.")]
        [SerializeField]
        private Color triggerMissedColor = Color.gray;
        public Color TriggerMissedColor => triggerMissedColor;

        //Render a smaller pointer inside of the main pointer.
        [Tooltip("Render a smaller pointer inside of the main pointer.")]
        [SerializeField] private bool innerPointer = true;
        public bool InnerPointer => innerPointer;

        //The Opacity of the Inner Pointer relative to the Primary Pointer.
        [Tooltip("The Opacity of the Inner Pointer relative to the Primary Pointer.")]
        [SerializeField] private float innerPointerOpacityScalar = 0.77f;
        public float InnerPointerOpacityScalar => innerPointerOpacityScalar;


        private PointerElement _pointerLeft;
        private PointerElement _pointerRight;

        //Values from the previous frame
        private bool _prevTouchingMode;

        private IProjectionOriginProvider _projectionOriginProvider;

        #endregion

        #region Unity methods

        /// <summary>
        /// Initialisation
        /// </summary>
        protected override void Start()
        {            
            if (mainCamera == null)
            {
                Debug.LogError("Main Camera must be set as it is used in interaction calculations", mainCamera);
            }
            
            if (leapDataProvider == null)
            {
                Debug.LogError("Leap Data Provider must be set to get tracking data", leapDataProvider);
            }

            var shoulderProjectionOriginProvider = new ShoulderProjectionOriginProvider(Camera.main);
            _projectionOriginProvider = shoulderProjectionOriginProvider;

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

            //Set Projective/Tactile Modes
            if (interactionMode == InteractionCapability.Indirect)
            {
                projectiveToTactileTransitionDistance = -float.MaxValue;
            }
            else if (interactionMode == InteractionCapability.Direct)
            {
                projectiveToTactileTransitionDistance = float.MaxValue;
            }

            _pointerLeft = new PointerElement(Chirality.Left, mainCamera, eventSystem, leapDataProvider, this, this, leftHandDetector);
            _pointerLeft.Initialise(transform, pointerSprite, pointerMaterial, innerPointer);

            _pointerRight = new PointerElement(Chirality.Right, mainCamera, eventSystem, leapDataProvider, this, this, rightHandDetector);
            _pointerRight.Initialise(transform, pointerSprite, pointerMaterial, innerPointer);
        }

        private void Update()
        {
            _projectionOriginProvider?.Update();
        }

        /// <summary>
        /// Process is called by UI system to process events
        /// </summary>
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

            _projectionOriginProvider?.Process();

            // Begin Processing Each Hand/Pointer
            _pointerLeft.Process(GetHand(Chirality.Left), _projectionOriginProvider);
            _pointerRight.Process(GetHand(Chirality.Right), _projectionOriginProvider);
        }
        
        /// <summary>
        /// Returns hand that matches specified chirality, if one exists
        /// </summary>
        /// <param name="chirality">Chirality of the required hand</param>
        /// <returns>Matching hand, if one exist else null</returns>
        private Hand GetHand(Chirality chirality)
        {
            foreach (var current in leapDataProvider.CurrentFrame.Hands)
            {
                if (chirality == Chirality.Left && current.IsLeft)
                {
                    return current;
                }

                if (chirality == Chirality.Right && current.IsRight)
                {
                    return current;
                }
            }

            return null;
        }
        
        private void OnDrawGizmos()
        {
            if (_projectionOriginProvider != null)
            {
                Debug.DrawRay(_projectionOriginProvider.ProjectionOriginLeft, _projectionOriginProvider.CurrentRotation * Vector3.forward * 5f, Color.green);
                Debug.DrawRay(_projectionOriginProvider.ProjectionOriginRight, _projectionOriginProvider.CurrentRotation * Vector3.forward * 5f, Color.green);

                Gizmos.DrawSphere(_projectionOriginProvider.ProjectionOriginLeft, 0.1f);
                Gizmos.DrawSphere(_projectionOriginProvider.ProjectionOriginRight, 0.1f);
                Gizmos.DrawSphere(mainCamera.transform.position, 0.1f);
            }
        }

        /// <summary>
        /// Only activate the InputModule when there are hands in the scene.
        /// </summary>
        /// <returns>True if hands are in the scene, otherwise false</returns>
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

        private void SendUpdateEventToSelectedObject()
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
