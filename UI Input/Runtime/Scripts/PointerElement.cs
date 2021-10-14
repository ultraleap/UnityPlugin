using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Leap.Unity.InputModule
{
        [Serializable]
        public class PointerElement
        {
            private readonly Camera _mainCamera;
            private readonly EventSystem _eventSystem;
            private readonly LeapProvider _leapDataProvider;
            private readonly IInputModuleSettings _settings;
            private readonly IInputModuleEventHandler _inputModuleEventHandler;
            private readonly PinchDetector _leftHandDetector;
            private readonly PinchDetector _rightHandDetector;
            
            public Chirality Chirality { get; private set; }

            public GameObject Pointer { get;  set; }
            private GameObject InnerPointer { get; set; }
            private SpriteRenderer SpriteRenderer { get; set; }
            private SpriteRenderer InnerSpriteRenderer { get; set; }

            private PointerEventData EventData { get; set; }
            public PointerStates PointerState { get; set; }

            private PointerStates PrevState { get; set; }
            private Vector2 PrevScreenPosition { get; set; }
            private Vector2 DragStartPosition { get; set; }

            private GameObject PreviousGameObjectUnderPointer { get; set; }
            private GameObject CurrentGameObject { get; set; }
            private GameObject GameObjectBeingDragged { get; set; }
            private GameObject CurrentGameObjectUnderPointer { get; set; }

            private bool PrevTriggeringInteraction { get; set; }
            private float TimeEnteredCanvas { get; set; }

            private List<RaycastResult> _raycastResultCache = new List<RaycastResult>();

            private static readonly Dictionary<(PointerStates from, PointerStates to), Action<IInputModuleEventHandler, PointerElement>> StateChangeActionMap = new Dictionary<(PointerStates prev, PointerStates pointer), Action<IInputModuleEventHandler, PointerElement>>()
            {
                {(PointerStates.OnCanvas, PointerStates.OnElement), (module, pointerElement) => module.OnBeginHover.Invoke(module, pointerElement.Pointer.transform.position) },
                {(PointerStates.OnCanvas, PointerStates.PinchingToCanvas), (module, pointerElement) => module.OnBeginMissed.Invoke(module, pointerElement.Pointer.transform.position) },
                {(PointerStates.PinchingToCanvas, PointerStates.OnCanvas), (module, pointerElement) => module.OnEndMissed.Invoke(module, pointerElement.Pointer.transform.position) },
                {(PointerStates.OnElement, PointerStates.OnCanvas), (module, pointerElement) => module.OnEndHover.Invoke(module, pointerElement.Pointer.transform.position) },
                {(PointerStates.OnElement, PointerStates.PinchingToElement), (module, pointerElement) => module.OnClickDown.Invoke(module, pointerElement.Pointer.transform.position) },
                {(PointerStates.PinchingToElement, PointerStates.OnElement), (module, pointerElement) => module.OnClickUp.Invoke(module, pointerElement.Pointer.transform.position) },
                {(PointerStates.PinchingToElement, PointerStates.OnCanvas), (module, pointerElement) => module.OnClickUp.Invoke(module, pointerElement.Pointer.transform.position) },
                {(PointerStates.NearCanvas, PointerStates.TouchingElement), (module, pointerElement) => module.OnClickDown.Invoke(module, pointerElement.Pointer.transform.position) },
                {(PointerStates.NearCanvas, PointerStates.TouchingCanvas), (module, pointerElement) => module.OnBeginMissed.Invoke(module, pointerElement.Pointer.transform.position) },
                {(PointerStates.TouchingCanvas, PointerStates.NearCanvas), (module, pointerElement) => module.OnEndMissed.Invoke(module, pointerElement.Pointer.transform.position) },
                {(PointerStates.TouchingElement, PointerStates.NearCanvas), (module, pointerElement) => module.OnClickUp.Invoke(module, pointerElement.Pointer.transform.position) },
                {(PointerStates.OffCanvas, PointerStates.OffCanvas), (module, pointerElement) => pointerElement.TimeEnteredCanvas = Time.time },
            };

            public PointerElement(Camera mainCamera, EventSystem eventSystem, LeapProvider leapDataProvider, IInputModuleSettings settings, IInputModuleEventHandler inputModuleEventHandler, PinchDetector leftHandDetector, PinchDetector rightHandDetector)
            {
                _mainCamera = mainCamera;
                _eventSystem = eventSystem;
                _leapDataProvider = leapDataProvider;
                _inputModuleEventHandler = inputModuleEventHandler;
                _settings = settings;
                _leftHandDetector = leftHandDetector;
                _rightHandDetector = rightHandDetector;
            }

            internal void Initialise(Transform parent, Sprite pointerSprite, Material pointerMaterial, bool innerPointer)
            {
                // Create the Canvas to render the Pointer on
                Pointer = new GameObject($"Pointer {Chirality}");
                SpriteRenderer = Pointer.AddComponent<SpriteRenderer>();
                SpriteRenderer.sortingOrder = 1000;

                // Add your sprite to the Sprite Renderer
                // Make sure to instantiate the material so each pointer can be modified independently
                SpriteRenderer.sprite = pointerSprite;
                SpriteRenderer.material = Object.Instantiate(pointerMaterial);

                Pointer.transform.parent = parent;
                Pointer.SetActive(false);

                if (innerPointer)
                {
                    //Create the Canvas to render the Pointer on
                    InnerPointer = new GameObject($"Inner Pointer {Chirality}");
                    InnerSpriteRenderer = InnerPointer.AddComponent<SpriteRenderer>();
                    InnerSpriteRenderer.sortingOrder = 1000;

                    //Add your sprite to the Canvas
                    InnerSpriteRenderer.sprite = pointerSprite;

                    InnerSpriteRenderer.material = Object.Instantiate(pointerMaterial);

                    InnerPointer.transform.parent = parent;
                    InnerPointer.SetActive(false);
                }
            }

            /// <summary>
            /// The z position of the index finger tip to the Pointer
            /// </summary>
            private float DistanceOfTipToPointer(Hand hand)
            {
                var tipPosition = hand.Fingers[(int)Finger.FingerType.TYPE_INDEX]
                    .Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();

                return -Pointer.transform.InverseTransformPoint(tipPosition).z * Pointer.transform.lossyScale.z - _settings.TactilePadding;
            }

            // /// <summary>
            // /// The z position of the index finger tip to the specified transform.
            // /// </summary>
            // /// <param name="uiElement"></param>
            // /// <param name="whichHand"></param>
            // /// <param name="whichFinger"></param>
            // /// <returns></returns>
            // private float DistanceOfTipToElement(Transform uiElement, int whichHand, int whichFinger)
            // {
            //     //Get Base of Index Finger Position
            //     var tipPosition = _leapDataProvider.CurrentFrame.Hands[whichHand].Fingers[whichFinger].Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();
            //     return -uiElement.InverseTransformPoint(tipPosition).z * uiElement.lossyScale.z - _settings.TactilePadding;
            // }

            /// <summary>
            /// Returns true if the specified pointer is in the "touching" interaction mode, i.e, whether it is touching or nearly touching a canvas or control.
            /// </summary>
            private bool IsTouchingOrNearlyTouchingCanvasOrElement =>
                PointerState == PointerStates.NearCanvas ||
                PointerState == PointerStates.TouchingCanvas ||
                PointerState == PointerStates.TouchingElement;

            /// <summary>
            /// Returns true if the pointer was interacting previously, but no longer is
            /// </summary>
            private bool NoLongerInteracting(Hand hand) =>
                PrevTriggeringInteraction && (!IsTriggeringInteraction(hand) || PointerState == PointerStates.OffCanvas);

            /// <summary>
            /// Returns true if a "click" is being triggered during the current frame.
            /// </summary>
            private bool IsTriggeringInteraction(Hand hand)
            {
                if (_settings.InteractionMode != InteractionCapability.Projective)
                {
                    if (IsTouchingOrNearlyTouchingCanvasOrElement)
                    {
                        return DistanceOfTipToPointer(hand) < 0f;
                    }
                }

                if (_settings.InteractionMode != InteractionCapability.Tactile)
                {
                    if (_rightHandDetector != null && hand.IsRight && _rightHandDetector.IsPinching
                        || _rightHandDetector == null && hand.PinchDistance < _settings.PinchingThreshold)
                    {                   
                        return true;
                    }
                    else if (_leftHandDetector != null && hand.IsLeft && _leftHandDetector.IsPinching
                             || _leftHandDetector == null && hand.PinchDistance < _settings.PinchingThreshold)
                    {
                        return true;
                    }
                }

                //Disabling Pinching during touch interactions; maybe still desirable?
                //return LeapDataProvider.CurrentFrame.Hands[whichPointer].PinchDistance < PinchingThreshold;

                return false;
            }

            /// <summary>
            /// Is the current mode limited to tactile interaction
            /// </summary>
            private bool OnlyTactileInteractionEnabled
                => _settings?.InteractionMode == InteractionCapability.Tactile;

            /// <summary>
            /// Is the current mode limited to projective interaction (far field)
            /// </summary>
            private bool OnlyProjectionInteractionEnabled
                => _settings?.InteractionMode == InteractionCapability.Projective;

            /// <summary>
            /// Is tactile interaction allowed and is the pointer tip distance within the tactile interaction distance
            /// </summary>
            private bool IsPermittedTactileInteraction(Hand hand)
                => OnlyTactileInteractionEnabled || !OnlyProjectionInteractionEnabled && DistanceOfTipToPointer(hand) < _settings.ProjectiveToTactileTransitionDistance;

            internal void Process(Hand hand, IProjectionOriginProvider projectionOriginProvider)
            {
                if (hand == null)
                {
                    if (Pointer.activeInHierarchy)
                    {
                        Pointer.SetActive(false);

                        if (InnerPointer)
                        {
                            InnerPointer.SetActive(false);
                        }
                    }
                    return;
                }

                // Raycast from shoulder through tip of the index finger to the UI
                bool tipRaycastUsed;
                if (SupportingTactileInteraction())
                {
                    tipRaycastUsed = GetLookPointerEventData(
                        hand,
                        projectionOriginProvider.ProjectionOriginForHand(hand.IsLeft),
                        true);

                    PrevState = PointerState; //Store old state for sound transition purposes
                    UpdatePointer(hand, EventData, EventData.pointerCurrentRaycast.gameObject);
                    ProcessState(hand, tipRaycastUsed);
                }

                // If didn't hit anything near the fingertip, try doing it again, but through the knuckle this time
                if ((PointerState == PointerStates.OffCanvas && _settings.InteractionMode != InteractionCapability.Tactile) ||
                    _settings.InteractionMode == InteractionCapability.Projective)
                {
                    tipRaycastUsed = GetLookPointerEventData(                    
                        hand,
                        projectionOriginProvider.ProjectionOriginForHand(hand.IsLeft),
                        false);

                    if (_settings.InteractionMode == InteractionCapability.Projective)
                    {
                        PrevState = PointerState; //Store old state for sound transition purposes
                    }

                    UpdatePointer(hand, EventData, EventData.pointerCurrentRaycast.gameObject);

                    if (!tipRaycastUsed && IsPermittedTactileInteraction(hand))
                    {
                        EventData.pointerCurrentRaycast = new RaycastResult();
                    }

                    ProcessState(hand, tipRaycastUsed);
                }

                // Handle the Environment Pointer
                if (_settings.RenderEnvironmentPointer && PointerState == PointerStates.OffCanvas)
                {
                    Vector3 indexMetacarpal = hand.Fingers[(int)Finger.FingerType.TYPE_INDEX].Bone(Bone.BoneType.TYPE_METACARPAL).Center.ToVector3();

                    Physics.Raycast(projectionOriginProvider.ProjectionOriginForHand(hand.IsLeft),
                        (indexMetacarpal - projectionOriginProvider.ProjectionOriginForHand(hand.IsLeft)).normalized,                                
                        out var environmentSpot);

                    Pointer.transform.position = environmentSpot.point + environmentSpot.normal * 0.01f;
                    Pointer.transform.rotation = Quaternion.LookRotation(environmentSpot.normal);

                    if (InnerPointer)
                    {
                        InnerPointer.transform.position = environmentSpot.point + environmentSpot.normal * 0.01f;
                        InnerPointer.transform.rotation = Quaternion.LookRotation(environmentSpot.normal);
                    }

                    EvaluatePointerSize(hand);

                    if (IsTriggeringInteraction(hand))
                    {
                        _inputModuleEventHandler?.OnEnvironmentPinch?.Invoke(_inputModuleEventHandler, Pointer.transform.position);
                    }
                }

                PrevScreenPosition = EventData.position;

                // Trigger events that come from changing pointer state
                RaiseEventsForStateChanges();

                if (EventData != null)
                {
                    //If we hit something with our Raycast, let's see if we should interact with it
                    if (EventData.pointerCurrentRaycast.gameObject != null &&
                        PointerState != PointerStates.OffCanvas)
                    {
                        PreviousGameObjectUnderPointer = CurrentGameObjectUnderPointer;
                        CurrentGameObjectUnderPointer = EventData.pointerCurrentRaycast.gameObject;

                        //Trigger Enter or Exit Events on the UI Element (like highlighting)
                        _inputModuleEventHandler?.HandlePointerExitAndEnterProxy(EventData, CurrentGameObjectUnderPointer);

                        if (!PrevTriggeringInteraction && IsTriggeringInteraction(hand))
                        {
                            PrevTriggeringInteraction = true;

                            if (Time.time - TimeEnteredCanvas >= Time.deltaTime)
                            {
                                //Deselect all objects
                                if (_eventSystem.currentSelectedGameObject)
                                {
                                    _eventSystem.SetSelectedGameObject(null);
                                }

                                //Record pointer telemetry
                                EventData.pressPosition = EventData.position;
                                EventData.pointerPressRaycast = EventData.pointerCurrentRaycast;
                                EventData.pointerPress = null; //Clear this for setting later
                                EventData.useDragThreshold = true;

                                //If we hit something good, let's trigger it!
                                if (CurrentGameObjectUnderPointer != null)
                                {
                                    CurrentGameObject = CurrentGameObjectUnderPointer;

                                    //See if this object, or one of its parents, has a pointerDownHandler
                                    var gameObjectJustPressed = ExecuteEvents.ExecuteHierarchy(CurrentGameObject, EventData, ExecuteEvents.pointerDownHandler);

                                    //If not, see if one has a pointerClickHandler!
                                    if (gameObjectJustPressed == null)
                                    {
                                        var gameObjectJustClicked = ExecuteEvents.ExecuteHierarchy(CurrentGameObject, EventData, ExecuteEvents.pointerClickHandler);

                                        if (gameObjectJustClicked != null)
                                        {
                                            CurrentGameObject = gameObjectJustClicked;
                                        }
                                    }
                                    else
                                    {
                                        CurrentGameObject = gameObjectJustPressed;

                                        //We want to do "click on button down" at same time, unlike regular mouse processing
                                        //Which does click when mouse goes up over same object it went down on
                                        //This improves the user's ability to select small menu items
                                        //ExecuteEvents.Execute(newPressed, PointEvents[whichPointer], ExecuteEvents.pointerClickHandler);
                                    }

                                    if (gameObjectJustPressed != null)
                                    {
                                        EventData.pointerPress = gameObjectJustPressed;
                                        CurrentGameObject = gameObjectJustPressed;

                                        //Select the currently pressed object
                                        if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(CurrentGameObject))
                                        {
                                            _eventSystem.SetSelectedGameObject(CurrentGameObject);
                                        }
                                    }

                                    //Debug.Log(currentGo[whichPointer].name);

                                    // Find something in the hierarchy that implements dragging, starting at this GO and searching up
                                    EventData.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(CurrentGameObject);

                                    //Debug.Log(PointEvents[whichPointer].pointerDrag.name);

                                    if (EventData.pointerDrag)
                                    {
                                        var dragHandler = EventData.pointerDrag.GetComponent<IDragHandler>();

                                        if (dragHandler != null)
                                        {
                                            if (dragHandler is EventTrigger && EventData.pointerDrag.transform.parent)
                                            {
                                                //Hack: EventSystems intercepting Drag Events causing funkiness
                                                EventData.pointerDrag =
                                                    ExecuteEvents.GetEventHandler<IDragHandler>(EventData.pointerDrag.transform.parent.gameObject);

                                                if (EventData.pointerDrag != null)
                                                {
                                                    dragHandler = EventData.pointerDrag.GetComponent<IDragHandler>();

                                                    if (dragHandler != null && !(dragHandler is EventTrigger))
                                                    {
                                                        GameObjectBeingDragged = EventData.pointerDrag;
                                                        DragStartPosition = EventData.position;

                                                        if (CurrentGameObject && CurrentGameObject == GameObjectBeingDragged)
                                                        {
                                                            ExecuteEvents.Execute(
                                                                EventData.pointerDrag,
                                                                EventData,
                                                                ExecuteEvents.beginDragHandler);

                                                            EventData.dragging = true;
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                GameObjectBeingDragged = EventData.pointerDrag; // Property OnDragTarget for .EventData.pointerDrag
                                                DragStartPosition = EventData.position;

                                                if (CurrentGameObject && CurrentGameObject == GameObjectBeingDragged)
                                                {
                                                    ExecuteEvents.Execute(EventData.pointerDrag, EventData, ExecuteEvents.beginDragHandler);
                                                    EventData.dragging = true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }


                    // Check if the pointer has moved beyond the pixel threshold which triggers dragging
                    // Refactor as method HandleScrolling?
                    if (!EventData.dragging && GameObjectBeingDragged &&
                        Vector2.Distance(EventData.position, DragStartPosition) * 100f >
                        EventSystem.current.pixelDragThreshold)
                    {
                        var dragHandler = EventData.pointerDrag.GetComponent<IDragHandler>();
                        if (dragHandler != null && dragHandler is ScrollRect)
                        {
                            // Are we dragging on an element inside a scroll rect?
                            if (CurrentGameObject && !CurrentGameObject.GetComponent<ScrollRect>())
                            {
                                ExecuteEvents.Execute(EventData.pointerDrag,
                                    EventData, ExecuteEvents.beginDragHandler);
                                EventData.dragging = true;

                                // https://answers.unity.com/questions/1082179/mouse-drag-element-inside-scrollrect-throws-pointe.html
                                // An OnPointerUp event is needed to unlock scrolling...
                                ExecuteEvents.Execute(CurrentGameObject, EventData,
                                    ExecuteEvents.pointerUpHandler);
                                EventData.rawPointerPress = null;
                                EventData.pointerPress = null;
                                CurrentGameObject = null;
                            }
                        }
                    }


                    //If we WERE interacting last frame, but are not this frame...
                    if (NoLongerInteracting(hand))
                    {
                        PrevTriggeringInteraction = false;

                        if (GameObjectBeingDragged)
                        {
                            ExecuteEvents.Execute(GameObjectBeingDragged, EventData, ExecuteEvents.endDragHandler);

                            if (CurrentGameObject && GameObjectBeingDragged == CurrentGameObject)
                            {
                                ExecuteEvents.Execute(GameObjectBeingDragged, EventData, ExecuteEvents.pointerUpHandler);
                            }

                            //Debug.Log(currentGoing[whichPointer].name);
                            if (CurrentGameObjectUnderPointer != null)
                            {
                                ExecuteEvents.ExecuteHierarchy(CurrentGameObjectUnderPointer, EventData, ExecuteEvents.dropHandler);
                            }

                            EventData.pointerDrag = null;
                            EventData.dragging = false;
                            GameObjectBeingDragged = null;
                        }

                        if (CurrentGameObject)
                        {
                            ExecuteEvents.Execute(CurrentGameObject, EventData, ExecuteEvents.pointerUpHandler);
                            ExecuteEvents.Execute(CurrentGameObject, EventData, ExecuteEvents.pointerClickHandler);
                            EventData.rawPointerPress = null;
                            EventData.pointerPress = null;
                            CurrentGameObject = null;
                            GameObjectBeingDragged = null;
                        }
                    }

                    //And for everything else, there is dragging.
                    if (EventData.pointerDrag != null && EventData.dragging)
                    {
                        ExecuteEvents.Execute(EventData.pointerDrag, EventData,ExecuteEvents.dragHandler);
                    }
                }

                UpdatePointerColor(hand);
            }

            /// <summary>
            /// Is tactile interaction allowed
            /// </summary>
            /// <returns>True if operating an InteractionCapability mode where tactile interaction is supported</returns>
            private bool SupportingTactileInteraction()
            {
                return _settings.InteractionMode != InteractionCapability.Projective;
            }

            /// <summary>
            /// Raycast from the EventCamera into UI Space
            /// </summary>
            /// <param name="origin"></param>
            /// <param name="forceTipRaycast"></param>
            /// <returns></returns>
            private bool GetLookPointerEventData(Hand hand, Vector3 origin, bool forceTipRaycast)
            {
                // Whether or not this will be a raycast through the finger tip
                var tipRaycast = false;

                //Initialize a blank PointerEvent
                if (EventData == null)
                {
                    EventData = new PointerEventData(_eventSystem);
                }
                else
                {
                    EventData.Reset();
                }

                //We're always going to assume we're "Left Clicking", for the benefit of uGUI
                EventData.button = PointerEventData.InputButton.Left;

                //If we're in "Touching Mode", Raycast through the fingers
                Vector3 indexFingerPosition; // It's a lie, this is not the index finger - but the selected pointing finger....
                if (IsTouchingOrNearlyTouchingCanvasOrElement || forceTipRaycast) // WTF is touching mode all about ??? Why is it related to tip raycast??
                {
                    tipRaycast = true;

                    //Focus pointer through the average of the extended fingers - comment is out of date, not taking an average any more???
                    float farthest = 0f;
                    indexFingerPosition = hand.Fingers[1].TipPosition
                        .ToVector3();
                    for (int i = 1; i < 3; i++)
                    {
                        float fingerDistance = Vector3.Distance(Camera.main.transform.position,
                            hand.Fingers[i].TipPosition.ToVector3());
                        float fingerExtension =
                            Mathf.Clamp01(Vector3.Dot(
                                hand.Fingers[i].Direction.ToVector3(),
                                _leapDataProvider.CurrentFrame.Hands[0].Direction.ToVector3())) / 1.5f;

                        if (fingerDistance > farthest && fingerExtension > 0.5f)
                        {
                            farthest = fingerDistance;
                            indexFingerPosition = hand.Fingers[i].TipPosition
                                .ToVector3(); // Hummm, not really the index finger position 0 but the selected furthest finger that is considered extended
                        }
                    }
                }
                else
                {
                    //Raycast through the knuckle of the finger
                    indexFingerPosition = hand.Fingers[(int)Finger.FingerType.TYPE_INDEX].Bone(Bone.BoneType.TYPE_METACARPAL).Center.ToVector3();
                }

                //Set the Raycast Direction and Delta
                EventData.position = Vector2.Lerp(PrevScreenPosition,
                    _mainCamera.WorldToScreenPoint(indexFingerPosition),
                    1.0f);

                EventData.delta = EventData.position - PrevScreenPosition;
                EventData.scrollDelta = Vector2.zero;

                //Perform the Raycast and sort all the things we hit by distance... (where distance is the canvas order, not the Z-depth???)
                _eventSystem.RaycastAll(EventData, _raycastResultCache);

                //Optional hack that subverts ScrollRect hierarchies; to avoid this, disable "RaycastTarget" on the Viewport and Content panes
                if (_settings.OverrideScrollViewClicks)
                {
                    EventData.pointerCurrentRaycast = new RaycastResult(); // Suboptimal creation of newed object, not needed each time
                    foreach (var t in _raycastResultCache) // Why do we loop over everything in the cache? Don't we want to find the first result of the valid type?
                    {
                        if (t.gameObject.GetComponent<Scrollbar>() != null)
                        {
                            EventData.pointerCurrentRaycast = t;
                        }
                        else if (EventData.pointerCurrentRaycast.gameObject == null &&
                                 t.gameObject.GetComponent<ScrollRect>() != null)
                        {
                            EventData.pointerCurrentRaycast = t;
                        }
                    }

                    if (EventData.pointerCurrentRaycast.gameObject == null)
                    {
                        EventData.pointerCurrentRaycast = _inputModuleEventHandler.FindFirstRaycastProxy(_raycastResultCache);
                    }
                }
                else
                {
                    EventData.pointerCurrentRaycast = _inputModuleEventHandler.FindFirstRaycastProxy(_raycastResultCache);
                }

                //Clear the list of things we hit; we don't need it anymore.
                _raycastResultCache.Clear();
                return tipRaycast;
            }



            //Tree to decide the State of the Pointer. Once again, why are we passing the pointer, Could be a method on the pointer object.
            private void ProcessState(Hand hand, bool tipRaycastUsed)
            {
                if (EventData.pointerCurrentRaycast.gameObject != null)
                {
                    // Why are we forcing tactile or projective if there is an enum to set up the mode, when are these states forced????
                    // Should be able to re-express this if statement to make it clearer....
                    if (IsPermittedTactileInteraction(hand))
                    {
                        if (IsTriggeringInteraction(hand))
                        {
                            if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(EventData
                                .pointerCurrentRaycast.gameObject))
                            {
                                PointerState = PointerStates.TouchingElement;
                            }
                            else
                            {
                                PointerState = PointerStates.TouchingCanvas; // Really this is touching non interactive UI element which could be the canvas
                            }
                        }
                        else
                        {
                            PointerState = PointerStates.NearCanvas; // Are we really near it though. We are over something but not close enough for it to be a tactile interaction????
                        }
                    }
                    else if (!tipRaycastUsed) // Provide more context here ... what is tipRayCast all about and why is it relevant to this logic?
                    {
                        if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(EventData
                            .pointerCurrentRaycast.gameObject))  // if HitElementCanBeClicked
                        {
                            // || PointEvents[whichPointer].dragging) {
                            if (IsTriggeringInteraction(hand))
                            {
                                // Erm why is this considered pinching rather than a click?
                                PointerState = PointerStates.PinchingToElement;
                            }
                            else
                            {
                                PointerState = PointerStates.OnElement;
                            }
                        }
                        else // Hit a non clickable UI element ...
                        {
                            if (IsTriggeringInteraction(hand))
                            {
                                PointerState = PointerStates.PinchingToCanvas;
                            }
                            else
                            {
                                PointerState = PointerStates.OnCanvas;
                            }
                        }
                    }
                    else
                    {
                        PointerState = PointerStates.OffCanvas;
                    }
                }
                else
                {
                    PointerState = PointerStates.OffCanvas;
                }
            }

            //Update the pointer location and whether or not it is enabled
            private void UpdatePointer(Hand hand, PointerEventData pointData, GameObject uiComponent)
            {
                var element = EventData.pointerCurrentRaycast.gameObject;
                if (element != null)
                {
                    Pointer.SetActive(true);

                    if (InnerPointer)
                    {
                        InnerPointer.SetActive(true);
                    }
                    
                    var draggingPlane = EventData.pointerCurrentRaycast.gameObject.GetComponent<RectTransform>();

                    if (RectTransformUtility.ScreenPointToWorldPointInRectangle(draggingPlane, pointData.position,
                        pointData.enterEventCamera, out var globalLookPos))
                    {
                        var hoverer = ExecuteEvents.GetEventHandler<IPointerEnterHandler>(uiComponent);
                        if (hoverer)
                        {
                            var componentInPlane = hoverer.transform.InverseTransformPoint(globalLookPos);
                            componentInPlane = new Vector3(componentInPlane.x, componentInPlane.y, 0f);
                            Pointer.transform.position = hoverer.transform.TransformPoint(componentInPlane);
                        }
                        else
                        {
                            Pointer.transform.position = globalLookPos;
                        }

                        var pointerAngle = Mathf.Rad2Deg * Mathf.Atan2(pointData.delta.x, pointData.delta.y);
                        Pointer.transform.rotation = draggingPlane.rotation * Quaternion.Euler(0f, 0f, -pointerAngle);

                        if (InnerPointer)
                        {
                            InnerPointer.transform.position = globalLookPos; // -transform.forward * 0.01f; //Amount the pointer floats above the Canvas
                            InnerPointer.transform.rotation = draggingPlane.rotation * Quaternion.Euler(0f, 0f, -pointerAngle);
                        }

                        EvaluatePointerSize(hand);
                    }
                }
            }

            private void EvaluatePointerSize(Hand hand)
            {
                //Use the Scale AnimCurve to Evaluate the Size of the Pointer
                float pointDistance = 1f;
                if (Camera.main != null)
                {
                    pointDistance = (Pointer.transform.position - Camera.main.transform.position).magnitude;
                }

                var pointerScale = _settings.PointerDistanceScale.Evaluate(pointDistance);
                if (!IsTouchingOrNearlyTouchingCanvasOrElement)
                {
                    pointerScale *= _settings.PointerPinchScale.Evaluate(hand.PinchDistance);
                }

                //Commented out Velocity Stretching because it looks funny when switching between Tactile and Projective
                Pointer.transform.localScale = pointerScale * new Vector3(1f, 1f /*+ pointData.delta.magnitude*1f*/, 1f);
            }
            
            private void RaiseEventsForStateChanges()
            {
                // Extract the Hover stuff as a separate method from the state change events?
                if (_settings.TriggerHoverOnElementSwitch)
                {
                    if (PrevState != PointerStates.OffCanvas && PointerState != PointerStates.OffCanvas)
                    {
                        if (CurrentGameObjectUnderPointer != PreviousGameObjectUnderPointer)
                        {
                            //When you begin to hover on an element
                            _inputModuleEventHandler?.OnBeginHover?.Invoke(_inputModuleEventHandler, Pointer.transform.position);
                        }
                    }
                }

                StateChangeActionMap.TryGetValue((PrevState, PointerState), out var result);
                result?.Invoke(_inputModuleEventHandler, this);
            }

            /// <summary>
            /// Updates the pointer by lerping to the relevant colour for the current state
            /// </summary>
            private void UpdatePointerColor(Hand hand)
            {
                var transitionAmount = Mathf.Clamp01(Mathf.Abs(DistanceOfTipToPointer(hand) - _settings.ProjectiveToTactileTransitionDistance) / 0.05f);

                //TODO Can we reduce the number of instantiations we do here w.r.t. creating colours?
                switch (PointerState)
                {
                    case PointerStates.OnCanvas:
                        LerpPointerColor(new Color(0f, 0f, 0f, 1f * transitionAmount), 0.2f);
                        LerpPointerColor(_settings.StandardColor, 0.2f);
                        break;
                    case PointerStates.OnElement:
                        LerpPointerColor(new Color(0f, 0f, 0f, 1f * transitionAmount), 0.2f);
                        LerpPointerColor(_settings.HoveringColor, 0.2f);
                        break;
                    case PointerStates.PinchingToCanvas:
                        LerpPointerColor(new Color(0f, 0f, 0f, 1f * transitionAmount), 0.2f);
                        LerpPointerColor(_settings.TriggerMissedColor, 0.2f);
                        break;
                    case PointerStates.PinchingToElement:
                        LerpPointerColor(new Color(0f, 0f, 0f, 1f * transitionAmount), 0.2f);
                        LerpPointerColor(_settings.TriggeringColor, 0.2f);
                        break;
                    case PointerStates.NearCanvas:
                        LerpPointerColor(new Color(0.0f, 0.0f, 0.0f, 0.5f * transitionAmount), 0.3f);
                        LerpPointerColor(_settings.StandardColor, 0.2f);
                        break;
                    case PointerStates.TouchingElement:
                        LerpPointerColor(new Color(0.0f, 0.0f, 0.0f, 0.7f * transitionAmount), 0.2f);
                        LerpPointerColor(_settings.TriggeringColor, 0.2f);
                        break;
                    case PointerStates.TouchingCanvas:
                        LerpPointerColor(new Color(0.0f, 0.01f, 0.0f, 0.5f * transitionAmount), 0.2f);
                        LerpPointerColor(_settings.TriggerMissedColor, 0.2f);
                        break;
                    case PointerStates.OffCanvas:

                        LerpPointerColor(_settings.TriggerMissedColor, 0.2f);

                        if (_settings.RenderEnvironmentPointer)
                        {
                            LerpPointerColor(new Color(0.0f, 0.0f, 0.0f, 0.5f * transitionAmount), 1f);
                        }
                        else
                        {
                            LerpPointerColor(new Color(0.0f, 0.0f, 0.0f, 0.001f), 1f);
                        }

                        break;
                }
            }

            /// <summary>
            /// Handles the lerp operation for the pointer colour
            /// If RGB are 0f or Alpha is 1f, then it will ignore those components and only lerp the remaining components
            /// Linearly interpolates the color of a cursor toward the specified color.
            /// </summary>
            /// <param name="color">The target color</param>
            /// <param name="lerpAlpha">The amount to interpolate by</param>
            private void LerpPointerColor(Color color, float lerpAlpha)
            {
                // TODO can we avoid the new operation for the colours?
                var pointerSprite = Pointer.GetComponent<SpriteRenderer>();
                var oldColor = pointerSprite.color;
                if (color.r == 0f && color.g == 0f && color.b == 0f)
                {
                    pointerSprite.material.color = Color.Lerp(oldColor, new Color(oldColor.r, oldColor.g, oldColor.b, color.a), lerpAlpha);
                    pointerSprite.color = Color.Lerp(oldColor, new Color(oldColor.r, oldColor.g, oldColor.b, color.a), lerpAlpha);
                }
                else if (color.a == 1f)
                {
                    pointerSprite.material.color = Color.Lerp(oldColor, new Color(color.r, color.g, color.b, oldColor.a), lerpAlpha);
                    pointerSprite.color = Color.Lerp(oldColor, new Color(color.r, color.g, color.b, oldColor.a), lerpAlpha);
                }
                else
                {
                    pointerSprite.material.color = Color.Lerp(oldColor, color, lerpAlpha);
                    pointerSprite.color = Color.Lerp(oldColor, color, lerpAlpha);
                }

                if (InnerPointer)
                {
                    var innerPointerSprite = InnerPointer.GetComponent<SpriteRenderer>();
                    oldColor = innerPointerSprite.color;
                    if (color.r == 0f && color.g == 0f && color.b == 0f)
                    {
                        innerPointerSprite.material.color = Color.Lerp(oldColor, new Color(oldColor.r, oldColor.g, oldColor.b, color.a * _settings.InnerPointerOpacityScalar), lerpAlpha);
                        innerPointerSprite.color = Color.Lerp(oldColor, new Color(oldColor.r, oldColor.g, oldColor.b, color.a * _settings.InnerPointerOpacityScalar), lerpAlpha);
                    }
                    else if (color.a == 1f)
                    {
                        innerPointerSprite.material.color = Color.Lerp(oldColor, new Color(color.r, color.g, color.b, oldColor.a * _settings.InnerPointerOpacityScalar), lerpAlpha);
                        innerPointerSprite.color = Color.Lerp(oldColor, new Color(color.r, color.g, color.b, oldColor.a * _settings.InnerPointerOpacityScalar), lerpAlpha);
                    }
                    else
                    {
                        innerPointerSprite.material.color = Color.Lerp(oldColor, new Color(color.r, color.g, color.b, color.a * _settings.InnerPointerOpacityScalar), lerpAlpha);
                        innerPointerSprite.color = Color.Lerp(oldColor, new Color(color.r, color.g, color.b, color.a * _settings.InnerPointerOpacityScalar), lerpAlpha);
                    }
                }
            }
        }

}
