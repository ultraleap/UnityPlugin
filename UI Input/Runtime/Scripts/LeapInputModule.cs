/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Leap.Unity.InputModule
{
    /** An InputModule that supports the use of Leap Motion tracking data for manipulating Unity UI controls. */
    public class LeapInputModule : BaseInputModule
    {
        #region Properties

        //General Interaction Parameters
        [Header(" Interaction Setup")]

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
        public PinchDetector rightHandDetector;

        //The number of pointers to create. By default, one pointer is created for each hand.
        [Tooltip("How many hands and pointers the Input Module should allocate for.")]
        int _numberOfPointers = 2;


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

        //The color for the cursor when it is hovering over a control.
        [Tooltip("The color of the pointer when it is hovering over any other UI element.")]
        [SerializeField] Color hoveringColor = Color.green;

        //The color for the cursor when it is actively interacting with a control.
        [Tooltip("The color of the pointer when it is triggering a UI element.")]
        [SerializeField] Color triggeringColor = Color.gray;

        //The color for the cursor when it is touching or triggering a non-active part of the UI (such as the canvas).
        [Tooltip("The color of the pointer when it is triggering blank canvas.")]
        [SerializeField] Color triggerMissedColor = Color.gray;

        //Advanced Options
        [Header(" Advanced Options")]

        [Tooltip("Whether or not to show Advanced Options in the Inspector.")]
        [SerializeField] bool showAdvancedOptions;
        public bool ShowAdvancedOptions => showAdvancedOptions;

        //The distance from the base of a UI element that tactile interaction is triggered.
        [Tooltip("The distance from the base of a UI element that tactile interaction is triggered.")]
        [SerializeField] float tactilePadding = 0.005f;

        //The sound that is played when the pointer transitions from canvas to element.
        [Tooltip("The sound that is played when the pointer transitions from canvas to element.")]
        [SerializeField] AudioClip beginHoverSound;

        //The sound that is played when the pointer transitions from canvas to element.
        [Tooltip("The sound that is played when the pointer transitions from canvas to element.")]
        [SerializeField] AudioClip endHoverSound;

        //The sound that is played when the pointer triggers a UI element.
        [Tooltip("The sound that is played when the pointer triggers a UI element.")]
        [SerializeField] AudioClip beginTriggerSound;

        //The sound that is played when the pointer triggers a UI element.
        [Tooltip("The sound that is played when the pointer triggers a UI element.")]
        [SerializeField] AudioClip endTriggerSound;

        //The sound that is played when the pointer triggers blank canvas.
        [Tooltip("The sound that is played when the pointer triggers blank canvas.")]
        [SerializeField] AudioClip beginMissedSound;

        //The sound that is played when the pointer triggers blank canvas.
        [Tooltip("The sound that is played when the pointer triggers blank canvas.")]
        [SerializeField] AudioClip endMissedSound;

        //The sound that is played while the pointer is dragging an object.
        [Tooltip("The sound that is played while the pointer is dragging an object.")]
        [SerializeField] AudioClip dragLoopSound;

        // Event delegates triggered by Input
        [Serializable]
        public class PositionEvent : UnityEvent<Vector3> { }


        [Header(" Event Setup")]

        //The event that is triggered upon clicking on a non-canvas UI element.
        [Tooltip("The event that is triggered upon clicking on a non-canvas UI element.")]
        [SerializeField] PositionEvent onClickDown;

        //The event that is triggered upon lifting up from a non-canvas UI element (Not 1:1 with onClickDown!)
        [Tooltip(
            "The event that is triggered upon lifting up from a non-canvas UI element (Not 1:1 with onClickDown!)")]
        [SerializeField] PositionEvent onClickUp;

        //The event that is triggered upon hovering over a non-canvas UI element.
        [Tooltip("The event that is triggered upon hovering over a non-canvas UI element.")]
        [SerializeField] PositionEvent onHover;

        //The event that is triggered while holding down a non-canvas UI element.
        [Tooltip("The event that is triggered while holding down a non-canvas UI element.")]
        [SerializeField] PositionEvent whileClickHeld;

        [Tooltip("Whether or not to show unsupported Experimental Options in the Inspector.")]
        [SerializeField] bool showExperimentalOptions;
        public bool ShowExperimentalOptions => showExperimentalOptions;

        /// Defines the interaction modes :
        ///
        /// - Hybrid: Both tactile and projective interaction. The active mode depends on the ProjectiveToTactileTransitionDistance value.
        /// - Tactile: The user must physically touch the controls.
        /// - Projective: A cursor is projected from the user's knuckle.
        public enum InteractionCapability
        {
            Hybrid,
            Tactile,
            Projective
        };

        //The mode to use for interaction. The default mode is tactile. The projective mode is considered experimental.
        [Tooltip("The interaction mode that the Input Module will be restricted to.")]
        [SerializeField] InteractionCapability interactionMode = InteractionCapability.Tactile;

        public InteractionCapability InteractionMode => interactionMode;

        //The distance from the canvas at which to switch to projective mode.
        [Tooltip("The distance from the base of a UI element that interaction switches from Projective-Pointer based to Touch based.")]
        [SerializeField] float projectiveToTactileTransitionDistance = 0.4f;

        //The size of the pointer in world coordinates with respect to the distance between the cursor and the camera.
        [Tooltip(
            "The size of the pointer in world coordinates with respect to the distance between the cursor and the camera.")]
        [SerializeField] AnimationCurve pointerDistanceScale = AnimationCurve.Linear(0f, 0.1f, 6f, 1f);

        //The size of the pointer in world coordinates with respect to the distance between the thumb and forefinger.
        [Tooltip("The size of the pointer in world coordinates with respect to the distance between the thumb and forefinger.")]
        [SerializeField] AnimationCurve pointerPinchScale = AnimationCurve.Linear(30f, 0.6f, 70f, 1.1f);

        //When not using a PinchDetector, the distance in mm that the tip of the thumb and forefinger should be to activate selection during projective interaction.
        [Tooltip("When not using a PinchDetector, the distance in mm that the tip of the thumb and forefinger should be to activate selection during projective interaction.")]
        [SerializeField] float pinchingThreshold = 30f;

        //Create a pointer for each finger.
        [Tooltip("Create a pointer for each finger.")]
        [SerializeField] bool perFingerPointer;

        //Render the pointer onto the environment.
        [Tooltip("Render the pointer onto the environment.")]
        [SerializeField] bool environmentPointer;
        public bool EnvironmentPointer => environmentPointer;

        //The event that is triggered while pinching to a point in the environment.
        [Tooltip("The event that is triggered while pinching to a point in the environment.")]
        public PositionEvent environmentPinch;

        //Render a smaller pointer inside of the main pointer.
        [Tooltip("Render a smaller pointer inside of the main pointer.")]
        [SerializeField] bool innerPointer = true;
        public bool InnerPointer => innerPointer;

        //The Opacity of the Inner Pointer relative to the Primary Pointer.
        [Tooltip("The Opacity of the Inner Pointer relative to the Primary Pointer.")]
        [SerializeField] float innerPointerOpacityScalar = 0.77f;

        //Trigger a Hover Event when switching between UI elements.
        [Tooltip("Trigger a Hover Event when switching between UI elements.")]
        [SerializeField] bool triggerHoverOnElementSwitch;

        //If the ScrollView still doesn't work even after disabling RaycastTarget on the intermediate layers.
        [Tooltip("If the ScrollView still doesn't work even after disabling RaycastTarget on the intermediate layers.")]
        [SerializeField] bool overrideScrollViewClicks;

        //Draw the raycast for projective interaction.
        [Tooltip("Draw the raycast for projective interaction.")]
        [SerializeField] bool drawDebug;

        //Retract compressible widgets when not using Tactile Interaction.
        [Tooltip("Retract compressible widgets when not using Tactile Interaction.")]
        [SerializeField] bool retractUI;

        //Retransform the Interaction Pointer to allow the Module to work in a non-stationary reference frame.
        [Tooltip("Retransform the Interaction Pointer to allow the Module to work in a non-stationary reference frame.")]
        [SerializeField] bool movingReferenceFrame;

        //Event related data
        //private Camera EventCamera;
        PointerEventData[] _pointEvents;
        PointerStates[] _pointerState;
        Transform[] _pointers;
        Transform[] _innerPointers;
        LineRenderer[] _pointerLines;

        //Object the pointer is hovering over
        GameObject[] _currentOverGo;

        //Values from the previous frame
        PointerStates[] _prevState;
        Vector2[] _prevScreenPosition;
        Vector2[] _dragBeginPosition;
        bool[] _prevTriggeringInteraction;
        bool _prevTouchingMode;
        GameObject[] _prevOverGo;
        float[] _timeEnteredCanvas;

        //Misc. Objects
        Canvas[] _canvases;
        Quaternion _currentRotation;
        AudioSource _soundPlayer;
        GameObject[] _currentGo;
        GameObject[] _currentGoing;
        Vector3 _oldCameraPos = Vector3.zero;

        Quaternion _oldCameraRot = Quaternion.identity;

        //private float OldCameraFoV;
        bool _forceProjective;
        bool _forceTactile;

        //Queue of Spheres to Debug Draw
        Queue<Vector3> _debugSphereQueue;

        #endregion

        enum PointerStates
        {
            OnCanvas,
            OnElement,
            PinchingToCanvas,
            PinchingToElement,
            NearCanvas,
            TouchingCanvas,
            TouchingElement,
            OffCanvas
        };

        //Initialization
        protected override void Start()
        {
            base.Start();

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
                _forceTactile = false;
                _forceProjective = true;
            }
            else if (interactionMode == InteractionCapability.Tactile)
            {
                projectiveToTactileTransitionDistance = float.MaxValue;
                _forceTactile = true;
                _forceProjective = false;
            }

            //Initialize the Pointers for Projective Interaction
            if (perFingerPointer)
            {
                _numberOfPointers = 10;
            }

            _pointers = new Transform[_numberOfPointers];
            _innerPointers = new Transform[_numberOfPointers];
            _pointerLines = new LineRenderer[_numberOfPointers];
            for (int index = 0; index < _pointers.Length; index++)
            {
                //Create the Canvas to render the Pointer on
                GameObject pointer = new GameObject("Pointer " + index);
                SpriteRenderer spriteRenderer = pointer.AddComponent<SpriteRenderer>();
                spriteRenderer.sortingOrder = 1000;

                //Add your sprite to the Sprite Renderer
                spriteRenderer.sprite = pointerSprite;
                spriteRenderer.material =
                    Instantiate(
                        pointerMaterial); //Make sure to instantiate the material so each pointer can be modified independently

                if (drawDebug)
                {
                    _pointerLines[index] = pointer.AddComponent<LineRenderer>();
                    _pointerLines[index].material = Instantiate(pointerMaterial);
                    _pointerLines[index].material.color = new Color(0f, 0f, 0f, 0f);
#if UNITY_5_5_OR_NEWER
#if UNITY_5_6_OR_NEWER
                    _pointerLines[index].positionCount = 2;
#else
                    _pointerLines[index].numPositions = 2;
#endif
                    _pointerLines[index].startWidth = 0.001f;
                    _pointerLines[index].endWidth = 0.001f;
#else
                    _pointerLines[index].SetVertexCount(2);
                    _pointerLines[index].SetWidth(0.001f, 0.001f);
#endif
                }

                _pointers[index] = pointer.GetComponent<Transform>();
                _pointers[index].parent = transform;
                pointer.SetActive(false);

                if (innerPointer)
                {
                    //Create the Canvas to render the Pointer on
                    GameObject innerPointer = new GameObject("Pointer " + index);
                    spriteRenderer = innerPointer.AddComponent<SpriteRenderer>();
                    spriteRenderer.sortingOrder = 1000;

                    //Add your sprite to the Canvas
                    spriteRenderer.sprite = pointerSprite;

                    spriteRenderer.material = Instantiate(pointerMaterial);

                    _innerPointers[index] = innerPointer.GetComponent<Transform>();
                    _innerPointers[index].parent = transform;
                    innerPointer.SetActive(false);
                }
            }

            //Initialize our Sound Player
            _soundPlayer = gameObject.AddComponent<AudioSource>();

            //Initialize the arrays that store persistent objects per pointer
            _pointEvents = new PointerEventData[_numberOfPointers];
            _pointerState = new PointerStates[_numberOfPointers];
            _currentOverGo = new GameObject[_numberOfPointers];
            _prevOverGo = new GameObject[_numberOfPointers];
            _currentGo = new GameObject[_numberOfPointers];
            _currentGoing = new GameObject[_numberOfPointers];
            _prevTriggeringInteraction = new bool[_numberOfPointers];
            _prevScreenPosition = new Vector2[_numberOfPointers];
            _dragBeginPosition = new Vector2[_numberOfPointers];
            _prevState = new PointerStates[_numberOfPointers];
            _timeEnteredCanvas = new float[_numberOfPointers];

            //Used for calculating the origin of the Projective Interactions
            if (Camera.main != null)
            {
                _currentRotation = Camera.main.transform.rotation;
            }
            else
            {
                Debug.LogAssertion("Tag your Main Camera with 'MainCamera' for the UI Module");
            }

            //Initializes the Queue of Spheres to draw in OnDrawGizmos
            if (drawDebug)
            {
                _debugSphereQueue = new Queue<Vector3>();
            }
        }

        //Update the Head Yaw for Calculating "Shoulder Positions"
        void Update()
        {
            if (Camera.main != null)
            {
                Quaternion headYaw = Quaternion.Euler(0f, _oldCameraRot.eulerAngles.y, 0f);
                _currentRotation = Quaternion.Slerp(_currentRotation, headYaw, 0.1f);
            }
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

            _oldCameraPos = Camera.main.transform.position;
            _oldCameraRot = Camera.main.transform.rotation;
            //OldCameraFoV = Camera.main.fieldOfView;

            //Send update events if there is a selected object
            //This is important for InputField to receive keyboard events
            SendUpdateEventToSelectedObject();

            //Begin Processing Each Hand
            for (var whichPointer = 0; whichPointer < _numberOfPointers; whichPointer++)
            {
                int whichHand;
                int whichFinger;
                if (perFingerPointer)
                {
                    whichHand = whichPointer <= 4 ? 0 : 1;
                    whichFinger = whichPointer <= 4 ? whichPointer : whichPointer - 5;
                    //Move on if this hand isn't visible in the frame
                    if (leapDataProvider.CurrentFrame.Hands.Count - 1 < whichHand)
                    {
                        if (_pointers[whichPointer].gameObject.activeInHierarchy)
                        {
                            _pointers[whichPointer].gameObject.SetActive(false);
                            if (innerPointer)
                            {
                                _innerPointers[whichPointer].gameObject.SetActive(false);
                            }
                        }

                        continue;
                    }
                }
                else
                {
                    whichHand = whichPointer;
                    whichFinger = 1;
                    //Move on if this hand isn't visible in the frame
                    if (leapDataProvider.CurrentFrame.Hands.Count - 1 < whichHand)
                    {
                        if (_pointers[whichPointer].gameObject.activeInHierarchy)
                        {
                            _pointers[whichPointer].gameObject.SetActive(false);
                            if (innerPointer)
                            {
                                _innerPointers[whichPointer].gameObject.SetActive(false);
                            }
                        }

                        continue;
                    }
                }

                //Calculate Shoulder Positions (for Projection)
                Vector3 projectionOrigin = Vector3.zero;
                if (Camera.main != null)
                {
                    switch (leapDataProvider.CurrentFrame.Hands[whichHand].IsRight)
                    {
                        case true:
                            projectionOrigin = _oldCameraPos + _currentRotation * new Vector3(0.15f, -0.2f, 0f);
                            break;
                        case false:
                            projectionOrigin = _oldCameraPos + _currentRotation * new Vector3(-0.15f, -0.2f, 0f);
                            break;
                    }
                }

                //Draw Shoulders as Spheres, and the Raycast as a Line
                if (drawDebug)
                {
                    _debugSphereQueue.Enqueue(projectionOrigin);
                    Debug.DrawRay(projectionOrigin, _currentRotation * Vector3.forward * 5f);
                }

                //Raycast from shoulder through tip of the index finger to the UI
                bool tipRaycast;
                if (interactionMode != InteractionCapability.Projective)
                {
                    tipRaycast = GetLookPointerEventData(whichPointer, whichHand, whichFinger, projectionOrigin,
                        _currentRotation * Vector3.forward, true);
                    _prevState[whichPointer] =
                        _pointerState[whichPointer]; //Store old state for sound transitionary purposes
                    UpdatePointer(whichPointer, _pointEvents[whichPointer],
                        _pointEvents[whichPointer].pointerCurrentRaycast.gameObject);
                    ProcessState(whichPointer, whichHand, whichFinger, tipRaycast);
                }

                //If didn't hit anything near the fingertip, try doing it again, but through the knuckle this time
                if (_pointerState[whichPointer] == PointerStates.OffCanvas &&
                    interactionMode != InteractionCapability.Tactile ||
                    interactionMode == InteractionCapability.Projective)
                {
                    tipRaycast = GetLookPointerEventData(whichPointer, whichHand, whichFinger, projectionOrigin,
                        _currentRotation * Vector3.forward, false);
                    if (interactionMode == InteractionCapability.Projective)
                    {
                        _prevState[whichPointer] =
                            _pointerState[whichPointer]; //Store old state for sound transitionary purposes
                    }

                    UpdatePointer(whichPointer, _pointEvents[whichPointer],
                        _pointEvents[whichPointer].pointerCurrentRaycast.gameObject);
                    if (!tipRaycast && (_forceTactile || !_forceProjective &&
                        DistanceOfTipToPointer(whichPointer, whichHand, whichFinger) <
                        projectiveToTactileTransitionDistance))
                    {
                        _pointEvents[whichPointer].pointerCurrentRaycast = new RaycastResult();
                    }

                    ProcessState(whichPointer, whichHand, whichFinger, tipRaycast);
                }

                //Handle the Environment Pointer
                if (environmentPointer && _pointerState[whichPointer] == PointerStates.OffCanvas)
                {
                    Vector3 indexMetacarpal = leapDataProvider.CurrentFrame.Hands[whichHand].Fingers[whichFinger]
                        .Bone(Bone.BoneType.TYPE_METACARPAL).Center.ToVector3();
                    Physics.Raycast(projectionOrigin, (indexMetacarpal - projectionOrigin).normalized,
                        out var environmentSpot);
                    _pointers[whichPointer].position = environmentSpot.point + environmentSpot.normal * 0.01f;
                    _pointers[whichPointer].rotation = Quaternion.LookRotation(environmentSpot.normal);
                    if (innerPointer)
                    {
                        _innerPointers[whichPointer].position = environmentSpot.point + environmentSpot.normal * 0.01f;
                        _innerPointers[whichPointer].rotation = Quaternion.LookRotation(environmentSpot.normal);
                    }

                    EvaluatePointerSize(whichPointer);

                    if (IsTriggeringInteraction(whichPointer, whichHand, whichFinger))
                    {
                        environmentPinch.Invoke(_pointers[whichPointer].position);
                    }
                }

                _prevScreenPosition[whichPointer] = _pointEvents[whichPointer].position;

                if (drawDebug)
                {
                    _pointerLines[whichPointer].SetPosition(0, Camera.main.transform.position);
                    _pointerLines[whichPointer].SetPosition(1, _pointers[whichPointer].position);
                }

                //Trigger events that come from changing pointer state
                ProcessStateEvents(whichPointer);
                if (_pointEvents[whichPointer] != null)
                {
                    //Tell Leap Buttons how far away the finger is
                    GameObject hoverer =
                        ExecuteEvents.GetEventHandler<IPointerClickHandler>(_pointEvents[whichPointer]
                            .pointerCurrentRaycast.gameObject);
                    if (hoverer != null)
                    {
                        ILeapWidget comp = hoverer.GetComponent<ILeapWidget>();
                        if (comp == null)
                        {
                            comp = hoverer.GetComponentInParent<ILeapWidget>();
                        }

                        if (comp != null)
                        {
                            //if (!isTriggeringInteraction(whichPointer, whichHand, whichFinger)) { //I forget why I put this here....
                            comp.HoverDistance(DistanceOfTipToPointer(whichPointer, whichHand, whichFinger));
                            //}
                        }
                    }

                    //If we hit something with our Raycast, let's see if we should interact with it
                    if (_pointEvents[whichPointer].pointerCurrentRaycast.gameObject != null &&
                        _pointerState[whichPointer] != PointerStates.OffCanvas)
                    {
                        _prevOverGo[whichPointer] = _currentOverGo[whichPointer];
                        _currentOverGo[whichPointer] = _pointEvents[whichPointer].pointerCurrentRaycast.gameObject;

                        //Trigger Enter or Exit Events on the UI Element (like highlighting)
                        HandlePointerExitAndEnter(_pointEvents[whichPointer], _currentOverGo[whichPointer]);

                        //If we weren't triggering an interaction last frame, but we are now...
                        if (!_prevTriggeringInteraction[whichPointer] &&
                            IsTriggeringInteraction(whichPointer, whichHand, whichFinger))
                        {
                            _prevTriggeringInteraction[whichPointer] = true;

                            if (Time.time - _timeEnteredCanvas[whichPointer] >= Time.deltaTime)
                            {
                                //Deselect all objects
                                if (eventSystem.currentSelectedGameObject)
                                {
                                    eventSystem.SetSelectedGameObject(null);
                                }

                                //Record pointer telemetry
                                _pointEvents[whichPointer].pressPosition = _pointEvents[whichPointer].position;
                                _pointEvents[whichPointer].pointerPressRaycast =
                                    _pointEvents[whichPointer].pointerCurrentRaycast;
                                _pointEvents[whichPointer].pointerPress = null; //Clear this for setting later
                                _pointEvents[whichPointer].useDragThreshold = true;

                                //If we hit something good, let's trigger it!
                                if (_currentOverGo[whichPointer] != null)
                                {
                                    _currentGo[whichPointer] = _currentOverGo[whichPointer];

                                    //See if this object, or one of its parents, has a pointerDownHandler
                                    GameObject newPressed = ExecuteEvents.ExecuteHierarchy(_currentGo[whichPointer],
                                        _pointEvents[whichPointer], ExecuteEvents.pointerDownHandler);

                                    //If not, see if one has a pointerClickHandler!
                                    if (newPressed == null)
                                    {
                                        newPressed = ExecuteEvents.ExecuteHierarchy(_currentGo[whichPointer],
                                            _pointEvents[whichPointer], ExecuteEvents.pointerClickHandler);
                                        if (newPressed != null)
                                        {
                                            _currentGo[whichPointer] = newPressed;
                                        }
                                    }
                                    else
                                    {
                                        _currentGo[whichPointer] = newPressed;
                                        //We want to do "click on button down" at same time, unlike regular mouse processing
                                        //Which does click when mouse goes up over same object it went down on
                                        //This improves the user's ability to select small menu items
                                        //ExecuteEvents.Execute(newPressed, PointEvents[whichPointer], ExecuteEvents.pointerClickHandler);
                                    }

                                    if (newPressed != null)
                                    {
                                        _pointEvents[whichPointer].pointerPress = newPressed;
                                        _currentGo[whichPointer] = newPressed;

                                        //Select the currently pressed object
                                        if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(
                                            _currentGo[whichPointer]))
                                        {
                                            eventSystem.SetSelectedGameObject(_currentGo[whichPointer]);
                                        }
                                    }

                                    //Debug.Log(currentGo[whichPointer].name);
                                    _pointEvents[whichPointer].pointerDrag =
                                        ExecuteEvents.GetEventHandler<IDragHandler>(_currentGo[whichPointer]);
                                    //Debug.Log(PointEvents[whichPointer].pointerDrag.name);

                                    if (_pointEvents[whichPointer].pointerDrag)
                                    {
                                        IDragHandler dragger = _pointEvents[whichPointer].pointerDrag
                                            .GetComponent<IDragHandler>();
                                        if (dragger != null)
                                        {
                                            if (dragger is EventTrigger &&
                                                _pointEvents[whichPointer].pointerDrag.transform.parent)
                                            {
                                                //Hack: EventSystems intercepting Drag Events causing funkiness
                                                _pointEvents[whichPointer].pointerDrag =
                                                    ExecuteEvents.GetEventHandler<IDragHandler>(
                                                        _pointEvents[whichPointer].pointerDrag.transform.parent
                                                            .gameObject);
                                                if (_pointEvents[whichPointer].pointerDrag != null)
                                                {
                                                    dragger = _pointEvents[whichPointer].pointerDrag
                                                        .GetComponent<IDragHandler>();
                                                    if (dragger != null && !(dragger is EventTrigger))
                                                    {
                                                        _currentGoing[whichPointer] =
                                                            _pointEvents[whichPointer].pointerDrag;
                                                        _dragBeginPosition[whichPointer] =
                                                            _pointEvents[whichPointer].position;
                                                        if (_currentGo[whichPointer] && _currentGo[whichPointer] ==
                                                            _currentGoing[whichPointer])
                                                        {
                                                            ExecuteEvents.Execute(
                                                                _pointEvents[whichPointer].pointerDrag,
                                                                _pointEvents[whichPointer],
                                                                ExecuteEvents.beginDragHandler);
                                                            _pointEvents[whichPointer].dragging = true;
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                _currentGoing[whichPointer] = _pointEvents[whichPointer].pointerDrag;
                                                _dragBeginPosition[whichPointer] = _pointEvents[whichPointer].position;
                                                if (_currentGo[whichPointer] && _currentGo[whichPointer] ==
                                                    _currentGoing[whichPointer])
                                                {
                                                    ExecuteEvents.Execute(_pointEvents[whichPointer].pointerDrag,
                                                        _pointEvents[whichPointer], ExecuteEvents.beginDragHandler);
                                                    _pointEvents[whichPointer].dragging = true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }


                    //If we have dragged beyond the drag threshold
                    if (!_pointEvents[whichPointer].dragging && _currentGoing[whichPointer] &&
                        Vector2.Distance(_pointEvents[whichPointer].position, _dragBeginPosition[whichPointer]) * 100f >
                        EventSystem.current.pixelDragThreshold)
                    {
                        IDragHandler dragger = _pointEvents[whichPointer].pointerDrag.GetComponent<IDragHandler>();
                        if (dragger != null && dragger is ScrollRect)
                        {
                            if (_currentGo[whichPointer] && !_currentGo[whichPointer].GetComponent<ScrollRect>())
                            {
                                ExecuteEvents.Execute(_pointEvents[whichPointer].pointerDrag,
                                    _pointEvents[whichPointer], ExecuteEvents.beginDragHandler);
                                _pointEvents[whichPointer].dragging = true;

                                ExecuteEvents.Execute(_currentGo[whichPointer], _pointEvents[whichPointer],
                                    ExecuteEvents.pointerUpHandler);
                                _pointEvents[whichPointer].rawPointerPress = null;
                                _pointEvents[whichPointer].pointerPress = null;
                                _currentGo[whichPointer] = null;
                            }
                        }
                    }


                    //If we WERE interacting last frame, but are not this frame...
                    if (_prevTriggeringInteraction[whichPointer] &&
                        (!IsTriggeringInteraction(whichPointer, whichHand, whichFinger) ||
                         _pointerState[whichPointer] == PointerStates.OffCanvas))
                    {
                        _prevTriggeringInteraction[whichPointer] = false;

                        if (_currentGoing[whichPointer])
                        {
                            ExecuteEvents.Execute(_currentGoing[whichPointer], _pointEvents[whichPointer],
                                ExecuteEvents.endDragHandler);
                            if (_currentGo[whichPointer] && _currentGoing[whichPointer] == _currentGo[whichPointer])
                            {
                                ExecuteEvents.Execute(_currentGoing[whichPointer], _pointEvents[whichPointer],
                                    ExecuteEvents.pointerUpHandler);
                            }

                            //Debug.Log(currentGoing[whichPointer].name);
                            if (_currentOverGo[whichPointer] != null)
                            {
                                ExecuteEvents.ExecuteHierarchy(_currentOverGo[whichPointer], _pointEvents[whichPointer],
                                    ExecuteEvents.dropHandler);
                            }

                            _pointEvents[whichPointer].pointerDrag = null;
                            _pointEvents[whichPointer].dragging = false;
                            _currentGoing[whichPointer] = null;
                        }

                        if (_currentGo[whichPointer])
                        {
                            ExecuteEvents.Execute(_currentGo[whichPointer], _pointEvents[whichPointer],
                                ExecuteEvents.pointerUpHandler);
                            ExecuteEvents.Execute(_currentGo[whichPointer], _pointEvents[whichPointer],
                                ExecuteEvents.pointerClickHandler);
                            _pointEvents[whichPointer].rawPointerPress = null;
                            _pointEvents[whichPointer].pointerPress = null;
                            _currentGo[whichPointer] = null;
                            _currentGoing[whichPointer] = null;
                        }
                    }

                    //And for everything else, there is dragging.
                    if (_pointEvents[whichPointer].pointerDrag != null && _pointEvents[whichPointer].dragging)
                    {
                        ExecuteEvents.Execute(_pointEvents[whichPointer].pointerDrag, _pointEvents[whichPointer],
                            ExecuteEvents.dragHandler);
                    }
                }

                UpdatePointerColor(whichPointer, whichHand, whichFinger);


                //Make the special Leap Widget Buttons Pop Up and Flatten when Appropriate
                if (_prevTouchingMode != GetTouchingMode() && retractUI)
                {
                    _prevTouchingMode = GetTouchingMode();
                    if (_prevTouchingMode)
                    {
                        foreach (var t in _canvases)
                        {
                            t.BroadcastMessage("Expand", SendMessageOptions.DontRequireReceiver);
                        }
                    }
                    else
                    {
                        foreach (var t in _canvases)
                        {
                            t.BroadcastMessage("Retract", SendMessageOptions.DontRequireReceiver);
                        }
                    }
                }
            }

            Camera.main.transform.position = _oldCameraPos;
            Camera.main.transform.rotation = _oldCameraRot;
            //Camera.main.fieldOfView = OldCameraFoV;
        }

        //Raycast from the EventCamera into UI Space
        bool GetLookPointerEventData(int whichPointer, int whichHand, int whichFinger, Vector3 origin,
            Vector3 direction, bool forceTipRaycast)
        {
            //Whether or not this will be a raycast through the finger tip
            bool tipRaycast = false;

            //Initialize a blank PointerEvent
            if (_pointEvents[whichPointer] == null)
            {
                _pointEvents[whichPointer] = new PointerEventData(eventSystem);
            }
            else
            {
                _pointEvents[whichPointer].Reset();
            }

            //We're always going to assume we're "Left Clicking", for the benefit of uGUI
            _pointEvents[whichPointer].button = PointerEventData.InputButton.Left;

            //If we're in "Touching Mode", Raycast through the fingers
            Vector3 indexFingerPosition;
            if (GetTouchingMode(whichPointer) || forceTipRaycast)
            {
                tipRaycast = true;

                //Focus pointer through the average of the extended fingers
                if (!perFingerPointer)
                {
                    /*
                    float numberOfExtendedFingers = 0.1f;
                    IndexFingerPosition = LeapDataProvider.CurrentFrame.Hands[whichHand].Fingers[whichFinger].TipPosition.ToVector3() * 0.1f;
                    //Averages cursor position through average of extended fingers; ended up being worse than expected
                    for (int i = 1; i < 4; i++) {
                      float fingerExtension = Mathf.Clamp01(Vector3.Dot(LeapDataProvider.CurrentFrame.Hands[whichHand].Fingers[i].Direction.ToVector3(), LeapDataProvider.CurrentFrame.Hands[whichPointer].Direction.ToVector3())) / 1.5f;
                      if (fingerExtension > 0f) {
                        numberOfExtendedFingers += fingerExtension;
                        IndexFingerPosition += LeapDataProvider.CurrentFrame.Hands[whichHand].Fingers[i].TipPosition.ToVector3() * fingerExtension;
                      }
                    }
                    IndexFingerPosition /= numberOfExtendedFingers;
                    */

                    float farthest = 0f;
                    indexFingerPosition = leapDataProvider.CurrentFrame.Hands[whichHand].Fingers[1].TipPosition
                        .ToVector3();
                    for (int i = 1; i < 3; i++)
                    {
                        float fingerDistance = Vector3.Distance(Camera.main.transform.position,
                            leapDataProvider.CurrentFrame.Hands[whichHand].Fingers[i].TipPosition.ToVector3());
                        float fingerExtension =
                            Mathf.Clamp01(Vector3.Dot(
                                leapDataProvider.CurrentFrame.Hands[whichHand].Fingers[i].Direction.ToVector3(),
                                leapDataProvider.CurrentFrame.Hands[whichPointer].Direction.ToVector3())) / 1.5f;
                        if (fingerDistance > farthest && fingerExtension > 0.5f)
                        {
                            farthest = fingerDistance;
                            indexFingerPosition = leapDataProvider.CurrentFrame.Hands[whichHand].Fingers[i].TipPosition
                                .ToVector3();
                        }
                    }
                }
                else
                {
                    indexFingerPosition = leapDataProvider.CurrentFrame.Hands[whichHand].Fingers[whichFinger]
                        .TipPosition.ToVector3();
                }

                //Else Raycast through the knuckle of the Index Finger
            }
            else
            {
                Camera.main.transform.position = origin;
                indexFingerPosition = leapDataProvider.CurrentFrame.Hands[whichHand].Fingers[whichFinger]
                    .Bone(Bone.BoneType.TYPE_METACARPAL).Center.ToVector3();
            }

            //Draw Camera Origin
            if (drawDebug)
                _debugSphereQueue.Enqueue(Camera.main.transform.position);

            //Set EventCamera's FoV
            //Camera.main.fieldOfView = 179f;

            //Set the Raycast Direction and Delta
            _pointEvents[whichPointer].position = Vector2.Lerp(_prevScreenPosition[whichPointer],
                Camera.main.WorldToScreenPoint(indexFingerPosition),
                1.0f); //new Vector2(Screen.width / 2, Screen.height / 2);
            _pointEvents[whichPointer].delta =
                (_pointEvents[whichPointer].position - _prevScreenPosition[whichPointer]) * -10f;
            _pointEvents[whichPointer].scrollDelta = Vector2.zero;

            //Perform the Raycast and sort all the things we hit by distance...
            eventSystem.RaycastAll(_pointEvents[whichPointer], m_RaycastResultCache);

            //Optional hack that subverts ScrollRect hierarchies; to avoid this, disable "RaycastTarget" on the Viewport and Content panes
            if (overrideScrollViewClicks)
            {
                _pointEvents[whichPointer].pointerCurrentRaycast = new RaycastResult();
                foreach (var t in m_RaycastResultCache)
                {
                    if (t.gameObject.GetComponent<Scrollbar>() != null)
                    {
                        _pointEvents[whichPointer].pointerCurrentRaycast = t;
                    }
                    else if (_pointEvents[whichPointer].pointerCurrentRaycast.gameObject == null &&
                             t.gameObject.GetComponent<ScrollRect>() != null)
                    {
                        _pointEvents[whichPointer].pointerCurrentRaycast = t;
                    }
                }

                if (_pointEvents[whichPointer].pointerCurrentRaycast.gameObject == null)
                {
                    _pointEvents[whichPointer].pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
                }
            }
            else
            {
                _pointEvents[whichPointer].pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
            }

            //Clear the list of things we hit; we don't need it anymore.
            m_RaycastResultCache.Clear();

            return tipRaycast;
        }

        //Tree to decide the State of the Pointer
        void ProcessState(int whichPointer, int whichHand, int whichFinger, bool forceTipRaycast)
        {
            if (_pointEvents[whichPointer].pointerCurrentRaycast.gameObject != null)
            {
                if (_forceTactile || !_forceProjective && DistanceOfTipToPointer(whichPointer, whichHand, whichFinger) <
                    projectiveToTactileTransitionDistance)
                {
                    if (IsTriggeringInteraction(whichPointer, whichHand, whichFinger))
                    {
                        if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(_pointEvents[whichPointer]
                            .pointerCurrentRaycast.gameObject))
                        {
                            _pointerState[whichPointer] = PointerStates.TouchingElement;
                        }
                        else
                        {
                            _pointerState[whichPointer] = PointerStates.TouchingCanvas;
                        }
                    }
                    else
                    {
                        _pointerState[whichPointer] = PointerStates.NearCanvas;
                    }
                }
                else if (!forceTipRaycast)
                {
                    if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(_pointEvents[whichPointer]
                        .pointerCurrentRaycast.gameObject))
                    {
                        // || PointEvents[whichPointer].dragging) {
                        if (IsTriggeringInteraction(whichPointer, whichHand, whichFinger))
                        {
                            _pointerState[whichPointer] = PointerStates.PinchingToElement;
                        }
                        else
                        {
                            _pointerState[whichPointer] = PointerStates.OnElement;
                        }
                    }
                    else
                    {
                        if (IsTriggeringInteraction(whichPointer, whichHand, whichFinger))
                        {
                            _pointerState[whichPointer] = PointerStates.PinchingToCanvas;
                        }
                        else
                        {
                            _pointerState[whichPointer] = PointerStates.OnCanvas;
                        }
                    }
                }
                else
                {
                    _pointerState[whichPointer] = PointerStates.OffCanvas;
                }
            }
            else
            {
                _pointerState[whichPointer] = PointerStates.OffCanvas;
            }
        }

        //Discrete 1-Frame Transition Behaviors like Sounds and Events
        //(color changing is in a different function since it is lerped over multiple frames)
        void ProcessStateEvents(int whichPointer)
        {
            if (triggerHoverOnElementSwitch)
            {
                if (_prevState[whichPointer] != PointerStates.OffCanvas &&
                    _pointerState[whichPointer] != PointerStates.OffCanvas)
                {
                    if (_currentOverGo[whichPointer] != _prevOverGo[whichPointer])
                    {
                        //When you begin to hover on an element
                        _soundPlayer.PlayOneShot(beginHoverSound);
                        onHover.Invoke(_pointers[whichPointer].transform.position);
                    }
                }
            }

            //Warning: Horrible State Machine ahead...
            if (_prevState[whichPointer] == PointerStates.OnCanvas)
            {
                if (_pointerState[whichPointer] == PointerStates.OnElement)
                {
                    //When you go from hovering on the Canvas to hovering on an element
                    if (!triggerHoverOnElementSwitch)
                    {
                        _soundPlayer.PlayOneShot(beginHoverSound);
                        onHover.Invoke(_pointers[whichPointer].transform.position);
                    }
                }
                else if (_pointerState[whichPointer] == PointerStates.PinchingToCanvas)
                {
                    //When you try to interact with the Canvas
                    _soundPlayer.PlayOneShot(beginMissedSound);
                }
            }
            else if (_prevState[whichPointer] == PointerStates.PinchingToCanvas)
            {
                if (_pointerState[whichPointer] == PointerStates.OnCanvas)
                {
                    //When you unpinch off of Blank Canvas
                    _soundPlayer.PlayOneShot(endMissedSound);
                }
            }
            else if (_prevState[whichPointer] == PointerStates.OnElement)
            {
                if (_pointerState[whichPointer] == PointerStates.OnCanvas)
                {
                    //When you begin to hover over the Canvas after hovering over an element
                    _soundPlayer.PlayOneShot(endHoverSound);
                }
                else if (_pointerState[whichPointer] == PointerStates.PinchingToElement)
                {
                    //When you click on an element
                    _soundPlayer.PlayOneShot(beginTriggerSound);
                    onClickDown.Invoke(_pointers[whichPointer].transform.position);
                }
            }
            else if (_prevState[whichPointer] == PointerStates.PinchingToElement)
            {
                if (_pointerState[whichPointer] == PointerStates.PinchingToCanvas)
                {
                    //When you slide off of an element while holding it
                    //SoundPlayer.PlayOneShot(HoverSound);
                }
                else if (_pointerState[whichPointer] == PointerStates.OnElement ||
                         _pointerState[whichPointer] == PointerStates.OnCanvas)
                {
                    //When you let go of an element
                    _soundPlayer.PlayOneShot(endTriggerSound);
                    onClickUp.Invoke(_pointers[whichPointer].transform.position);
                }
            }
            else if (_prevState[whichPointer] == PointerStates.NearCanvas)
            {
                if (_pointerState[whichPointer] == PointerStates.TouchingElement)
                {
                    //When you physically touch an element
                    _soundPlayer.PlayOneShot(beginTriggerSound);
                    onClickDown.Invoke(_pointers[whichPointer].transform.position);
                }

                if (_pointerState[whichPointer] == PointerStates.TouchingCanvas)
                {
                    //When you physically touch Blank Canvas
                    _soundPlayer.PlayOneShot(beginMissedSound);
                }
            }
            else if (_prevState[whichPointer] == PointerStates.TouchingCanvas)
            {
                if (_pointerState[whichPointer] == PointerStates.NearCanvas)
                {
                    //When you physically lift off of Blank Canvas
                    _soundPlayer.PlayOneShot(endMissedSound);
                }
            }
            else if (_prevState[whichPointer] == PointerStates.TouchingElement)
            {
                if (_pointerState[whichPointer] == PointerStates.NearCanvas)
                {
                    //When you physically pull out of an element
                    _soundPlayer.PlayOneShot(endTriggerSound);
                    onClickUp.Invoke(_pointers[whichPointer].transform.position);
                }
            }
            else if (_prevState[whichPointer] == PointerStates.OffCanvas)
            {
                if (_pointerState[whichPointer] != PointerStates.OffCanvas)
                {
                    //Record the time the hand entered an interactable state
                    _timeEnteredCanvas[whichPointer] = Time.time;
                }
            }
        }

        //Update the pointer location and whether or not it is enabled
        void UpdatePointer(int whichPointer, PointerEventData pointData, GameObject uiComponent)
        {
            if (environmentPointer && _pointerState[whichPointer] == PointerStates.OffCanvas)
            {
                _pointers[whichPointer].gameObject.SetActive(true);
                if (innerPointer)
                {
                    _innerPointers[whichPointer].gameObject.SetActive(true);
                }
            }

            if (_currentOverGo[whichPointer] != null)
            {
                _pointers[whichPointer].gameObject.SetActive(true);
                if (innerPointer)
                {
                    _innerPointers[whichPointer].gameObject.SetActive(true);
                }

                if (_pointEvents[whichPointer].pointerCurrentRaycast.gameObject != null)
                {
                    RectTransform draggingPlane = _pointEvents[whichPointer].pointerCurrentRaycast.gameObject
                        .GetComponent<RectTransform>();
                    if (RectTransformUtility.ScreenPointToWorldPointInRectangle(draggingPlane, pointData.position,
                        pointData.enterEventCamera, out var globalLookPos))
                    {
                        GameObject hoverer = ExecuteEvents.GetEventHandler<IPointerEnterHandler>(uiComponent);
                        if (hoverer)
                        {
                            Vector3 componentInPlane = hoverer.transform.InverseTransformPoint(globalLookPos);
                            componentInPlane = new Vector3(componentInPlane.x, componentInPlane.y, 0f);
                            _pointers[whichPointer].position =
                                hoverer.transform
                                    .TransformPoint(
                                        componentInPlane); // -transform.forward * 0.01f; //Amount the pointer floats above the Canvas
                        }
                        else
                        {
                            _pointers[whichPointer].position = globalLookPos;
                        }

                        float pointerAngle = Mathf.Rad2Deg * Mathf.Atan2(pointData.delta.x, pointData.delta.y);
                        _pointers[whichPointer].rotation =
                            draggingPlane.rotation * Quaternion.Euler(0f, 0f, -pointerAngle);
                        if (innerPointer)
                        {
                            _innerPointers[whichPointer].position =
                                globalLookPos; // -transform.forward * 0.01f; //Amount the pointer floats above the Canvas
                            _innerPointers[whichPointer].rotation =
                                draggingPlane.rotation * Quaternion.Euler(0f, 0f, -pointerAngle);
                        }

                        EvaluatePointerSize(whichPointer);
                    }
                }
            }
        }

        void EvaluatePointerSize(int whichPointer)
        {
            //Use the Scale AnimCurve to Evaluate the Size of the Pointer
            float pointDistance = 1f;
            if (Camera.main != null)
            {
                pointDistance = (_pointers[whichPointer].position - Camera.main.transform.position).magnitude;
            }

            float pointerScale = pointerDistanceScale.Evaluate(pointDistance);

            if (innerPointer)
            {
                _innerPointers[whichPointer].localScale = pointerScale * pointerPinchScale.Evaluate(0f) * Vector3.one;
            }

            if (!perFingerPointer && !GetTouchingMode(whichPointer))
            {
                if (whichPointer == 0)
                {
                    pointerScale *= pointerPinchScale.Evaluate(leapDataProvider.CurrentFrame.Hands[0].PinchDistance);
                }
                else if (whichPointer == 1)
                {
                    pointerScale *= pointerPinchScale.Evaluate(leapDataProvider.CurrentFrame.Hands[1].PinchDistance);
                }
            }

            //Commented out Velocity Stretching because it looks funny when switching between Tactile and Projective
            _pointers[whichPointer].localScale =
                pointerScale * new Vector3(1f, 1f /*+ pointData.delta.magnitude*1f*/, 1f);
        }

        /** A boolean function that returns true if a "click" is being triggered during the current frame. */
        bool IsTriggeringInteraction(int whichPointer, int whichHand, int whichFinger)
        {
            if (interactionMode != InteractionCapability.Projective)
            {
                if (GetTouchingMode(whichPointer))
                {
                    return DistanceOfTipToPointer(whichPointer, whichHand, whichFinger) < 0f;
                }
            }

            if (interactionMode != InteractionCapability.Tactile)
            {
                if (leapDataProvider.CurrentFrame.Hands[whichHand].IsRight && rightHandDetector != null &&
                    rightHandDetector.IsPinching || rightHandDetector == null &&
                    leapDataProvider.CurrentFrame.Hands[whichHand].PinchDistance < pinchingThreshold)
                {
                    return true;
                }
                else if (leapDataProvider.CurrentFrame.Hands[whichHand].IsLeft && leftHandDetector != null &&
                    leftHandDetector.IsPinching || leftHandDetector == null &&
                    leapDataProvider.CurrentFrame.Hands[whichHand].PinchDistance < pinchingThreshold)
                {
                    return true;
                }
            }

            //Disabling Pinching during touch interactions; maybe still desirable?
            //return LeapDataProvider.CurrentFrame.Hands[whichPointer].PinchDistance < PinchingThreshold;

            return false;
        }

        /** The z position of the index finger tip to the Pointer. */
        float DistanceOfTipToPointer(int whichPointer, int whichHand, int whichFinger)
        {
            //Get Base of Index Finger Position
            Vector3 tipPosition = leapDataProvider.CurrentFrame.Hands[whichHand].Fingers[whichFinger]
                .Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();
            return -_pointers[whichPointer].InverseTransformPoint(tipPosition).z *
                _pointers[whichPointer].lossyScale.z - tactilePadding;
        }

        /** The z position of the index finger tip to the specified transform. */
        float DistanceOfTipToElement(Transform uiElement, int whichHand, int whichFinger)
        {
            //Get Base of Index Finger Position
            Vector3 tipPosition = leapDataProvider.CurrentFrame.Hands[whichHand].Fingers[whichFinger]
                .Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();
            return -uiElement.InverseTransformPoint(tipPosition).z * uiElement.lossyScale.z - tactilePadding;
        }

        /** Returns true if any active pointer is in the "touching" interaction mode, i.e, whether it is touching or nearly touching a canvas or control. */
        bool GetTouchingMode()
        {
            var mode = false;
            foreach (var t in _pointerState)
            {
                if (t == PointerStates.NearCanvas || t == PointerStates.TouchingCanvas ||
                    t == PointerStates.TouchingElement)
                {
                    mode = true;
                }
            }

            return mode;
        }

        /** Returns true if the specified pointer is in the "touching" interaction mode, i.e, whether it is touching or nearly touching a canvas or control. */
        bool GetTouchingMode(int whichPointer)
        {
            return _pointerState[whichPointer] == PointerStates.NearCanvas ||
                   _pointerState[whichPointer] == PointerStates.TouchingCanvas ||
                   _pointerState[whichPointer] == PointerStates.TouchingElement;
        }

        //Where the color that the Pointer will lerp to is chosen
        void UpdatePointerColor(int whichPointer, int whichHand, int whichFinger)
        {
            float transitionAmount =
                Mathf.Clamp01(Mathf.Abs(DistanceOfTipToPointer(whichPointer, whichHand, whichFinger) -
                                        projectiveToTactileTransitionDistance) / 0.05f);

            switch (_pointerState[whichPointer])
            {
                case PointerStates.OnCanvas:
                    LerpPointerColor(whichPointer, new Color(0f, 0f, 0f, 1f * transitionAmount), 0.2f);
                    LerpPointerColor(whichPointer, standardColor, 0.2f);
                    break;
                case PointerStates.OnElement:
                    LerpPointerColor(whichPointer, new Color(0f, 0f, 0f, 1f * transitionAmount), 0.2f);
                    LerpPointerColor(whichPointer, hoveringColor, 0.2f);
                    break;
                case PointerStates.PinchingToCanvas:
                    LerpPointerColor(whichPointer, new Color(0f, 0f, 0f, 1f * transitionAmount), 0.2f);
                    LerpPointerColor(whichPointer, triggerMissedColor, 0.2f);
                    break;
                case PointerStates.PinchingToElement:
                    LerpPointerColor(whichPointer, new Color(0f, 0f, 0f, 1f * transitionAmount), 0.2f);
                    LerpPointerColor(whichPointer, triggeringColor, 0.2f);
                    break;
                case PointerStates.NearCanvas:
                    LerpPointerColor(whichPointer, new Color(0.0f, 0.0f, 0.0f, 0.5f * transitionAmount), 0.3f);
                    LerpPointerColor(whichPointer, standardColor, 0.2f);
                    break;
                case PointerStates.TouchingElement:
                    LerpPointerColor(whichPointer, new Color(0.0f, 0.0f, 0.0f, 0.7f * transitionAmount), 0.2f);
                    LerpPointerColor(whichPointer, triggeringColor, 0.2f);
                    break;
                case PointerStates.TouchingCanvas:
                    LerpPointerColor(whichPointer, new Color(0.0f, 0.01f, 0.0f, 0.5f * transitionAmount), 0.2f);
                    LerpPointerColor(whichPointer, triggerMissedColor, 0.2f);
                    break;
                case PointerStates.OffCanvas:
                    LerpPointerColor(whichPointer, triggerMissedColor, 0.2f);
                    if (environmentPointer)
                    {
                        LerpPointerColor(whichPointer, new Color(0.0f, 0.0f, 0.0f, 0.5f * transitionAmount), 1f);
                    }
                    else
                    {
                        LerpPointerColor(whichPointer, new Color(0.0f, 0.0f, 0.0f, 0.001f), 1f);
                    }

                    break;
            }
        }

        //Where the lerping of the pointer's color takes place
        //If RGB are 0f or Alpha is 1f, then it will ignore those components and only lerp the remaining components
        /** Linearly interpolates the color of a cursor toward the specified color.
         *  @param whichPointer The identifier of the pointer to change.
         *  @param color The target color.
         *  @param lerpalpha The amount to interpolate by.
         */
        void LerpPointerColor(int whichPointer, Color color, float lerpAlpha)
        {
            SpriteRenderer pointerSprite = _pointers[whichPointer].GetComponent<SpriteRenderer>();
            Color oldColor = pointerSprite.color;
            if (color.r == 0f && color.g == 0f && color.b == 0f)
            {
                pointerSprite.material.color = Color.Lerp(oldColor,
                    new Color(oldColor.r, oldColor.g, oldColor.b, color.a), lerpAlpha);
                pointerSprite.color = Color.Lerp(oldColor, new Color(oldColor.r, oldColor.g, oldColor.b, color.a),
                    lerpAlpha);
            }
            else if (color.a == 1f)
            {
                pointerSprite.material.color =
                    Color.Lerp(oldColor, new Color(color.r, color.g, color.b, oldColor.a), lerpAlpha);
                pointerSprite.color = Color.Lerp(oldColor, new Color(color.r, color.g, color.b, oldColor.a), lerpAlpha);
            }
            else
            {
                pointerSprite.material.color = Color.Lerp(oldColor, color, lerpAlpha);
                pointerSprite.color = Color.Lerp(oldColor, color, lerpAlpha);
            }

            if (innerPointer)
            {
                SpriteRenderer innerPointerSprite = _innerPointers[whichPointer].GetComponent<SpriteRenderer>();
                oldColor = innerPointerSprite.color;
                if (color.r == 0f && color.g == 0f && color.b == 0f)
                {
                    innerPointerSprite.material.color = Color.Lerp(oldColor,
                        new Color(oldColor.r, oldColor.g, oldColor.b, color.a * innerPointerOpacityScalar), lerpAlpha);
                    innerPointerSprite.color = Color.Lerp(oldColor,
                        new Color(oldColor.r, oldColor.g, oldColor.b, color.a * innerPointerOpacityScalar), lerpAlpha);
                }
                else if (color.a == 1f)
                {
                    innerPointerSprite.material.color = Color.Lerp(oldColor,
                        new Color(color.r, color.g, color.b, oldColor.a * innerPointerOpacityScalar), lerpAlpha);
                    innerPointerSprite.color = Color.Lerp(oldColor,
                        new Color(color.r, color.g, color.b, oldColor.a * innerPointerOpacityScalar), lerpAlpha);
                }
                else
                {
                    innerPointerSprite.material.color = Color.Lerp(oldColor,
                        new Color(color.r, color.g, color.b, color.a * innerPointerOpacityScalar), lerpAlpha);
                    innerPointerSprite.color = Color.Lerp(oldColor,
                        new Color(color.r, color.g, color.b, color.a * innerPointerOpacityScalar), lerpAlpha);
                }
            }
        }

        void SendUpdateEventToSelectedObject()
        {
            if (eventSystem.currentSelectedGameObject == null) return;

            BaseEventData data = GetBaseEventData();
            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);
        }

        void OnDrawGizmos()
        {
            if (drawDebug)
            {
                while (_debugSphereQueue != null && _debugSphereQueue.Count > 0)
                {
                    Gizmos.DrawSphere(_debugSphereQueue.Dequeue(), 0.1f);
                }
            }
        }

        /** Only activate the InputModule when there are hands in the scene. */
        public override bool ShouldActivateModule()
        {
            return leapDataProvider.CurrentFrame != null && leapDataProvider.CurrentFrame.Hands.Count > 0 &&
                   base.ShouldActivateModule();
        }
    }
}
