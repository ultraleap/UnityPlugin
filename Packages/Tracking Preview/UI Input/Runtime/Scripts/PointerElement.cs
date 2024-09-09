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
using UnityEngine.UI;

namespace Leap.InputModule
{
    /// <summary>
    /// Representation of a pointer that can be controlled by the LeapInputModule
    /// </summary>
    [Serializable]
    public class PointerElement : MonoBehaviour
    {
        #region Properties

        public event Action<PointerElement, Hand> OnPointerStateChanged;

        [SerializeField] private EventSystem eventSystem;
        [SerializeField] private UIInputModule module;
        [SerializeField] private UIInputCursor cursor;
        [SerializeField] private bool forceDisable = false;
        [SerializeField] private bool disableWhenOffCanvas = true;
        [SerializeField] private Finger.FingerType finger = Finger.FingerType.INDEX;

        public Chirality Chirality { get; private set; }

        private PointerEventData EventData { get; set; }
        public PointerStates PointerStateTactile { get; private set; }
        public PointerStates PointerStateProjective { get; private set; }

        /// <summary>
        /// Gets the aggregated pointer state, based on the interaction mode and the projective and tactile states
        /// </summary>
        public PointerStates AggregatePointerState
        {
            get
            {
                switch (module?.InteractionMode)
                {
                    case InteractionCapability.Both:
                        if (PointerStateTactile == PointerStates.OffCanvas)
                        {
                            return PointerStateProjective;
                        }
                        else
                        {
                            return PointerStateTactile;
                        }

                    case InteractionCapability.Direct:
                        return PointerStateTactile;

                    case InteractionCapability.Indirect:
                        return PointerStateProjective;

                    default:
                        Debug.LogWarning($"Unknown interaction mode");
                        break;
                }

                throw new Exception("Unknown interaction mode");
            }
        }

        private PointerStates PrevStateTactile { get; set; }
        private PointerStates PrevStateProjective { get; set; }
        private Vector2 PrevScreenPosition { get; set; }
        private Vector2 DragStartPosition { get; set; }

        private GameObject PreviousGameObjectUnderPointer { get; set; }
        private GameObject CurrentGameObject { get; set; }

        private GameObject GameObjectBeingDragged { get; set; }
        private GameObject CurrentGameObjectUnderPointer { get; set; }

        private bool PrevTriggeringInteraction { get; set; }
        private float TimeEnteredCanvas { get; set; }

        private bool hadHandLastProcessUpdate = false;

        private List<RaycastResult> _raycastResultCache = new List<RaycastResult>();

        private static readonly Dictionary<(PointerStates from, PointerStates to), (string ActionName, Action<IInputModuleEventHandler, PointerElement> Action)> StateChangeActionMap
            = new Dictionary<(PointerStates prev, PointerStates pointer), (string, Action<IInputModuleEventHandler, PointerElement>)>
        {
            {(PointerStates.OnCanvas, PointerStates.OnElement), ("OnBeginHover", (module, pointerElement) => module.OnBeginHover?.Invoke(module, pointerElement.transform.position)) },
            {(PointerStates.OnCanvas, PointerStates.PinchingToCanvas), ("OnBeginMissed", (module, pointerElement) => module.OnBeginMissed?.Invoke(module, pointerElement.transform.position)) },
            {(PointerStates.PinchingToCanvas, PointerStates.OnCanvas), ("OnEndMissed", (module, pointerElement) => module.OnEndMissed?.Invoke(module, pointerElement.transform.position)) },
            {(PointerStates.OnElement, PointerStates.OnCanvas), ("OnEndHover", (module, pointerElement) => module.OnEndHover?.Invoke(module, pointerElement.transform.position)) },
            {(PointerStates.OnElement, PointerStates.PinchingToElement), ("OnClickDown", (module, pointerElement) => module.OnClickDown?.Invoke(module, pointerElement.transform.position)) },
            {(PointerStates.PinchingToElement, PointerStates.OnElement), ("OnClickUp", (module, pointerElement) => module.OnClickUp?.Invoke(module, pointerElement.transform.position)) },
            {(PointerStates.PinchingToElement, PointerStates.OnCanvas), ("OnClickUp", (module, pointerElement) => module.OnClickUp?.Invoke(module, pointerElement.transform.position)) },

            {(PointerStates.NearCanvas, PointerStates.TouchingElement), ("OnClickDown", (module, pointerElement) => module.OnClickDown?.Invoke(module, pointerElement.transform.position)) },
            {(PointerStates.NearCanvas, PointerStates.TouchingCanvas), ("OnBeginMissed", (module, pointerElement) => module.OnBeginMissed?.Invoke(module, pointerElement.transform.position)) },
            {(PointerStates.TouchingCanvas, PointerStates.NearCanvas), ("OnEndMissed", (module, pointerElement) => module.OnEndMissed?.Invoke(module, pointerElement.transform.position)) },
            {(PointerStates.TouchingElement, PointerStates.NearCanvas), ("OnClickUp", (module, pointerElement) => module.OnClickUp?.Invoke(module, pointerElement.transform.position)) }
        };

        /// <summary>
        /// Returns true if the user is interacting directly with the user interface (tactile mode)
        /// </summary>
        public bool IsUserInteractingDirectly
        {
            get
            {
                switch (module?.InteractionMode)
                {
                    case InteractionCapability.Both:
                    case InteractionCapability.Direct:

                        return (PointerStateTactile == PointerStates.OnCanvas ||
                            PointerStateTactile == PointerStates.OnElement ||
                            PointerStateTactile == PointerStates.NearCanvas ||
                            PointerStateTactile == PointerStates.TouchingCanvas ||
                            PointerStateTactile == PointerStates.TouchingElement);

                    case InteractionCapability.Indirect:
                        return false;

                    default:
                        break;
                }

                throw new Exception("Unknown interaction mode");
            }
        }

        /// <summary>
        /// Should the cursor be shown when the user is interacting directly with the canvas/elements
        /// </summary>
        public bool ShowDirectPointerCursor
        {
            get
            {
                return module.ShowDirectPointerCursor;
            }
        }

        #endregion

        private void Start()
        {
            EventData = new PointerEventData(eventSystem);
        }

        /// <summary>
        /// The z position of the finger tip to the Pointer
        /// </summary>
        public float DistanceOfTipToPointer(Hand hand)
        {
            var tipPosition = hand.fingers[(int)finger].GetBone(Bone.BoneType.DISTAL).NextJoint;
            var pointerTransform = transform;
            return -pointerTransform.transform.InverseTransformPoint(tipPosition).z * pointerTransform.transform.lossyScale.z - module.TactilePadding;
        }

        /// <summary>
        /// Returns true if the specified pointer is in the "touching" interaction mode, i.e, whether it is touching or nearly touching a canvas or control.
        /// </summary>
        private bool IsTouchingOrNearlyTouchingCanvasOrElement()
        {
            switch (module?.InteractionMode)
            {
                case InteractionCapability.Both:
                    return IsTouchingOrNearlyTouchingCanvasOrElement(PointerStateProjective) || IsTouchingOrNearlyTouchingCanvasOrElement(PointerStateTactile);

                case InteractionCapability.Direct:
                    return IsTouchingOrNearlyTouchingCanvasOrElement(PointerStateTactile);

                case InteractionCapability.Indirect:
                    return IsTouchingOrNearlyTouchingCanvasOrElement(PointerStateProjective);

                default:
                    break;
            }

            throw new Exception("Unknown interaction mode");
        }

        private bool IsTouchingOrNearlyTouchingCanvasOrElement(PointerStates pointerState)
        {
            return pointerState == PointerStates.NearCanvas ||
                   pointerState == PointerStates.TouchingCanvas ||
                   pointerState == PointerStates.TouchingElement;
        }

        /// <summary>
        /// Returns true if the pointer was interacting previously, but no longer is
        /// </summary>
        private bool NoLongerInteracting(Hand hand)
        {
            switch (module?.InteractionMode)
            {
                case InteractionCapability.Both:
                    return (PrevTriggeringInteraction && (!IsTriggeringInteraction(hand) ||
                        (PointerStateTactile == PointerStates.OffCanvas && PointerStateProjective == PointerStates.OffCanvas)));

                case InteractionCapability.Direct:
                    return (PrevTriggeringInteraction && (!IsTriggeringInteraction(hand) || PointerStateTactile == PointerStates.OffCanvas));

                case InteractionCapability.Indirect:
                    return (PrevTriggeringInteraction && (!IsTriggeringInteraction(hand) || PointerStateProjective == PointerStates.OffCanvas));

                default:
                    break;
            }

            throw new Exception("Unknown interaction mode");
        }

        /// <summary>
        /// Returns true if the pointer was interacting previously, but no longer is
        /// </summary>
        private bool OffCanvas()
        {
            switch (module?.InteractionMode)
            {
                case InteractionCapability.Both:
                    return PointerStateTactile == PointerStates.OffCanvas && PointerStateProjective == PointerStates.OffCanvas;

                case InteractionCapability.Direct:
                    return PointerStateTactile == PointerStates.OffCanvas;

                case InteractionCapability.Indirect:
                    return PointerStateProjective == PointerStates.OffCanvas;

                default:
                    break;
            }

            throw new Exception("Unknown interaction mode");
        }


        /// <summary>
        /// Returns true if a "click" is being triggered during the current frame.
        /// </summary>
        private bool IsTriggeringInteraction(Hand hand)
        {
            if (module.InteractionMode != InteractionCapability.Indirect)
            {
                if (IsTouchingOrNearlyTouchingCanvasOrElement())
                {
                    // Is fingertip beyond the pointer - e.g. pushed past the button surface?
                    var val = DistanceOfTipToPointer(hand) < 0f;
                    return val;
                }
            }

            // N.B. Without pinch detector
            if (module.InteractionMode != InteractionCapability.Direct)
            {
                if (hand.PinchDistance < module.PinchingThreshold)
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
            if (hand == null)
            {
                if (cursor.gameObject.activeSelf)
                {
                    cursor.gameObject.SetActive(false);
                }

                if (hadHandLastProcessUpdate)
                {
                    CancelAllInput(hand);
                    hadHandLastProcessUpdate = false;
                }

                return;
            }
            else
            {
                hadHandLastProcessUpdate = true;

                if (forceDisable)
                {
                    cursor.gameObject.SetActive(false);
                }
                else if (disableWhenOffCanvas && OffCanvas())
                {
                    if (cursor.gameObject.activeSelf)
                    {
                        cursor.gameObject.SetActive(false);
                    }
                }
                else if (!cursor.gameObject.activeSelf)
                {
                    cursor.gameObject.SetActive(true);
                }
            }

            //Select interaction
            switch (module?.InteractionMode)
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

            if (EventData != null)
            {
                PrevScreenPosition = EventData.position;
            }

            switch (module?.InteractionMode)
            {
                case InteractionCapability.Both:

                    // Only raise projective events if there are no interesting tactile interactions happening
                    if (PointerStateTactile != PointerStates.OffCanvas && PrevStateTactile != PointerStates.OffCanvas)
                    {
                        RaiseEventsForStateChanges(PointerStateTactile, PrevStateTactile);
                    }

                    RaiseEventsForStateChanges(PointerStateProjective, PrevStateProjective);
                    break;

                case InteractionCapability.Direct:
                    RaiseEventsForStateChanges(PointerStateTactile, PrevStateTactile);
                    break;

                case InteractionCapability.Indirect:
                    RaiseEventsForStateChanges(PointerStateProjective, PrevStateProjective);
                    break;
            }

            ResetTimeEnteredCanvas();
            ProcessUnityEvents(hand);
        }

        void CancelAllInput(Hand hand)
        {
            PrevStateProjective = PointerStateProjective;
            PrevStateTactile = PointerStateTactile;

            UpdatePointer(EventData);

            PointerStateTactile = PointerStates.OffCanvas;
            PointerStateProjective = PointerStates.OffCanvas;

            OnPointerStateChanged?.Invoke(this, hand);

            if (EventData != null)
            {
                PrevScreenPosition = EventData.position;
            }

            switch (module?.InteractionMode)
            {
                case InteractionCapability.Both:

                    // Only raise projective events if there are no interesting tactile interactions happening
                    if (PointerStateTactile != PointerStates.OffCanvas && PrevStateTactile != PointerStates.OffCanvas)
                    {
                        RaiseEventsForStateChanges(PointerStateTactile, PrevStateTactile);
                    }

                    RaiseEventsForStateChanges(PointerStateProjective, PrevStateProjective);
                    break;

                case InteractionCapability.Direct:
                    RaiseEventsForStateChanges(PointerStateTactile, PrevStateTactile);
                    break;

                case InteractionCapability.Indirect:
                    RaiseEventsForStateChanges(PointerStateProjective, PrevStateProjective);
                    break;
            }

            ResetTimeEnteredCanvas();
            ProcessUnityEvents(hand);

            PreviousGameObjectUnderPointer = CurrentGameObjectUnderPointer;
            CurrentGameObjectUnderPointer = null;

            //Trigger Enter or Exit Events on the UI Element (like highlighting)
            module.HandlePointerExitAndEnterProxy(EventData, CurrentGameObjectUnderPointer);
        }

        private void ResetTimeEnteredCanvas()
        {
            switch (module?.InteractionMode)
            {
                case InteractionCapability.Both:
                    if (PointerStateProjective == PointerStates.OffCanvas && PrevStateProjective == PointerStates.OffCanvas && PointerStateTactile == PointerStates.OffCanvas && PrevStateTactile == PointerStates.OffCanvas)
                    {
                        TimeEnteredCanvas = Time.time;
                    }
                    break;

                case InteractionCapability.Direct:
                    if (PointerStateTactile == PointerStates.OffCanvas && PrevStateTactile == PointerStates.OffCanvas)
                    {
                        TimeEnteredCanvas = Time.time;
                    }
                    break;

                case InteractionCapability.Indirect:
                    if (PointerStateProjective == PointerStates.OffCanvas && PrevStateProjective == PointerStates.OffCanvas)
                    {
                        TimeEnteredCanvas = Time.time;
                    }
                    break;
            }
        }

        private void ProcessHybrid(IProjectionOriginProvider projectionOriginProvider, Hand hand)
        {
            ProcessTactile(projectionOriginProvider, hand);

            // If nothing interesting is happening in terms of direct/tactile interaction, then process the indirect/projective interaction
            if (PointerStateTactile == PointerStates.OffCanvas)
            {
                ProcessProjective(projectionOriginProvider, hand);
            }
        }

        private void ProcessTactile(IProjectionOriginProvider projectionOriginProvider, Hand hand)
        {
            // Raycast from shoulder through tip of the finger to the UI
            var tipRaycastUsed = GetLookPointerEventData(
                hand,
                projectionOriginProvider.ProjectionOriginForHand(hand),
                forceTipRaycast: true);

            PrevStateTactile = PointerStateTactile;
            UpdatePointer(EventData);
            PointerStateTactile = ProcessState(hand, tipRaycastUsed, PointerStateTactile);
            OnPointerStateChanged?.Invoke(this, hand);
        }

        private void ProcessProjective(IProjectionOriginProvider projectionOriginProvider, Hand hand)
        {
            // If didn't hit anything near the fingertip, try doing it again, but through the knuckle this time
            var tipRaycastUsed = GetLookPointerEventData(
                hand,
                projectionOriginProvider.ProjectionOriginForHand(hand),
                forceTipRaycast: false);

            PrevStateProjective = PointerStateProjective;
            UpdatePointer(EventData);
            PointerStateProjective = ProcessState(hand, tipRaycastUsed, PointerStateProjective);
            OnPointerStateChanged?.Invoke(this, hand);
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
            if (EventData.pointerCurrentRaycast.gameObject == null || OffCanvas())
            {
                PreviousGameObjectUnderPointer = CurrentGameObjectUnderPointer;
                CurrentGameObjectUnderPointer = null;

                //Trigger Enter or Exit Events on the UI Element (like highlighting)
                module.HandlePointerExitAndEnterProxy(EventData, CurrentGameObjectUnderPointer);
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

                        // Find something in the hierarchy that implements dragging, starting at this GO and searching up
                        EventData.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(CurrentGameObject);

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
            if (hand == null || NoLongerInteracting(hand))
            {
                PrevTriggeringInteraction = false;

                if (GameObjectBeingDragged)
                {
                    ExecuteEvents.Execute(GameObjectBeingDragged, EventData, ExecuteEvents.endDragHandler);

                    if (CurrentGameObject && GameObjectBeingDragged == CurrentGameObject)
                    {
                        ExecuteEvents.Execute(GameObjectBeingDragged, EventData, ExecuteEvents.pointerUpHandler);
                    }

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

            if (EventData == null)
            {
                return false;
            }

            EventData.Reset();

            //We're always going to assume we're "Left Clicking", for the benefit of uGUI
            EventData.button = PointerEventData.InputButton.Left;

            //If we're in "Touching Mode", Raycast through the finger
            Vector3 pointerPosition;
            if (IsTouchingOrNearlyTouchingCanvasOrElement() || forceTipRaycast)
            {
                tipRaycast = true;
                pointerPosition = hand.fingers[(int)finger].TipPosition;
            }
            else
            {
                //Raycast through the knuckle of the finger
                pointerPosition = module.MainCamera.transform.position - origin + hand.fingers[(int)finger].GetBone(Bone.BoneType.METACARPAL).Center;
            }

            //Set the Raycast Direction and Delta
            EventData.position = module.MainCamera.WorldToScreenPoint(pointerPosition);
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
        private PointerStates ProcessState(Hand hand, bool tipRaycastUsed, PointerStates PointerState)
        {
            if (EventData == null)
            {
                return PointerState;
            }

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
                    return PointerState;
                }
            }
            else
            {
                PointerState = PointerStates.OffCanvas;
                return PointerState;
            }

            return PointerState;
        }

        /// <summary>
        /// Update the pointer location and whether or not it is enabled
        /// </summary>
        /// <param name="pointData">Pointer event data</param>
        private void UpdatePointer(PointerEventData pointData)
        {
            if (EventData == null)
            {
                return;
            }

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
                        transform.position = globalLookPos;
                    }

                    transform.rotation = draggingPlane.rotation;
                }
            }
        }

        private void RaiseEventsForStateChanges(PointerStates pointerState, PointerStates prevState)
        {
            if (module.TriggerHoverOnElementSwitch)
            {
                if (prevState != PointerStates.OffCanvas && pointerState != PointerStates.OffCanvas)
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

            if (StateChangeActionMap.TryGetValue((prevState, pointerState), out var result))
            {
                result.Action.Invoke(module, this);
            }
        }
    }
}