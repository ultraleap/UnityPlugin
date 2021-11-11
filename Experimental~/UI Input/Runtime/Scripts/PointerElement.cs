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

namespace Leap.Unity.InputModule
{
    /// <summary>
    /// Representation of a pointer that can be controlled by the LeapInputModule
    /// </summary>
    [Serializable]
    public class PointerElement : MonoBehaviour
    {
        #region Properties
        
        public event Action<PointerElement, Hand> OnPointerStateChanged;
        public event Action<bool> On;
        
        private Camera mainCamera;
        private LeapProvider leapDataProvider;
        
        [SerializeField] private EventSystem eventSystem;
        [SerializeField] private UIInputModule module;
        [SerializeField] private UIInputCursor cursor;
        [SerializeField] private bool forceDisable = false;
        [SerializeField] private bool disableWhenOffCanvas = true;

        public Chirality Chirality { get; private set; }

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
            {(PointerStates.OnCanvas, PointerStates.OnElement), (module, pointerElement) => module.OnBeginHover.Invoke(module, pointerElement.transform.position) },
            {(PointerStates.OnCanvas, PointerStates.PinchingToCanvas), (module, pointerElement) => module.OnBeginMissed.Invoke(module, pointerElement.transform.position) },
            {(PointerStates.PinchingToCanvas, PointerStates.OnCanvas), (module, pointerElement) => module.OnEndMissed.Invoke(module, pointerElement.transform.position) },
            {(PointerStates.OnElement, PointerStates.OnCanvas), (module, pointerElement) => module.OnEndHover.Invoke(module, pointerElement.transform.position) },
            {(PointerStates.OnElement, PointerStates.PinchingToElement), (module, pointerElement) => module.OnClickDown.Invoke(module, pointerElement.transform.position) },
            {(PointerStates.PinchingToElement, PointerStates.OnElement), (module, pointerElement) => module.OnClickUp.Invoke(module, pointerElement.transform.position) },
            {(PointerStates.PinchingToElement, PointerStates.OnCanvas), (module, pointerElement) => module.OnClickUp.Invoke(module, pointerElement.transform.position) },
            {(PointerStates.NearCanvas, PointerStates.TouchingElement), (module, pointerElement) => module.OnClickDown.Invoke(module, pointerElement.transform.position) },
            {(PointerStates.NearCanvas, PointerStates.TouchingCanvas), (module, pointerElement) => module.OnBeginMissed.Invoke(module, pointerElement.transform.position) },
            {(PointerStates.TouchingCanvas, PointerStates.NearCanvas), (module, pointerElement) => module.OnEndMissed.Invoke(module, pointerElement.transform.position) },
            {(PointerStates.TouchingElement, PointerStates.NearCanvas), (module, pointerElement) => module.OnClickUp.Invoke(module, pointerElement.transform.position) },
            {(PointerStates.OffCanvas, PointerStates.OffCanvas), (module, pointerElement) => pointerElement.TimeEnteredCanvas = Time.time },
        };
        
        #endregion
        
        private void Start()
        {
            EventData = new PointerEventData(eventSystem);
            
            leapDataProvider = module.LeapDataProvider;
            mainCamera = module.MainCamera;
        }

        /// <summary>
        /// The z position of the index finger tip to the Pointer
        /// </summary>
        private float DistanceOfTipToPointer(Hand hand)
        {
            var tipPosition = hand.Fingers[(int)Finger.FingerType.TYPE_INDEX]
                .Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();

            var pointerTransform = transform;
            return -pointerTransform.transform.InverseTransformPoint(tipPosition).z * pointerTransform.transform.lossyScale.z - module.TactilePadding;
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
            if (module.InteractionMode != InteractionCapability.Indirect)
            {
                if (IsTouchingOrNearlyTouchingCanvasOrElement)
                {
                    return DistanceOfTipToPointer(hand) < 0f;
                }
            }
            
            // N.B. Without pinch detector
            if (module.InteractionMode != InteractionCapability.Direct)
            {
                if ( hand.PinchDistance < module.PinchingThreshold)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        public bool HasMatchingChirality(Hand hand)
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
            => module != null && module.InteractionMode == InteractionCapability.Direct;

        /// <summary>
        /// Is the current mode limited to projective interaction (far field)
        /// </summary>
        private bool OnlyProjectionInteractionEnabled
            => module != null && module.InteractionMode == InteractionCapability.Indirect;

        /// <summary>
        /// Is tactile interaction allowed and is the pointer tip distance within the tactile interaction distance
        /// </summary>
        private bool IsPermittedTactileInteraction(Hand hand)
            => OnlyTactileInteractionEnabled || !OnlyProjectionInteractionEnabled && DistanceOfTipToPointer(hand) < module.ProjectiveToTactileTransitionDistance;

        internal void Process(Hand hand, IProjectionOriginProvider projectionOriginProvider)
        {
            //Control cursor display
            cursor.gameObject.SetActive(true);
            
            if (forceDisable)
            {
                cursor.gameObject.SetActive(false);
            }
            
            if (hand == null || (disableWhenOffCanvas && PointerState == PointerStates.OffCanvas))
            {
                if (gameObject.activeInHierarchy)
                {
                    cursor.gameObject.SetActive(false);
                    if (hand == null) return;
                }
            }

            //Select interaction
            switch (module.InteractionMode)
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
            module.HandlePointerExitAndEnterProxy(EventData, CurrentGameObjectUnderPointer);

            if (!PrevTriggeringInteraction && IsTriggeringInteraction(hand))
            {
                PrevTriggeringInteraction = true;

                if (Time.time - TimeEnteredCanvas >= Time.deltaTime)
                {
                    //Deselect all objects
                    if (eventSystem.currentSelectedGameObject)
                    {
                        eventSystem.SetSelectedGameObject(null);
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
                        }

                        if (gameObjectJustPressed != null)
                        {
                            EventData.pointerPress = gameObjectJustPressed;
                            CurrentGameObject = gameObjectJustPressed;

                            //Select the currently pressed object
                            if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(CurrentGameObject))
                            {
                                eventSystem.SetSelectedGameObject(CurrentGameObject);
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
                    var fingerDistance = Vector3.Distance(mainCamera.transform.position,
                        hand.Fingers[i].TipPosition.ToVector3());
                    var fingerExtension =
                        Mathf.Clamp01(Vector3.Dot(
                            hand.Fingers[i].Direction.ToVector3(),
                            leapDataProvider.CurrentFrame.Hands[0].Direction.ToVector3())) / 1.5f;

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
                pointerPosition = mainCamera.transform.position - origin + hand.Fingers[(int)Finger.FingerType.TYPE_INDEX].Bone(Bone.BoneType.TYPE_METACARPAL).Center.ToVector3();
            }

            //Set the Raycast Direction and Delta
            EventData.position = mainCamera.WorldToScreenPoint(pointerPosition);
            EventData.delta = EventData.position - PrevScreenPosition;
            EventData.scrollDelta = Vector2.zero;

            //Perform the Raycast and sort all the things we hit by distance
            eventSystem.RaycastAll(EventData, _raycastResultCache);
            EventData.pointerCurrentRaycast = UIInputModule.FindFirstRaycast(_raycastResultCache);

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
                        PointerState = PointerStates.NearCanvas; 
                    }
                }
                else if (!tipRaycastUsed)
                { 
                    if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(EventData.pointerCurrentRaycast.gameObject)) 
                    {
                        PointerState = IsTriggeringInteraction(hand) 
                            ? PointerStates.PinchingToElement 
                            : PointerStates.OnElement;
                    }
                    else
                    {
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
            
            OnPointerStateChanged?.Invoke(this, hand);
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
                var draggingPlane = EventData.pointerCurrentRaycast.gameObject.GetComponent<RectTransform>();

                if (RectTransformUtility.ScreenPointToWorldPointInRectangle(draggingPlane, pointData.position,
                    pointData.enterEventCamera, out var globalLookPos))
                {
                    var hoverer = ExecuteEvents.GetEventHandler<IPointerEnterHandler>(element);
                    if (hoverer)
                    {
                        var componentInPlane = hoverer.transform.InverseTransformPoint(globalLookPos);
                        componentInPlane = new Vector3(componentInPlane.x, componentInPlane.y, 0f);
                        transform.position = hoverer.transform.TransformPoint(componentInPlane);
                    }
                    else
                    {
                        //Amount the pointer floats above the Canvas
                        transform.position = globalLookPos - transform.forward * 0.01f;
                    }

                    transform.rotation = draggingPlane.rotation;
                }
            }
        }
        
        private void RaiseEventsForStateChanges()
        {
            if (module.TriggerHoverOnElementSwitch)
            {
                if (PrevState != PointerStates.OffCanvas && PointerState != PointerStates.OffCanvas)
                {
                    if (CurrentGameObjectUnderPointer != PreviousGameObjectUnderPointer)
                    {
                        if (module is IInputModuleEventHandler eventHandler)
                        {
                            //When you begin to hover on an element
                            eventHandler?.OnBeginHover?.Invoke(module, transform.position);
                        }
                    }
                }
            }

            StateChangeActionMap.TryGetValue((PrevState, PointerState), out var result);
            result?.Invoke(module, this);
        }
    }
}
