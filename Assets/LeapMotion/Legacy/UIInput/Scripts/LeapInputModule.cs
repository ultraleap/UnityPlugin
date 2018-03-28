/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using System.Collections.Generic;
using Leap.Unity;
using Leap;

namespace Leap.Unity.InputModule {
  /** An InputModule that supports the use of Leap Motion tracking data for manipulating Unity UI controls. */
  public class LeapInputModule : BaseInputModule {
    //General Interaction Parameters
    [Header(" Interaction Setup")]
    [Tooltip("The current Leap Data Provider for the scene.")]
    /** The LeapProvider providing tracking data to the scene. */
    public LeapProvider LeapDataProvider;
    [Tooltip("An optional alternate detector for pinching on the left hand.")]
    /** An optional component that will be used to detect pinch motions if set.
     * Primarily used for projective or hybrid interaction modes (under experimental features).
     */
    public Leap.Unity.PinchDetector LeftHandDetector;
    [Tooltip("An optional alternate detector for pinching on the right hand.")]
    /** An optional component that will be used to detect pinch motions if set.
     * Primarily used for projective or hybrid interaction modes (under experimental features).
     */
    public Leap.Unity.PinchDetector RightHandDetector;
    [Tooltip("How many hands and pointers the Input Module should allocate for.")]
    /** The number of pointers to create. By default, one pointer is created for each hand. */
    int NumberOfPointers = 2;

    //Customizable Pointer Parameters
    [Header(" Pointer Setup")]
    [Tooltip("The sprite used to represent your pointers during projective interaction.")]
    /** The sprite for the cursor. */
    public Sprite PointerSprite;
    [Tooltip("The material to be instantiated for your pointers during projective interaction.")]
    /** The cursor material. */
    public Material PointerMaterial;
    [Tooltip("The color of the pointer when it is hovering over blank canvas.")]
    /** The color for the cursor when it is not in a special state. */
    public Color StandardColor = Color.white;
    [Tooltip("The color of the pointer when it is hovering over any other UI element.")]
    /** The color for the cursor when it is hovering over a control. */
    public Color HoveringColor = Color.green;
    [Tooltip("The color of the pointer when it is triggering a UI element.")]
    /** The color for the cursor when it is actively interacting with a control. */
    public Color TriggeringColor = Color.gray;
    [Tooltip("The color of the pointer when it is triggering blank canvas.")]
    /** The color for the cursor when it is touching or triggering a non-active part of the UI (such as the canvas). */
    public Color TriggerMissedColor = Color.gray;

    //Advanced Options
    [Header(" Advanced Options")]
    [Tooltip("Whether or not to show Advanced Options in the Inspector.")]
    public bool ShowAdvancedOptions = false;
    [Tooltip("The distance from the base of a UI element that tactile interaction is triggered.")]
    /** The distance from the base of a UI element that tactile interaction is triggered.*/
    public float TactilePadding = 0.005f;
    [Tooltip("The sound that is played when the pointer transitions from canvas to element.")]
    /** The sound that is played when the pointer transitions from canvas to element.*/
    public AudioClip BeginHoverSound;
    [Tooltip("The sound that is played when the pointer transitions from canvas to element.")]
    /** The sound that is played when the pointer transitions from canvas to element.*/
    public AudioClip EndHoverSound;
    [Tooltip("The sound that is played when the pointer triggers a UI element.")]
    /** The sound that is played when the pointer triggers a UI element.*/
    public AudioClip BeginTriggerSound;
    [Tooltip("The sound that is played when the pointer triggers a UI element.")]
    /** The sound that is played when the pointer triggers a UI element.*/
    public AudioClip EndTriggerSound;
    [Tooltip("The sound that is played when the pointer triggers blank canvas.")]
    /** The sound that is played when the pointer triggers blank canvas.*/
    public AudioClip BeginMissedSound;
    [Tooltip("The sound that is played when the pointer triggers blank canvas.")]
    /** The sound that is played when the pointer triggers blank canvas.*/
    public AudioClip EndMissedSound;
    [Tooltip("The sound that is played while the pointer is dragging an object.")]
    /** The sound that is played while the pointer is dragging an object.*/
    public AudioClip DragLoopSound;

    // Event delegates triggered by Input
    [System.Serializable]
    public class PositionEvent : UnityEvent<Vector3> { }

    [Header(" Event Setup")]
    [Tooltip("The event that is triggered upon clicking on a non-canvas UI element.")]
    /** The event that is triggered upon clicking on a non-canvas UI element.*/
    public PositionEvent onClickDown;
    [Tooltip("The event that is triggered upon lifting up from a non-canvas UI element (Not 1:1 with onClickDown!)")]
    /** The event that is triggered upon lifting up from a non-canvas UI element (Not 1:1 with onClickDown!)*/
    public PositionEvent onClickUp;
    [Tooltip("The event that is triggered upon hovering over a non-canvas UI element.")]
    /** The event that is triggered upon hovering over a non-canvas UI element.*/
    public PositionEvent onHover;
    [Tooltip("The event that is triggered while holding down a non-canvas UI element.")]
    /** The event that is triggered while holding down a non-canvas UI element.*/
    public PositionEvent whileClickHeld;

    [Tooltip("Whether or not to show unsupported Experimental Options in the Inspector.")]
    public bool ShowExperimentalOptions = false;
    /** Defines the interaction modes :
     *
     *  - Hybrid: Both tactile and projective interaction. The active mode depends on the ProjectiveToTactileTransitionDistance value.
     *
     *  - Tactile: The user must physically touch the controls.
     *
     *  - Projective: A cursor is projected from the user's knuckle.
     */
    public enum InteractionCapability : int {
      Hybrid,
      Tactile,
      Projective
    };
    [Tooltip("The interaction mode that the Input Module will be restricted to.")]
    /** The mode to use for interaction. The default mode is tactile. The projective mode is considered experimental.*/
    public InteractionCapability InteractionMode = InteractionCapability.Tactile;
    [Tooltip("The distance from the base of a UI element that interaction switches from Projective-Pointer based to Touch based.")]
    /** The distance from the canvas at which to switch to projective mode. */
    public float ProjectiveToTactileTransitionDistance = 0.4f;
    [Tooltip("The size of the pointer in world coordinates with respect to the distance between the cursor and the camera.")]
    /** The size of the pointer in world coordinates with respect to the distance between the cursor and the camera.*/
    public AnimationCurve PointerDistanceScale = AnimationCurve.Linear(0f, 0.1f, 6f, 1f);
    [Tooltip("The size of the pointer in world coordinates with respect to the distance between the thumb and forefinger.")]
    /** The size of the pointer in world coordinates with respect to the distance between the thumb and forefinger.*/
    public AnimationCurve PointerPinchScale = AnimationCurve.Linear(30f, 0.6f, 70f, 1.1f);
    [Tooltip("When not using a PinchDetector, the distance in mm that the tip of the thumb and forefinger should be to activate selection during projective interaction.")]
    /** When not using a PinchDetector, the distance in mm that the tip of the thumb and forefinger should be to activate selection during projective interaction.*/
    public float PinchingThreshold = 30f;
    [Tooltip("Create a pointer for each finger.")]
    /** Create a pointer for each finger.*/
    public bool perFingerPointer = false;
    [Tooltip("Render the pointer onto the enviroment.")]
    /** Render the pointer onto the enviroment.*/
    public bool EnvironmentPointer = false;
    [Tooltip("The event that is triggered while pinching to a point in the environment.")]
    /** The event that is triggered while pinching to a point in the environment.*/
    public PositionEvent environmentPinch;
    [Tooltip("Render a smaller pointer inside of the main pointer.")]
    /** Render a smaller pointer inside of the main pointer.*/
    public bool InnerPointer = true;
    [Tooltip("The Opacity of the Inner Pointer relative to the Primary Pointer.")]
    /** The Opacity of the Inner Pointer relative to the Primary Pointer.*/
    public float InnerPointerOpacityScalar = 0.77f;
    [Tooltip("Trigger a Hover Event when switching between UI elements.")]
    /** Trigger a Hover Event when switching between UI elements.*/
    public bool TriggerHoverOnElementSwitch = false;
    [Tooltip("If the ScrollView still doesn't work even after disabling RaycastTarget on the intermediate layers.")]
    /** If the ScrollView still doesn't work even after disabling RaycastTarget on the intermediate layers.*/
    public bool OverrideScrollViewClicks = false;
    [Tooltip("Draw the raycast for projective interaction.")]
    /** Draw the raycast for projective interaction.*/
    public bool DrawDebug = false;
    [Tooltip("Retract compressible widgets when not using Tactile Interaction.")]
    /** Retract compressible widgets when not using Tactile Interaction.*/
    public bool RetractUI = false;
    [Tooltip("Retransform the Interaction Pointer to allow the Module to work in a non-stationary reference frame.")]
    /** Retransform the Interaction Pointer to allow the Module to work in a non-stationary reference frame.*/
    public bool MovingReferenceFrame = false;

    //Event related data
    //private Camera EventCamera;
    private PointerEventData[] PointEvents;
    private pointerStates[] pointerState;
    private Transform[] Pointers;
    private Transform[] InnerPointers;
    private LineRenderer[] PointerLines;

    //Object the pointer is hovering over
    private GameObject[] currentOverGo;

    //Values from the previous frame
    private pointerStates[] PrevState;
    private Vector2[] PrevScreenPosition;
    private Vector2[] DragBeginPosition;
    private bool[] PrevTriggeringInteraction;
    private bool PrevTouchingMode;
    private GameObject[] prevOverGo;
    private float[] timeEnteredCanvas;

    //Misc. Objects
    private Canvas[] canvases;
    private Quaternion CurrentRotation;
    private AudioSource SoundPlayer;
    private GameObject[] currentGo;
    private GameObject[] currentGoing;
    private Vector3 OldCameraPos = Vector3.zero;
    private Quaternion OldCameraRot = Quaternion.identity;
    //private float OldCameraFoV;
    private bool forceProjective = false;
    private bool forceTactile = false;

    //Queue of Spheres to Debug Draw
    private Queue<Vector3> DebugSphereQueue;

    enum pointerStates : int {
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
    protected override void Start() {
      base.Start();

      if (LeapDataProvider == null) {
        LeapDataProvider = FindObjectOfType<LeapProvider>();
        if (LeapDataProvider == null || !LeapDataProvider.isActiveAndEnabled) {
          Debug.LogError("Cannot use LeapImageRetriever if there is no LeapProvider!");
          enabled = false;
          return;
        }
      }

      canvases = Resources.FindObjectsOfTypeAll<Canvas>();

      //Set Projective/Tactile Modes
      if (InteractionMode == InteractionCapability.Projective) {
        ProjectiveToTactileTransitionDistance = -float.MaxValue;
        forceTactile = false;
        forceProjective = true;
      } else if (InteractionMode == InteractionCapability.Tactile) {
        ProjectiveToTactileTransitionDistance = float.MaxValue;
        forceTactile = true;
        forceProjective = false;
      }

      //Initialize the Pointers for Projective Interaction
      if (perFingerPointer == true) {
        NumberOfPointers = 10;
      }
      Pointers = new Transform[NumberOfPointers];
      InnerPointers = new Transform[NumberOfPointers];
      PointerLines = new LineRenderer[NumberOfPointers];
      for (int index = 0; index < Pointers.Length; index++) {
        //Create the Canvas to render the Pointer on
        GameObject pointer = new GameObject("Pointer " + index);
        SpriteRenderer renderer = pointer.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 1000;

        //Add your sprite to the Sprite Renderer
        renderer.sprite = PointerSprite;
        renderer.material = Instantiate(PointerMaterial); //Make sure to instantiate the material so each pointer can be modified independently

        if (DrawDebug) {
          PointerLines[index] = pointer.AddComponent<LineRenderer>();
          PointerLines[index].material = Instantiate(PointerMaterial);
          PointerLines[index].material.color = new Color(0f, 0f, 0f, 0f);
#if UNITY_5_5_OR_NEWER
#if UNITY_5_6_OR_NEWER
          PointerLines[index].positionCount = 2;
#else
          PointerLines[index].numPositions = 2;
#endif
          PointerLines[index].startWidth = 0.001f;
          PointerLines[index].endWidth = 0.001f;
#else
          PointerLines[index].SetVertexCount(2);
          PointerLines[index].SetWidth(0.001f, 0.001f);
#endif
        }

        Pointers[index] = pointer.GetComponent<Transform>();
        Pointers[index].parent = transform;
        pointer.SetActive(false);

        if (InnerPointer) {
          //Create the Canvas to render the Pointer on
          GameObject innerPointer = new GameObject("Pointer " + index);
          renderer = innerPointer.AddComponent<SpriteRenderer>();
          renderer.sortingOrder = 1000;

          //Add your sprite to the Canvas
          renderer.sprite = PointerSprite;

          renderer.material = Instantiate(PointerMaterial);

          InnerPointers[index] = innerPointer.GetComponent<Transform>();
          InnerPointers[index].parent = transform;
          innerPointer.SetActive(false);
        }
      }


      //Initialize our Sound Player
      SoundPlayer = this.gameObject.AddComponent<AudioSource>();

      //Initialize the arrays that store persistent objects per pointer
      PointEvents = new PointerEventData[NumberOfPointers];
      pointerState = new pointerStates[NumberOfPointers];
      currentOverGo = new GameObject[NumberOfPointers];
      prevOverGo = new GameObject[NumberOfPointers];
      currentGo = new GameObject[NumberOfPointers];
      currentGoing = new GameObject[NumberOfPointers];
      PrevTriggeringInteraction = new bool[NumberOfPointers];
      PrevScreenPosition = new Vector2[NumberOfPointers];
      DragBeginPosition = new Vector2[NumberOfPointers];
      PrevState = new pointerStates[NumberOfPointers];
      timeEnteredCanvas = new float[NumberOfPointers];

      //Used for calculating the origin of the Projective Interactions
      if (Camera.main != null) {
        CurrentRotation = Camera.main.transform.rotation;
      } else {
        Debug.LogAssertion("Tag your Main Camera with 'MainCamera' for the UI Module");
      }

      //Initializes the Queue of Spheres to draw in OnDrawGizmos
      if (DrawDebug) {
        DebugSphereQueue = new Queue<Vector3>();
      }
    }

    //Update the Head Yaw for Calculating "Shoulder Positions"
    void Update() {
      if (Camera.main != null) {
        Quaternion HeadYaw = Quaternion.Euler(0f, OldCameraRot.eulerAngles.y, 0f);
        CurrentRotation = Quaternion.Slerp(CurrentRotation, HeadYaw, 0.1f);
      }
    }

    //Process is called by UI system to process events
    public override void Process() {
      if (MovingReferenceFrame) {
        (LeapDataProvider as LeapServiceProvider).RetransformFrames();
      }

      OldCameraPos = Camera.main.transform.position;
      OldCameraRot = Camera.main.transform.rotation;
      //OldCameraFoV = Camera.main.fieldOfView;

      //Send update events if there is a selected object
      //This is important for InputField to receive keyboard events
      SendUpdateEventToSelectedObject();

      //Begin Processing Each Hand
      for (int whichPointer = 0; whichPointer < NumberOfPointers; whichPointer++) {
        int whichHand;
        int whichFinger;
        if (perFingerPointer) {
          whichHand = whichPointer <= 4 ? 0 : 1;
          whichFinger = whichPointer <= 4 ? whichPointer : whichPointer - 5;
          //Move on if this hand isn't visible in the frame
          if (LeapDataProvider.CurrentFrame.Hands.Count - 1 < whichHand) {
            if (Pointers[whichPointer].gameObject.activeInHierarchy == true) {
              Pointers[whichPointer].gameObject.SetActive(false);
              if (InnerPointer) {
                InnerPointers[whichPointer].gameObject.SetActive(false);
              }
            }
            continue;
          }
        } else {
          whichHand = whichPointer;
          whichFinger = 1;
          //Move on if this hand isn't visible in the frame
          if (LeapDataProvider.CurrentFrame.Hands.Count - 1 < whichHand) {
            if (Pointers[whichPointer].gameObject.activeInHierarchy == true) {
              Pointers[whichPointer].gameObject.SetActive(false);
              if (InnerPointer) {
                InnerPointers[whichPointer].gameObject.SetActive(false);
              }
            }
            continue;
          }
        }

        //Calculate Shoulder Positions (for Projection)
        Vector3 ProjectionOrigin = Vector3.zero;
        if (Camera.main != null) {
          switch (LeapDataProvider.CurrentFrame.Hands[whichHand].IsRight) {
            case true:
              ProjectionOrigin = OldCameraPos + CurrentRotation * new Vector3(0.15f, -0.2f, 0f);
              break;
            case false:
              ProjectionOrigin = OldCameraPos + CurrentRotation * new Vector3(-0.15f, -0.2f, 0f);
              break;
          }
        }

        //Draw Shoulders as Spheres, and the Raycast as a Line
        if (DrawDebug) {
          DebugSphereQueue.Enqueue(ProjectionOrigin);
          Debug.DrawRay(ProjectionOrigin, CurrentRotation * Vector3.forward * 5f);
        }

        //Raycast from shoulder through tip of the index finger to the UI
        bool TipRaycast = false;
        if (InteractionMode != InteractionCapability.Projective) {
          TipRaycast = GetLookPointerEventData(whichPointer, whichHand, whichFinger, ProjectionOrigin, CurrentRotation * Vector3.forward, true);
          PrevState[whichPointer] = pointerState[whichPointer]; //Store old state for sound transitionary purposes
          UpdatePointer(whichPointer, PointEvents[whichPointer], PointEvents[whichPointer].pointerCurrentRaycast.gameObject);
          ProcessState(whichPointer, whichHand, whichFinger, TipRaycast);
        }

        //If didn't hit anything near the fingertip, try doing it again, but through the knuckle this time
        if (((pointerState[whichPointer] == pointerStates.OffCanvas) && (InteractionMode != InteractionCapability.Tactile)) || (InteractionMode == InteractionCapability.Projective)) {
          TipRaycast = GetLookPointerEventData(whichPointer, whichHand, whichFinger, ProjectionOrigin, CurrentRotation * Vector3.forward, false);
          if ((InteractionMode == InteractionCapability.Projective)) {
            PrevState[whichPointer] = pointerState[whichPointer]; //Store old state for sound transitionary purposes
          }
          UpdatePointer(whichPointer, PointEvents[whichPointer], PointEvents[whichPointer].pointerCurrentRaycast.gameObject);
          if (!TipRaycast && (forceTactile || (!forceProjective && distanceOfTipToPointer(whichPointer, whichHand, whichFinger) < ProjectiveToTactileTransitionDistance))) {
            PointEvents[whichPointer].pointerCurrentRaycast = new RaycastResult();
          }
          ProcessState(whichPointer, whichHand, whichFinger, TipRaycast);
        }

        //Handle the Environment Pointer
        if ((EnvironmentPointer) && (pointerState[whichPointer] == pointerStates.OffCanvas)) {
          Vector3 IndexMetacarpal = LeapDataProvider.CurrentFrame.Hands[whichHand].Fingers[whichFinger].Bone(Bone.BoneType.TYPE_METACARPAL).Center.ToVector3();
          RaycastHit EnvironmentSpot;
          Physics.Raycast(ProjectionOrigin, (IndexMetacarpal - ProjectionOrigin).normalized, out EnvironmentSpot);
          Pointers[whichPointer].position = EnvironmentSpot.point + (EnvironmentSpot.normal * 0.01f);
          Pointers[whichPointer].rotation = Quaternion.LookRotation(EnvironmentSpot.normal);
          if (InnerPointer) {
            InnerPointers[whichPointer].position = EnvironmentSpot.point + (EnvironmentSpot.normal * 0.01f);
            InnerPointers[whichPointer].rotation = Quaternion.LookRotation(EnvironmentSpot.normal);
          }
          evaluatePointerSize(whichPointer);

          if (isTriggeringInteraction(whichPointer, whichHand, whichFinger)) {
            environmentPinch.Invoke(Pointers[whichPointer].position);
          }
        }

        PrevScreenPosition[whichPointer] = PointEvents[whichPointer].position;

        if (DrawDebug) {
          PointerLines[whichPointer].SetPosition(0, Camera.main.transform.position);
          PointerLines[whichPointer].SetPosition(1, Pointers[whichPointer].position);
        }

        //Trigger events that come from changing pointer state
        ProcessStateEvents(whichPointer);
        if ((PointEvents[whichPointer] != null)) {
          //Tell Leap Buttons how far away the finger is
          GameObject Hoverer = ExecuteEvents.GetEventHandler<IPointerClickHandler>(PointEvents[whichPointer].pointerCurrentRaycast.gameObject);
          if ((Hoverer != null)) {
            ILeapWidget comp = Hoverer.GetComponent<ILeapWidget>();
            if (comp == null) { comp = Hoverer.GetComponentInParent<ILeapWidget>(); }
            if (comp != null) {
              //if (!isTriggeringInteraction(whichPointer, whichHand, whichFinger)) { //I forget why I put this here....
              ((ILeapWidget)comp).HoverDistance(distanceOfTipToPointer(whichPointer, whichHand, whichFinger));
              //}
            }
          }

          //If we hit something with our Raycast, let's see if we should interact with it
          if (PointEvents[whichPointer].pointerCurrentRaycast.gameObject != null && pointerState[whichPointer] != pointerStates.OffCanvas) {
            prevOverGo[whichPointer] = currentOverGo[whichPointer];
            currentOverGo[whichPointer] = PointEvents[whichPointer].pointerCurrentRaycast.gameObject;

            //Trigger Enter or Exit Events on the UI Element (like highlighting)
            base.HandlePointerExitAndEnter(PointEvents[whichPointer], currentOverGo[whichPointer]);

            //If we weren't triggering an interaction last frame, but we are now...
            if (!PrevTriggeringInteraction[whichPointer] && isTriggeringInteraction(whichPointer, whichHand, whichFinger)) {
              PrevTriggeringInteraction[whichPointer] = true;

              if ((Time.time - timeEnteredCanvas[whichPointer] >= Time.deltaTime)) {
                //Deselect all objects
                if (base.eventSystem.currentSelectedGameObject) {
                  base.eventSystem.SetSelectedGameObject(null);
                }

                //Record pointer telemetry
                PointEvents[whichPointer].pressPosition = PointEvents[whichPointer].position;
                PointEvents[whichPointer].pointerPressRaycast = PointEvents[whichPointer].pointerCurrentRaycast;
                PointEvents[whichPointer].pointerPress = null; //Clear this for setting later
                PointEvents[whichPointer].useDragThreshold = true;

                //If we hit something good, let's trigger it!
                if (currentOverGo[whichPointer] != null) {
                  currentGo[whichPointer] = currentOverGo[whichPointer];

                  //See if this object, or one of its parents, has a pointerDownHandler
                  GameObject newPressed = ExecuteEvents.ExecuteHierarchy(currentGo[whichPointer], PointEvents[whichPointer], ExecuteEvents.pointerDownHandler);

                  //If not, see if one has a pointerClickHandler!
                  if (newPressed == null) {
                    newPressed = ExecuteEvents.ExecuteHierarchy(currentGo[whichPointer], PointEvents[whichPointer], ExecuteEvents.pointerClickHandler);
                    if (newPressed != null) {
                      currentGo[whichPointer] = newPressed;
                    }
                  } else {
                    currentGo[whichPointer] = newPressed;
                    //We want to do "click on button down" at same time, unlike regular mouse processing
                    //Which does click when mouse goes up over same object it went down on
                    //This improves the user's ability to select small menu items
                    //ExecuteEvents.Execute(newPressed, PointEvents[whichPointer], ExecuteEvents.pointerClickHandler);
                  }

                  if (newPressed != null) {
                    PointEvents[whichPointer].pointerPress = newPressed;
                    currentGo[whichPointer] = newPressed;

                    //Select the currently pressed object
                    if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentGo[whichPointer])) {
                      base.eventSystem.SetSelectedGameObject(currentGo[whichPointer]);
                    }
                  }

                  //Debug.Log(currentGo[whichPointer].name);
                  PointEvents[whichPointer].pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentGo[whichPointer]);
                  //Debug.Log(PointEvents[whichPointer].pointerDrag.name);

                  if (PointEvents[whichPointer].pointerDrag) {
                    IDragHandler Dragger = PointEvents[whichPointer].pointerDrag.GetComponent<IDragHandler>();
                    if (Dragger != null) {
                      if (Dragger is EventTrigger && PointEvents[whichPointer].pointerDrag.transform.parent) { //Hack: EventSystems intercepting Drag Events causing funkiness
                        PointEvents[whichPointer].pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(PointEvents[whichPointer].pointerDrag.transform.parent.gameObject);
                        if (PointEvents[whichPointer].pointerDrag != null) {
                          Dragger = PointEvents[whichPointer].pointerDrag.GetComponent<IDragHandler>();
                          if ((Dragger != null) && !(Dragger is EventTrigger)) {
                            currentGoing[whichPointer] = PointEvents[whichPointer].pointerDrag;
                            DragBeginPosition[whichPointer] = PointEvents[whichPointer].position;
                            if (currentGo[whichPointer] && currentGo[whichPointer] == currentGoing[whichPointer]) {
                              ExecuteEvents.Execute(PointEvents[whichPointer].pointerDrag, PointEvents[whichPointer], ExecuteEvents.beginDragHandler);
                              PointEvents[whichPointer].dragging = true;
                            }
                          }
                        }
                      } else {
                        currentGoing[whichPointer] = PointEvents[whichPointer].pointerDrag;
                        DragBeginPosition[whichPointer] = PointEvents[whichPointer].position;
                        if (currentGo[whichPointer] && currentGo[whichPointer] == currentGoing[whichPointer]) {
                          ExecuteEvents.Execute(PointEvents[whichPointer].pointerDrag, PointEvents[whichPointer], ExecuteEvents.beginDragHandler);
                          PointEvents[whichPointer].dragging = true;
                        }
                      }
                    }
                  }
                }
              }
            }
          }


          //If we have dragged beyond the drag threshold
          if (!PointEvents[whichPointer].dragging && currentGoing[whichPointer] && Vector2.Distance(PointEvents[whichPointer].position, DragBeginPosition[whichPointer]) * 100f > EventSystem.current.pixelDragThreshold) {
            IDragHandler Dragger = PointEvents[whichPointer].pointerDrag.GetComponent<IDragHandler>();
            if (Dragger != null && Dragger is ScrollRect) {
              if (currentGo[whichPointer] && !(currentGo[whichPointer].GetComponent<ScrollRect>())) {
                ExecuteEvents.Execute(PointEvents[whichPointer].pointerDrag, PointEvents[whichPointer], ExecuteEvents.beginDragHandler);
                PointEvents[whichPointer].dragging = true;

                ExecuteEvents.Execute(currentGo[whichPointer], PointEvents[whichPointer], ExecuteEvents.pointerUpHandler);
                PointEvents[whichPointer].rawPointerPress = null;
                PointEvents[whichPointer].pointerPress = null;
                currentGo[whichPointer] = null;
              }
            }
          }


          //If we WERE interacting last frame, but are not this frame...
          if (PrevTriggeringInteraction[whichPointer] && ((!isTriggeringInteraction(whichPointer, whichHand, whichFinger)) || (pointerState[whichPointer] == pointerStates.OffCanvas))) {
            PrevTriggeringInteraction[whichPointer] = false;

            if (currentGoing[whichPointer]) {
              ExecuteEvents.Execute(currentGoing[whichPointer], PointEvents[whichPointer], ExecuteEvents.endDragHandler);
              if ((currentGo[whichPointer]) && (currentGoing[whichPointer] == currentGo[whichPointer])) {
                ExecuteEvents.Execute(currentGoing[whichPointer], PointEvents[whichPointer], ExecuteEvents.pointerUpHandler);
              }
              //Debug.Log(currentGoing[whichPointer].name);
              if (currentOverGo[whichPointer] != null) {
                ExecuteEvents.ExecuteHierarchy(currentOverGo[whichPointer], PointEvents[whichPointer], ExecuteEvents.dropHandler);
              }
              PointEvents[whichPointer].pointerDrag = null;
              PointEvents[whichPointer].dragging = false;
              currentGoing[whichPointer] = null;
            }

            if (currentGo[whichPointer]) {
              ExecuteEvents.Execute(currentGo[whichPointer], PointEvents[whichPointer], ExecuteEvents.pointerUpHandler);
              ExecuteEvents.Execute(currentGo[whichPointer], PointEvents[whichPointer], ExecuteEvents.pointerClickHandler);
              PointEvents[whichPointer].rawPointerPress = null;
              PointEvents[whichPointer].pointerPress = null;
              currentGo[whichPointer] = null;
              currentGoing[whichPointer] = null;
            }
          }

          //And for everything else, there is dragging.
          if (PointEvents[whichPointer].pointerDrag != null && PointEvents[whichPointer].dragging) {
            ExecuteEvents.Execute(PointEvents[whichPointer].pointerDrag, PointEvents[whichPointer], ExecuteEvents.dragHandler);
          }
        }

        updatePointerColor(whichPointer, whichHand, whichFinger);


        //Make the special Leap Widget Buttons Pop Up and Flatten when Appropriate
        if (PrevTouchingMode != getTouchingMode() && RetractUI) {
          PrevTouchingMode = getTouchingMode();
          if (PrevTouchingMode) {
            for (int i = 0; i < canvases.Length; i++) {
              canvases[i].BroadcastMessage("Expand", SendMessageOptions.DontRequireReceiver);
            }
          } else {
            for (int i = 0; i < canvases.Length; i++) {
              canvases[i].BroadcastMessage("Retract", SendMessageOptions.DontRequireReceiver);
            }
          }
        }

      }

      Camera.main.transform.position = OldCameraPos;
      Camera.main.transform.rotation = OldCameraRot;
      //Camera.main.fieldOfView = OldCameraFoV;
    }

    //Raycast from the EventCamera into UI Space
    private bool GetLookPointerEventData(int whichPointer, int whichHand, int whichFinger, Vector3 Origin, Vector3 Direction, bool forceTipRaycast) {

      //Whether or not this will be a raycast through the finger tip
      bool TipRaycast = false;

      //Initialize a blank PointerEvent
      if (PointEvents[whichPointer] == null) {
        PointEvents[whichPointer] = new PointerEventData(base.eventSystem);
      } else {
        PointEvents[whichPointer].Reset();
      }

      //We're always going to assume we're "Left Clicking", for the benefit of uGUI
      PointEvents[whichPointer].button = PointerEventData.InputButton.Left;

      //If we're in "Touching Mode", Raycast through the fingers
      Vector3 IndexFingerPosition;
      if (getTouchingMode(whichPointer) || forceTipRaycast) {
        TipRaycast = true;

        //Focus pointer through the average of the extended fingers
        if (!perFingerPointer) {
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
          IndexFingerPosition = LeapDataProvider.CurrentFrame.Hands[whichHand].Fingers[1].TipPosition.ToVector3();
          for (int i = 1; i < 3; i++) {
            float fingerDistance = Vector3.Distance(Camera.main.transform.position, LeapDataProvider.CurrentFrame.Hands[whichHand].Fingers[i].TipPosition.ToVector3());
            float fingerExtension = Mathf.Clamp01(Vector3.Dot(LeapDataProvider.CurrentFrame.Hands[whichHand].Fingers[i].Direction.ToVector3(), LeapDataProvider.CurrentFrame.Hands[whichPointer].Direction.ToVector3())) / 1.5f;
            if (fingerDistance > farthest && fingerExtension > 0.5f) {
              farthest = fingerDistance;
              IndexFingerPosition = LeapDataProvider.CurrentFrame.Hands[whichHand].Fingers[i].TipPosition.ToVector3();
            }
          }
        } else {
          IndexFingerPosition = LeapDataProvider.CurrentFrame.Hands[whichHand].Fingers[whichFinger].TipPosition.ToVector3();
        }

        //Else Raycast through the knuckle of the Index Finger
      } else {
        Camera.main.transform.position = Origin;
        IndexFingerPosition = LeapDataProvider.CurrentFrame.Hands[whichHand].Fingers[whichFinger].Bone(Bone.BoneType.TYPE_METACARPAL).Center.ToVector3();
      }

      //Draw Camera Origin
      if (DrawDebug)
        DebugSphereQueue.Enqueue(Camera.main.transform.position);

      //Set EventCamera's FoV
      //Camera.main.fieldOfView = 179f;

      //Set the Raycast Direction and Delta
      PointEvents[whichPointer].position = Vector2.Lerp(PrevScreenPosition[whichPointer], Camera.main.WorldToScreenPoint(IndexFingerPosition), 1.0f);//new Vector2(Screen.width / 2, Screen.height / 2);
      PointEvents[whichPointer].delta = (PointEvents[whichPointer].position - PrevScreenPosition[whichPointer]) * -10f;
      PointEvents[whichPointer].scrollDelta = Vector2.zero;

      //Perform the Raycast and sort all the things we hit by distance...
      base.eventSystem.RaycastAll(PointEvents[whichPointer], m_RaycastResultCache);

      //Optional hack that subverts ScrollRect hierarchies; to avoid this, disable "RaycastTarget" on the Viewport and Content panes
      if (OverrideScrollViewClicks) {
        PointEvents[whichPointer].pointerCurrentRaycast = new RaycastResult();
        for (int i = 0; i < m_RaycastResultCache.Count; i++) {
          if (m_RaycastResultCache[i].gameObject.GetComponent<Scrollbar>() != null) {
            PointEvents[whichPointer].pointerCurrentRaycast = m_RaycastResultCache[i];
          } else if (PointEvents[whichPointer].pointerCurrentRaycast.gameObject == null && m_RaycastResultCache[i].gameObject.GetComponent<ScrollRect>() != null) {
            PointEvents[whichPointer].pointerCurrentRaycast = m_RaycastResultCache[i];
          }
        }
        if (PointEvents[whichPointer].pointerCurrentRaycast.gameObject == null) {
          PointEvents[whichPointer].pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
        }
      } else {
        PointEvents[whichPointer].pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
      }

      //Clear the list of things we hit; we don't need it anymore.
      m_RaycastResultCache.Clear();

      return TipRaycast;
    }

    //Tree to decide the State of the Pointer
    private void ProcessState(int whichPointer, int whichHand, int whichFinger, bool forceTipRaycast) {
      if ((PointEvents[whichPointer].pointerCurrentRaycast.gameObject != null)) {
        if (forceTactile || (!forceProjective && distanceOfTipToPointer(whichPointer, whichHand, whichFinger) < ProjectiveToTactileTransitionDistance)) {
          if (isTriggeringInteraction(whichPointer, whichHand, whichFinger)) {
            if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(PointEvents[whichPointer].pointerCurrentRaycast.gameObject)) {
              pointerState[whichPointer] = pointerStates.TouchingElement;
            } else {
              pointerState[whichPointer] = pointerStates.TouchingCanvas;
            }
          } else {
            pointerState[whichPointer] = pointerStates.NearCanvas;
          }
        } else if (!forceTipRaycast) {
          if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(PointEvents[whichPointer].pointerCurrentRaycast.gameObject)) {// || PointEvents[whichPointer].dragging) {
            if (isTriggeringInteraction(whichPointer, whichHand, whichFinger)) {
              pointerState[whichPointer] = pointerStates.PinchingToElement;
            } else {
              pointerState[whichPointer] = pointerStates.OnElement;
            }
          } else {
            if (isTriggeringInteraction(whichPointer, whichHand, whichFinger)) {
              pointerState[whichPointer] = pointerStates.PinchingToCanvas;
            } else {
              pointerState[whichPointer] = pointerStates.OnCanvas;
            }
          }
        } else {
          pointerState[whichPointer] = pointerStates.OffCanvas;
        }
      } else {
        pointerState[whichPointer] = pointerStates.OffCanvas;
      }
    }

    //Discrete 1-Frame Transition Behaviors like Sounds and Events
    //(color changing is in a different function since it is lerped over multiple frames)
    private void ProcessStateEvents(int whichPointer) {
      if (TriggerHoverOnElementSwitch) {
        if ((PrevState[whichPointer] != pointerStates.OffCanvas) && (pointerState[whichPointer] != pointerStates.OffCanvas)) {
          if (currentOverGo[whichPointer] != prevOverGo[whichPointer]) {
            //When you begin to hover on an element
            SoundPlayer.PlayOneShot(BeginHoverSound);
            onHover.Invoke(Pointers[whichPointer].transform.position);
          }
        }
      }

      //Warning: Horrible State Machine ahead...
      if (PrevState[whichPointer] == pointerStates.OnCanvas) {
        if (pointerState[whichPointer] == pointerStates.OnElement) {
          //When you go from hovering on the Canvas to hovering on an element
          if (!TriggerHoverOnElementSwitch) {
            SoundPlayer.PlayOneShot(BeginHoverSound);
            onHover.Invoke(Pointers[whichPointer].transform.position);
          }
        } else if (pointerState[whichPointer] == pointerStates.PinchingToCanvas) {
          //When you try to interact with the Canvas
          SoundPlayer.PlayOneShot(BeginMissedSound);
        }
      } else if (PrevState[whichPointer] == pointerStates.PinchingToCanvas) {
        if (pointerState[whichPointer] == pointerStates.OnCanvas) {
          //When you unpinch off of Blank Canvas
          SoundPlayer.PlayOneShot(EndMissedSound);
        }
      } else if (PrevState[whichPointer] == pointerStates.OnElement) {
        if (pointerState[whichPointer] == pointerStates.OnCanvas) {
          //When you begin to hover over the Canvas after hovering over an element
          SoundPlayer.PlayOneShot(EndHoverSound);
        } else if (pointerState[whichPointer] == pointerStates.PinchingToElement) {
          //When you click on an element
          SoundPlayer.PlayOneShot(BeginTriggerSound);
          onClickDown.Invoke(Pointers[whichPointer].transform.position);
        }
      } else if (PrevState[whichPointer] == pointerStates.PinchingToElement) {
        if (pointerState[whichPointer] == pointerStates.PinchingToCanvas) {
          //When you slide off of an element while holding it
          //SoundPlayer.PlayOneShot(HoverSound);
        } else if (pointerState[whichPointer] == pointerStates.OnElement || pointerState[whichPointer] == pointerStates.OnCanvas) {
          //When you let go of an element
          SoundPlayer.PlayOneShot(EndTriggerSound);
          onClickUp.Invoke(Pointers[whichPointer].transform.position);
        }
      } else if (PrevState[whichPointer] == pointerStates.NearCanvas) {
        if (pointerState[whichPointer] == pointerStates.TouchingElement) {
          //When you physically touch an element
          SoundPlayer.PlayOneShot(BeginTriggerSound);
          onClickDown.Invoke(Pointers[whichPointer].transform.position);
        }
        if (pointerState[whichPointer] == pointerStates.TouchingCanvas) {
          //When you physically touch Blank Canvas
          SoundPlayer.PlayOneShot(BeginMissedSound);
        }
      } else if (PrevState[whichPointer] == pointerStates.TouchingCanvas) {
        if (pointerState[whichPointer] == pointerStates.NearCanvas) {
          //When you physically lift off of Blank Canvas
          SoundPlayer.PlayOneShot(EndMissedSound);
        }
      } else if (PrevState[whichPointer] == pointerStates.TouchingElement) {
        if (pointerState[whichPointer] == pointerStates.NearCanvas) {
          //When you physically pull out of an element
          SoundPlayer.PlayOneShot(EndTriggerSound);
          onClickUp.Invoke(Pointers[whichPointer].transform.position);
        }
      } else if (PrevState[whichPointer] == pointerStates.OffCanvas) {
        if (pointerState[whichPointer] != pointerStates.OffCanvas) {
          //Record the time the hand entered an interactable state
          timeEnteredCanvas[whichPointer] = Time.time;
        }
      }
    }

    //Update the pointer location and whether or not it is enabled
    private void UpdatePointer(int whichPointer, PointerEventData pointData, GameObject UIComponent) {
      if (EnvironmentPointer && pointerState[whichPointer] == pointerStates.OffCanvas) {
        Pointers[whichPointer].gameObject.SetActive(true);
        if (InnerPointer) { InnerPointers[whichPointer].gameObject.SetActive(true); }
      }
      if (currentOverGo[whichPointer] != null) {
        Pointers[whichPointer].gameObject.SetActive(true);
        if (InnerPointer) { InnerPointers[whichPointer].gameObject.SetActive(true); }
        if (PointEvents[whichPointer].pointerCurrentRaycast.gameObject != null) {
          RectTransform draggingPlane = PointEvents[whichPointer].pointerCurrentRaycast.gameObject.GetComponent<RectTransform>();
          Vector3 globalLookPos;
          if (RectTransformUtility.ScreenPointToWorldPointInRectangle(draggingPlane, pointData.position, pointData.enterEventCamera, out globalLookPos)) {

            GameObject Hoverer = ExecuteEvents.GetEventHandler<IPointerEnterHandler>(UIComponent);
            if (Hoverer) {
              Vector3 ComponentInPlane = Hoverer.transform.InverseTransformPoint(globalLookPos);
              ComponentInPlane = new Vector3(ComponentInPlane.x, ComponentInPlane.y, 0f);
              Pointers[whichPointer].position = Hoverer.transform.TransformPoint(ComponentInPlane);// -transform.forward * 0.01f; //Amount the pointer floats above the Canvas
            } else {
              Pointers[whichPointer].position = globalLookPos;
            }

            float pointerAngle = Mathf.Rad2Deg * (Mathf.Atan2(pointData.delta.x, pointData.delta.y));
            Pointers[whichPointer].rotation = draggingPlane.rotation * Quaternion.Euler(0f, 0f, -pointerAngle);
            if (InnerPointer) {
              InnerPointers[whichPointer].position = globalLookPos;// -transform.forward * 0.01f; //Amount the pointer floats above the Canvas
              InnerPointers[whichPointer].rotation = draggingPlane.rotation * Quaternion.Euler(0f, 0f, -pointerAngle);
            }
            evaluatePointerSize(whichPointer);
          }
        }
      }
    }


    void evaluatePointerSize(int whichPointer) {
      //Use the Scale AnimCurve to Evaluate the Size of the Pointer
      float PointDistance = 1f;
      if (Camera.main != null) {
        PointDistance = (Pointers[whichPointer].position - Camera.main.transform.position).magnitude;
      }

      float Pointerscale = PointerDistanceScale.Evaluate(PointDistance);

      if (InnerPointer) { InnerPointers[whichPointer].localScale = Pointerscale * PointerPinchScale.Evaluate(0f) * Vector3.one; }

      if (!perFingerPointer && !getTouchingMode(whichPointer)) {
        if (whichPointer == 0) {
          Pointerscale *= PointerPinchScale.Evaluate(LeapDataProvider.CurrentFrame.Hands[0].PinchDistance);
        } else if (whichPointer == 1) {
          Pointerscale *= PointerPinchScale.Evaluate(LeapDataProvider.CurrentFrame.Hands[1].PinchDistance);
        }
      }

      //Commented out Velocity Stretching because it looks funny when switching between Tactile and Projective
      Pointers[whichPointer].localScale = Pointerscale * new Vector3(1f, 1f /*+ pointData.delta.magnitude*1f*/, 1f);
    }

    /** A boolean function that returns true if a "click" is being triggered during the current frame. */
    public bool isTriggeringInteraction(int whichPointer, int whichHand, int whichFinger) {

      if (InteractionMode != InteractionCapability.Projective) {
        if (getTouchingMode(whichPointer)) {
          return (distanceOfTipToPointer(whichPointer, whichHand, whichFinger) < 0f);
        }
      }

      if (InteractionMode != InteractionCapability.Tactile) {
        if ((LeapDataProvider.CurrentFrame.Hands[whichHand].IsRight) && (RightHandDetector != null && RightHandDetector.IsPinching) || (RightHandDetector == null && LeapDataProvider.CurrentFrame.Hands[whichHand].PinchDistance < PinchingThreshold)) {
          return true;
        } else if ((LeapDataProvider.CurrentFrame.Hands[whichHand].IsLeft) && (LeftHandDetector != null && LeftHandDetector.IsPinching) || (LeftHandDetector == null && LeapDataProvider.CurrentFrame.Hands[whichHand].PinchDistance < PinchingThreshold)) {
          return true;
        }
      }

      //Disabling Pinching during touch interactions; maybe still desirable?
      //return LeapDataProvider.CurrentFrame.Hands[whichPointer].PinchDistance < PinchingThreshold;

      return false;
    }

    /** The z position of the index finger tip to the Pointer. */
    public float distanceOfTipToPointer(int whichPointer, int whichHand, int whichFinger) {
      //Get Base of Index Finger Position
      Vector3 TipPosition = LeapDataProvider.CurrentFrame.Hands[whichHand].Fingers[whichFinger].Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();
      return (-Pointers[whichPointer].InverseTransformPoint(TipPosition).z * Pointers[whichPointer].lossyScale.z) - TactilePadding;
    }

    /** The z position of the index finger tip to the specified transform. */
    public float distanceOfTipToElement(Transform UIElement, int whichHand, int whichFinger) {
      //Get Base of Index Finger Position
      Vector3 TipPosition = LeapDataProvider.CurrentFrame.Hands[whichHand].Fingers[whichFinger].Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();
      return (-UIElement.InverseTransformPoint(TipPosition).z * UIElement.lossyScale.z) - TactilePadding;
    }

    /** Returns true if any active pointer is in the "touching" interaction mode, i.e, whether it is touching or nearly touching a canvas or control. */
    public bool getTouchingMode() {
      bool mode = false;
      for (int i = 0; i < pointerState.Length; i++) {
        if (pointerState[i] == pointerStates.NearCanvas || pointerState[i] == pointerStates.TouchingCanvas || pointerState[i] == pointerStates.TouchingElement) {
          mode = true;
        }
      }
      return mode;
    }

    /** Returns true if the specified pointer is in the "touching" interaction mode, i.e, whether it is touching or nearly touching a canvas or control. */
    public bool getTouchingMode(int whichPointer) {
      return (pointerState[whichPointer] == pointerStates.NearCanvas || pointerState[whichPointer] == pointerStates.TouchingCanvas || pointerState[whichPointer] == pointerStates.TouchingElement);
    }

    //Where the color that the Pointer will lerp to is chosen
    void updatePointerColor(int whichPointer, int whichHand, int whichFinger) {
      float TransitionAmount = Mathf.Clamp01(Mathf.Abs((distanceOfTipToPointer(whichPointer, whichHand, whichFinger) - ProjectiveToTactileTransitionDistance)) / 0.05f);

      switch (pointerState[whichPointer]) {
        case pointerStates.OnCanvas:
          lerpPointerColor(whichPointer, new Color(0f, 0f, 0f, 1f * TransitionAmount), 0.2f);
          lerpPointerColor(whichPointer, StandardColor, 0.2f);
          break;
        case pointerStates.OnElement:
          lerpPointerColor(whichPointer, new Color(0f, 0f, 0f, 1f * TransitionAmount), 0.2f);
          lerpPointerColor(whichPointer, HoveringColor, 0.2f);
          break;
        case pointerStates.PinchingToCanvas:
          lerpPointerColor(whichPointer, new Color(0f, 0f, 0f, 1f * TransitionAmount), 0.2f);
          lerpPointerColor(whichPointer, TriggerMissedColor, 0.2f);
          break;
        case pointerStates.PinchingToElement:
          lerpPointerColor(whichPointer, new Color(0f, 0f, 0f, 1f * TransitionAmount), 0.2f);
          lerpPointerColor(whichPointer, TriggeringColor, 0.2f);
          break;
        case pointerStates.NearCanvas:
          lerpPointerColor(whichPointer, new Color(0.0f, 0.0f, 0.0f, 0.5f * TransitionAmount), 0.3f);
          lerpPointerColor(whichPointer, StandardColor, 0.2f);
          break;
        case pointerStates.TouchingElement:
          lerpPointerColor(whichPointer, new Color(0.0f, 0.0f, 0.0f, 0.7f * TransitionAmount), 0.2f);
          lerpPointerColor(whichPointer, TriggeringColor, 0.2f);
          break;
        case pointerStates.TouchingCanvas:
          lerpPointerColor(whichPointer, new Color(0.0f, 0.01f, 0.0f, 0.5f * TransitionAmount), 0.2f);
          lerpPointerColor(whichPointer, TriggerMissedColor, 0.2f);
          break;
        case pointerStates.OffCanvas:
          lerpPointerColor(whichPointer, TriggerMissedColor, 0.2f);
          if (EnvironmentPointer) {
            lerpPointerColor(whichPointer, new Color(0.0f, 0.0f, 0.0f, 0.5f * TransitionAmount), 1f);
          } else {
            lerpPointerColor(whichPointer, new Color(0.0f, 0.0f, 0.0f, 0.001f), 1f);
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
    public void lerpPointerColor(int whichPointer, Color color, float lerpalpha) {
      SpriteRenderer PointerSprite = Pointers[whichPointer].GetComponent<SpriteRenderer>();
      Color oldColor = PointerSprite.color;
      if (color.r == 0f && color.g == 0f && color.b == 0f) {
        PointerSprite.material.color = Color.Lerp(oldColor, new Color(oldColor.r, oldColor.g, oldColor.b, color.a), lerpalpha);
        PointerSprite.color = Color.Lerp(oldColor, new Color(oldColor.r, oldColor.g, oldColor.b, color.a), lerpalpha);
      } else if (color.a == 1f) {
        PointerSprite.material.color = Color.Lerp(oldColor, new Color(color.r, color.g, color.b, oldColor.a), lerpalpha);
        PointerSprite.color = Color.Lerp(oldColor, new Color(color.r, color.g, color.b, oldColor.a), lerpalpha);
      } else {
        PointerSprite.material.color = Color.Lerp(oldColor, color, lerpalpha);
        PointerSprite.color = Color.Lerp(oldColor, color, lerpalpha);
      }

      if (InnerPointer) {
        SpriteRenderer InnerPointerSprite = InnerPointers[whichPointer].GetComponent<SpriteRenderer>();
        oldColor = InnerPointerSprite.color;
        if (color.r == 0f && color.g == 0f && color.b == 0f) {
          InnerPointerSprite.material.color = Color.Lerp(oldColor, new Color(oldColor.r, oldColor.g, oldColor.b, color.a * InnerPointerOpacityScalar), lerpalpha);
          InnerPointerSprite.color = Color.Lerp(oldColor, new Color(oldColor.r, oldColor.g, oldColor.b, color.a * InnerPointerOpacityScalar), lerpalpha);
        } else if (color.a == 1f) {
          InnerPointerSprite.material.color = Color.Lerp(oldColor, new Color(color.r, color.g, color.b, oldColor.a * InnerPointerOpacityScalar), lerpalpha);
          InnerPointerSprite.color = Color.Lerp(oldColor, new Color(color.r, color.g, color.b, oldColor.a * InnerPointerOpacityScalar), lerpalpha);
        } else {
          InnerPointerSprite.material.color = Color.Lerp(oldColor, new Color(color.r, color.g, color.b, color.a * InnerPointerOpacityScalar), lerpalpha);
          InnerPointerSprite.color = Color.Lerp(oldColor, new Color(color.r, color.g, color.b, color.a * InnerPointerOpacityScalar), lerpalpha);
        }
      }
    }

    private bool SendUpdateEventToSelectedObject() {
      if (base.eventSystem.currentSelectedGameObject == null)
        return false;

      BaseEventData data = GetBaseEventData();
      ExecuteEvents.Execute(base.eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);
      return data.used;
    }

    void OnDrawGizmos() {
      if (DrawDebug) {
        while (DebugSphereQueue != null && DebugSphereQueue.Count > 0) {
          Gizmos.DrawSphere(DebugSphereQueue.Dequeue(), 0.1f);
        }
      }
    }

    /** Only activate the InputModule when there are hands in the scene. */
    public override bool ShouldActivateModule() {
      return LeapDataProvider.CurrentFrame != null && LeapDataProvider.CurrentFrame.Hands.Count > 0 && base.ShouldActivateModule();
    }
  }
}
