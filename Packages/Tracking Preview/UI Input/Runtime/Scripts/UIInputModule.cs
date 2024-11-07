/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Leap.InputModule
{
    /// <summary>
    /// An InputModule that supports the use of Leap Motion tracking data for manipulating Unity UI controls.
    /// </summary>
    public class UIInputModule : BaseInputModule, IInputModuleEventHandler
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

        //Should the pointer cursor be shown for direct interaction
        [Tooltip("Show pointer cursor for direct interaction")]
        [SerializeField]
        private bool showDirectPointerCursor = true;

        public bool ShowDirectPointerCursor => showDirectPointerCursor;

        //The LeapProvider providing tracking data to the scene.
        [Tooltip("The main camera used for calculating interactions.")]
        [SerializeField]
        private Camera mainCamera;

        /// <summary>
        /// The main camera reference
        /// </summary>
        public Camera MainCamera
        {
            get => mainCamera;
            set => mainCamera = value;
        }

        //The LeapProvider providing tracking data to the scene.
        [Tooltip("The current Leap Data Provider for the scene.")]
        [SerializeField]
        private LeapProvider leapDataProvider;

        /// <summary>
        /// A reference to the source of hand tracking data to use, the 'leap provider'
        /// </summary>
        public LeapProvider LeapDataProvider
        {
            get => leapDataProvider;
            set => leapDataProvider = value;
        }

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

        //Event related data
        [SerializeField] private PointerElement _pointerLeft;
        [SerializeField] private PointerElement _pointerRight;

        //Misc. Objects
        private bool _prevTouchingMode;
        private IProjectionOriginProvider _projectionOriginProvider;
        public IProjectionOriginProvider ProjectionOriginProvider => _projectionOriginProvider;

        [SerializeField] private bool _drawGizmos = false;

        #endregion

        #region Unity methods

        /// <summary>
        /// Initialisation
        /// </summary>
        protected override void Awake()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            //We must have legacy input enabled, or be using an older Unity version
            enabled = false;
            return;
#endif

            //Find and apply LSP/Camera if not already
            if (leapDataProvider == null)
            {
                leapDataProvider = Hands.Provider;
                if (leapDataProvider == null || !leapDataProvider.isActiveAndEnabled)
                {
                    Debug.LogError("Failed to find active LeapProvider", leapDataProvider);
                    enabled = false;
                    return;
                }
            }
            if (mainCamera == null)
            {
                if (leapDataProvider != null && leapDataProvider is LeapXRServiceProvider)
                    mainCamera = ((LeapXRServiceProvider)leapDataProvider).mainCamera;
                if (mainCamera == null)
                {
                    Debug.LogError("Failed to find main camera", mainCamera);
                    enabled = false;
                    return;
                }
            }

            base.Awake();
        }
        protected override void Start()
        {
            var shoulderProjectionOriginProvider = new ShoulderProjectionOriginProvider(mainCamera);
            _projectionOriginProvider = shoulderProjectionOriginProvider;

            //Assign mainCamera to Canvases
            var canvases = Resources.FindObjectsOfTypeAll<Canvas>();
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i].worldCamera == null)
                {
                    canvases[i].worldCamera = mainCamera;
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
            if (!_drawGizmos)
            {
                return;
            }
            if (_projectionOriginProvider != null)
            {
                _projectionOriginProvider.DrawGizmos();

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
        /// Exposes protected static method, only for use by PointerElement so that it does not need to be a nested class
        /// </summary>
        /// <param name="candidates">Potential candidates</param>
        /// <returns></returns>
        public new static RaycastResult FindFirstRaycast(List<RaycastResult> candidates)
        {
            return BaseInputModule.FindFirstRaycast(candidates);
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