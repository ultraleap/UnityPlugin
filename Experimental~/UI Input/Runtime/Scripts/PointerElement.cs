/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Leap.Unity.InputModule
{
    /// <summary>
    /// Representation of a pointer that can be controlled by the LeapInputModule
    /// </summary>
    [Serializable]
    public class PointerElement
    {
        #region Properties
        
        private readonly Camera _mainCamera;
        private readonly EventSystem _eventSystem;
        private readonly LeapProvider _leapDataProvider;
        private readonly IInputModuleSettings _settings;
        private readonly IInputModuleEventHandler _inputModuleEventHandler;
        private readonly PinchDetector _pinchDetector;
        
        public Chirality Chirality { get; private set; }

        public GameObject Pointer { get;  set; }
        private GameObject InnerPointer { get; set; }
        private SpriteRenderer SpriteRenderer { get; set; }
        private SpriteRenderer InnerSpriteRenderer { get; set; }

        private PointerEventData EventData { get; set; }
        public PointerStates PointerState { get; private set; }

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

        private static readonly Dictionary<(PointerStates from, PointerStates to), Action<IInputModuleEventHandler, PointerElement>> StateChangeActionMap 
            = new Dictionary<(PointerStates prev, PointerStates pointer), Action<IInputModuleEventHandler, PointerElement>>()
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
        
        #endregion
        
        #region Initialisation

        public PointerElement(Chirality chirality, Camera mainCamera, EventSystem eventSystem, LeapProvider leapDataProvider, IInputModuleSettings settings, IInputModuleEventHandler inputModuleEventHandler, PinchDetector pinchDetector)
        {
            Chirality = chirality;
            _mainCamera = mainCamera;
            _eventSystem = eventSystem;
            _leapDataProvider = leapDataProvider;
            _inputModuleEventHandler = inputModuleEventHandler;
            _settings = settings;
            _pinchDetector = pinchDetector;
            
            EventData = new PointerEventData(_eventSystem);
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
            if (pointerMaterial)
            {
                SpriteRenderer.material = Object.Instantiate(pointerMaterial);
            }
            else
            {
                Debug.LogWarning("Pointer material must be set for pointers to be visible", pointerMaterial);
            }

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
                if (pointerMaterial)
                {
                    InnerSpriteRenderer.material = Object.Instantiate(pointerMaterial);
                }
                else
                {
                    Debug.LogWarning("Pointer material must be set for inner pointers to be visible", pointerMaterial);
                }

                InnerPointer.transform.parent = Pointer.transform;
                InnerPointer.SetActive(false);
            }
        }
        
        #endregion

        /// <summary>
        /// The z position of the index finger tip to the Pointer
        /// </summary>
        private float DistanceOfTipToPointer(Hand hand)
        {
            var tipPosition = hand.Fingers[(int)Finger.FingerType.TYPE_INDEX]
                .Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();

            return -Pointer.transform.InverseTransformPoint(tipPosition).z * Pointer.transform.lossyScale.z - _settings.TactilePadding;
        }

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
            if (_settings.InteractionMode != InteractionCapability.Indirect)
            {
                if (IsTouchingOrNearlyTouchingCanvasOrElement)
                {
                    return DistanceOfTipToPointer(hand) < 0f;
                }
            }

            if (_settings.InteractionMode != InteractionCapability.Direct)
            {
                if (_pinchDetector != null && HasMatchingChirality(hand) && _pinchDetector.IsPinching)
                {
                    return true;
                }

                if (_pinchDetector == null && hand.PinchDistance < _settings.PinchingThreshold)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        bool HasMatchingChirality(Hand hand)
        {
            switch (Chirality)
            {
                case Chirality.Left when hand.IsLeft:
                    return true;
                case Chirality.Right when hand.IsRight:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Is the current mode limited to tactile interaction
        /// </summary>
        private bool OnlyTactileInteractionEnabled
            => _settings?.InteractionMode == InteractionCapability.Direct;

        /// <summary>
        /// Is the current mode limited to projective interaction (far field)
        /// </summary>
        private bool OnlyProjectionInteractionEnabled
            => _settings?.InteractionMode == InteractionCapability.Indirect;

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

            switch (_settings.InteractionMode)
            {
                case InteractionCapability.Both:
                    ProcessHybrid(projectionOriginProvider, hand);
                    break;
                case InteractionCapability.Direct:
                    ProcessTactile(projectionOriginProvider, hand);
                    break;
                case InteractionCapability.Indirect:
                    ProcessProjective(projectionOriginProvider, hand);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            

            PrevScreenPosition = EventData.position;

            // Trigger events that come from changing pointer state
            RaiseEventsForStateChanges();
            ProcessUnityEvents(hand);
        }
        
        private void ProcessHybrid(IProjectionOriginProvider projectionOriginProvider, Hand hand)
        {
            ProcessTactile(projectionOriginProvider, hand);

            if (PointerState == PointerStates.OffCanvas)
            {
                ProcessProjective(projectionOriginProvider, hand);
            }
        }

        private void ProcessTactile(IProjectionOriginProvider projectionOriginProvider, Hand hand)
        {
            // Raycast from shoulder through tip of the index finger to the UI
            var tipRaycastUsed = GetLookPointerEventData(
                hand,
                projectionOriginProvider.ProjectionOriginForHand(hand),
                forceTipRaycast: true);

            PrevState = PointerState;
            UpdatePointer(EventData);
            EvaluatePointerSize(hand);
            ProcessState(hand, tipRaycastUsed);
        }

        private void ProcessProjective(IProjectionOriginProvider projectionOriginProvider, Hand hand)
        {
            // If didn't hit anything near the fingertip, try doing it again, but through the knuckle this time
            var tipRaycastUsed = GetLookPointerEventData(
                hand,
                projectionOriginProvider.ProjectionOriginForHand(hand),
                forceTipRaycast: false);

            PrevState = PointerState;
            UpdatePointer(EventData);
            EvaluatePointerSize(hand);
            ProcessState(hand, tipRaycastUsed);
        } 
        
        #region Raise Unity Events

        private void ProcessUnityEvents(Hand hand)
        {
            // JM - hic sunt dracones
            if (EventData == null)
            {
                return;
            }

            ProcessUnityEvents_HandleRaycast(hand);
            ProcessUnityEvents_HandleScrolling();
            ProcessUnityEvents_HandleNoLongerInteracting(hand);

            //And for everything else, there is dragging.
            if (EventData.pointerDrag != null && EventData.dragging)
            {
                ExecuteEvents.Execute(EventData.pointerDrag, EventData, ExecuteEvents.dragHandler);
            }

            UpdatePointerColor(hand);
        }

        private void ProcessUnityEvents_HandleRaycast(Hand hand)
        {
            //If we hit something with our Raycast, let's see if we should interact with it
            if (EventData.pointerCurrentRaycast.gameObject == null || PointerState == PointerStates.OffCanvas)
            {
                return;
            }
            
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
                        var gameObjectJustPressed = ExecuteEvents.ExecuteHierarchy(CurrentGameObject, EventData,
                            ExecuteEvents.pointerDownHandler);

                        //If not, see if one has a pointerClickHandler!
                        if (gameObjectJustPressed == null)
                        {
                            var gameObjectJustClicked = ExecuteEvents.ExecuteHierarchy(CurrentGameObject,
                                EventData,
                                ExecuteEvents.pointerClickHandler);

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
                                        ExecuteEvents.GetEventHandler<IDragHandler>(EventData.pointerDrag
                                            .transform
                                            .parent.gameObject);

                                    if (EventData.pointerDrag != null)
                                    {
                                        dragHandler = EventData.pointerDrag.GetComponent<IDragHandler>();

                                        if (dragHandler != null && !(dragHandler is EventTrigger))
                                        {
                                            GameObjectBeingDragged = EventData.pointerDrag;
                                            DragStartPosition = EventData.position;

                                            if (CurrentGameObject &&
                                                CurrentGameObject == GameObjectBeingDragged)
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
                                    // Property OnDragTarget for .EventData.pointerDrag
                                    GameObjectBeingDragged = EventData.pointerDrag; 
                                    DragStartPosition = EventData.position;

                                    if (CurrentGameObject && CurrentGameObject == GameObjectBeingDragged)
                                    {
                                        ExecuteEvents.Execute(EventData.pointerDrag, EventData,
                                            ExecuteEvents.beginDragHandler);
                                        EventData.dragging = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ProcessUnityEvents_HandleScrolling()
        {
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
        }
        
        private void ProcessUnityEvents_HandleNoLongerInteracting(Hand hand)
        {
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
                        ExecuteEvents.ExecuteHierarchy(CurrentGameObjectUnderPointer, EventData,
                            ExecuteEvents.dropHandler);
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
        }
        
        #endregion 
        
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

            EventData.Reset();

            //We're always going to assume we're "Left Clicking", for the benefit of uGUI
            EventData.button = PointerEventData.InputButton.Left;

            //If we're in "Touching Mode", Raycast through the fingers
            Vector3 pointerPosition;
            if (IsTouchingOrNearlyTouchingCanvasOrElement || forceTipRaycast)
            {
                tipRaycast = true;

                var farthest = 0f;
                pointerPosition = hand.GetIndex().TipPosition.ToVector3();
                for (var i = 1; i < 3; i++)
                {
                    var fingerDistance = Vector3.Distance(_mainCamera.transform.position,
                        hand.Fingers[i].TipPosition.ToVector3());
                    var fingerExtension =
                        Mathf.Clamp01(Vector3.Dot(
                            hand.Fingers[i].Direction.ToVector3(),
                            _leapDataProvider.CurrentFrame.Hands[0].Direction.ToVector3())) / 1.5f;

                    if (fingerDistance > farthest && fingerExtension > 0.5f)
                    {
                        farthest = fingerDistance;
                        pointerPosition = hand.Fingers[i].TipPosition.ToVector3(); 
                    }
                }
            }
            else
            {
                //Raycast through the knuckle of the finger
                pointerPosition = _mainCamera.transform.position - origin + hand.Fingers[(int)Finger.FingerType.TYPE_INDEX].Bone(Bone.BoneType.TYPE_METACARPAL).Center.ToVector3();
            }

            //Set the Raycast Direction and Delta
            EventData.position = _mainCamera.WorldToScreenPoint(pointerPosition);
            EventData.delta = EventData.position - PrevScreenPosition;
            EventData.scrollDelta = Vector2.zero;

            //Perform the Raycast and sort all the things we hit by distance... (where distance is the canvas order, not the Z-depth???)
            _eventSystem.RaycastAll(EventData, _raycastResultCache);
            EventData.pointerCurrentRaycast = _inputModuleEventHandler.FindFirstRaycastProxy(_raycastResultCache);

            //Clear the list of things we hit; we don't need it anymore.
            _raycastResultCache.Clear();
            return tipRaycast;
        }

        /// <summary>
        /// Tree to decide the State of the Pointer
        /// </summary>
        /// <param name="hand"></param>
        /// <param name="tipRaycastUsed"></param>
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
                        PointerState = ExecuteEvents.GetEventHandler<IPointerClickHandler>(EventData.pointerCurrentRaycast.gameObject) 
                            ? PointerStates.TouchingElement 
                            : PointerStates.TouchingCanvas;
                    }
                    else
                    {
                        // Are we really near it though. We are over something but not close enough for it to be a tactile interaction????
                        PointerState = PointerStates.NearCanvas; 
                    }
                }
                else if (!tipRaycastUsed)
                { 
                    // Provide more context here ... what is tipRayCast all about and why is it relevant to this logic?
                    if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(EventData.pointerCurrentRaycast.gameObject)) 
                    {
                        // if HitElementCanBeClicked
                        PointerState = IsTriggeringInteraction(hand) 
                            ? PointerStates.PinchingToElement 
                            : PointerStates.OnElement;
                    }
                    else
                    {
                        // Hit a non clickable UI element ...
                        PointerState = IsTriggeringInteraction(hand) 
                            ? PointerStates.PinchingToCanvas 
                            : PointerStates.OnCanvas;
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

        /// <summary>
        /// Update the pointer location and whether or not it is enabled
        /// </summary>
        /// <param name="pointData">Pointer event data</param>
        private void UpdatePointer(PointerEventData pointData)
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
                    var hoverer = ExecuteEvents.GetEventHandler<IPointerEnterHandler>(element);
                    if (hoverer)
                    {
                        var componentInPlane = hoverer.transform.InverseTransformPoint(globalLookPos);
                        componentInPlane = new Vector3(componentInPlane.x, componentInPlane.y, 0f);
                        Pointer.transform.position = hoverer.transform.TransformPoint(componentInPlane);
                    }
                    else
                    {
                        //Amount the pointer floats above the Canvas
                        Pointer.transform.position = globalLookPos - Pointer.transform.forward * 0.01f;
                    }

                    var pointerAngle = Mathf.Rad2Deg * Mathf.Atan2(pointData.delta.x, pointData.delta.y);
                    Pointer.transform.rotation = draggingPlane.rotation * Quaternion.Euler(0f, 0f, -pointerAngle);

                    if (InnerPointer)
                    {
                        //Amount the pointer floats above the Canvas
                        InnerPointer.transform.position = globalLookPos - InnerPointer.transform.forward * 0.01f; 
                        InnerPointer.transform.rotation = draggingPlane.rotation * Quaternion.Euler(0f, 0f, -pointerAngle);
                    }
                }
            }
        }

        private void EvaluatePointerSize(Hand hand)
        {
            //Use the Scale AnimCurve to Evaluate the Size of the Pointer
            float pointDistance = 1f;
            if (_mainCamera != null)
            {
                pointDistance = (Pointer.transform.position - _mainCamera.transform.position).magnitude;
            }

            var pointerScale = _settings.PointerDistanceScale.Evaluate(pointDistance);
            if (!IsTouchingOrNearlyTouchingCanvasOrElement)
            {
                pointerScale *= _settings.PointerPinchScale.Evaluate(hand.PinchDistance);
            }

            Pointer.transform.localScale = pointerScale * new Vector3(1f, 1f, 1f);
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
        
        #region Cursor

        /// <summary>
        /// Updates the pointer by lerping to the relevant colour for the current state
        /// </summary>
        private void UpdatePointerColor(Hand hand)
        {
            var transitionAmount = Mathf.Clamp01(Mathf.Abs(DistanceOfTipToPointer(hand) - _settings.ProjectiveToTactileTransitionDistance) / 0.05f);

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
                    LerpPointerColor(new Color(0.0f, 0.0f, 0.0f, 0.001f), 1f);
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
        
        #endregion
    }
}
