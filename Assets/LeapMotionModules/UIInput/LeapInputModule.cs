using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using System.Collections.Generic;
using Leap;
using Leap.Unity;
using UnityEngine.VR;

public class LeapInputModule : BaseInputModule {
    public LeapProvider LeapDataProvider;
    public int NumberOfHands = 2;
    public float ProjectiveToTactileTransitionDistance = 0.12f;
    public float PinchingThreshold = 0.8f;
    public bool OverrideScrollViewClicks = false;
    public bool DrawDebug = false;

    [Header(" [Pointer setup]")]
    public Sprite PointerSprite;
    public Material PointerMaterial;
    public float NormalPointerScale = 0.00025f; //In world space
    private RectTransform[] Pointers;
    public Color NormalColor = Color.white;
    public Color HoveringColor = Color.green;
    public AudioClip HoverSound;
    public Color TriggeringColor = Color.gray;
    public AudioClip TriggerSound;
    public Color TriggerMissedColor = Color.gray;
    public AudioClip MissedSound;

    // Event delegates triggered on click.
    public UnityEvent onClickDown;
    public UnityEvent onClickUp;
    public UnityEvent onHover;
    public UnityEvent whileClickHeld;

    private pointerStates[] pointerState;
    private pointerStates[] OldState;
    private AudioSource PointerSounds;

    private PointerEventData[] PointEvents;
    private Camera EventCamera;

    private GameObject[] CurrentPoint;
    private GameObject[] CurrentPressed;
    private GameObject[] CurrentDragging;

    private Queue<Vector3> DebugSphereQueue;
    Canvas[] canvases;

    private bool[] OldTriggeringInteraction;
    private Quaternion CurrentRotation;
    private Vector2[] PrevScreenPosition;



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


    // Use this for initialization
    protected override void Start() {
        base.Start();

        EventCamera = new GameObject("Leap UI Selection Camera").AddComponent<Camera>();
        EventCamera.clearFlags = CameraClearFlags.Nothing;
        EventCamera.cullingMask = 0;
        EventCamera.nearClipPlane = 0.01f;
        EventCamera.fieldOfView = 179f;
        EventCamera.transform.SetParent(this.transform);

        canvases = GameObject.FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases) {
            canvas.worldCamera = EventCamera;
        }

        Pointers = new RectTransform[NumberOfHands];
        for (int index = 0; index < Pointers.Length; index++) {
            GameObject pointer = new GameObject("Pointer " + index);
            Canvas canvas = pointer.AddComponent<Canvas>();
            pointer.AddComponent<CanvasRenderer>();
            pointer.AddComponent<CanvasScaler>();
            pointer.AddComponent<GraphicRaycaster>();

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 1000; //set to be on top of everything

            UnityEngine.UI.Image image = pointer.AddComponent<UnityEngine.UI.Image>();
            image.sprite = PointerSprite;
            image.material = Instantiate(PointerMaterial);
            image.raycastTarget = false;

            if (PointerSprite == null)
                Debug.LogError("Set PointerSprite on " + this.gameObject.name + " to the sprite you want to use as your pointer.", this.gameObject);

            Pointers[index] = pointer.GetComponent<RectTransform>();
        }

        PointerSounds = this.gameObject.AddComponent<AudioSource>();
        //for (int index = 0; index < PointerSounds.Length; index++) {
        //    PointerSounds[index] = new AudioSource();
        //}

        CurrentPoint = new GameObject[NumberOfHands];
        CurrentPressed = new GameObject[NumberOfHands];
        CurrentDragging = new GameObject[NumberOfHands];

        OldTriggeringInteraction = new bool[NumberOfHands];
        PrevScreenPosition = new Vector2[NumberOfHands];
        pointerState = new pointerStates[NumberOfHands];
        OldState = new pointerStates[NumberOfHands];

        CurrentRotation = InputTracking.GetLocalRotation(VRNode.Head);

        PointEvents = new PointerEventData[NumberOfHands];
        if (DrawDebug) {
            DebugSphereQueue = new Queue<Vector3>();
        }
    }

    void Update() {
        Quaternion HeadYaw = Quaternion.Euler(0f, InputTracking.GetLocalRotation(VRNode.Head).eulerAngles.y, 0f);
        CurrentRotation = Quaternion.Slerp(CurrentRotation, HeadYaw, 0.01f);
    }

    //Process is called by UI system to process events
    public override void Process() {
        //DebugSphereQueue.Enqueue(InputTracking.GetLocalPosition(VRNode.CenterEye));

        //Send update events if there is a selected object - this is important for InputField to receive keyboard events
        SendUpdateEventToSelectedObject();

        //See if there is a UI element that is currently being looked at
        for (int whichHand = 0; whichHand < NumberOfHands; whichHand++) {
            if (LeapDataProvider.CurrentFrame.Hands.Count - 1 < whichHand) {
                if (Pointers[whichHand].gameObject.activeInHierarchy == true) {
                    Pointers[whichHand].gameObject.SetActive(false);
                }
                continue;
            }

            //Calculate Shoulders
            Vector3 ProjectionOrigin = Vector3.zero;
            switch (LeapDataProvider.CurrentFrame.Hands[whichHand].IsRight) {
                case true:
                    ProjectionOrigin = InputTracking.GetLocalPosition(VRNode.Head) + CurrentRotation * new Vector3(0.15f,-0.2f,0f);
                    break;
                case false:
                    ProjectionOrigin = InputTracking.GetLocalPosition(VRNode.Head) + CurrentRotation * new Vector3(-0.15f, -0.2f, 0f);
                    break;
            }

            //Draw Debug things
            DebugSphereQueue.Enqueue(ProjectionOrigin);
            if (DrawDebug)
                Debug.DrawRay(ProjectionOrigin, CurrentRotation * Vector3.forward * 5f);


            //Raycast from shoulder through index finger to the UI
            GetLookPointerEventData(whichHand, ProjectionOrigin, CurrentRotation * Vector3.forward);

            if (PointEvents[whichHand].pointerCurrentRaycast.gameObject != null) {
                CurrentPoint[whichHand] = PointEvents[whichHand].pointerCurrentRaycast.gameObject;

                //Handle enter and exit events (highlight)
                base.HandlePointerExitAndEnter(PointEvents[whichHand], CurrentPoint[whichHand]);

                //Update cursor
                UpdatePointer(whichHand, PointEvents[whichHand]);

                if (!OldTriggeringInteraction[whichHand] && isTriggeringInteraction(whichHand)) {
                    ClearSelection();
                    OldTriggeringInteraction[whichHand] = true;

                    PointEvents[whichHand].pressPosition = PointEvents[whichHand].position;
                    PointEvents[whichHand].pointerPressRaycast = PointEvents[whichHand].pointerCurrentRaycast;
                    PointEvents[whichHand].pointerPress = null;

                    if (CurrentPoint[whichHand] != null) {
                        CurrentPressed[whichHand] = CurrentPoint[whichHand];

                        GameObject newPressed = ExecuteEvents.ExecuteHierarchy(CurrentPressed[whichHand], PointEvents[whichHand], ExecuteEvents.pointerDownHandler);

                        if (newPressed == null) {
                            //Some UI elements might only have click handler and not pointer down handler
                            newPressed = ExecuteEvents.ExecuteHierarchy(CurrentPressed[whichHand], PointEvents[whichHand], ExecuteEvents.pointerClickHandler);
                            if (newPressed != null) {
                                CurrentPressed[whichHand] = newPressed;
                            }
                        } else {
                            CurrentPressed[whichHand] = newPressed;
                            //We want to do "click on button down" at same time, unlike regular mouse processing
                            //Which does click when mouse goes up over same object it went down on
                            //The reason to do this is head tracking might be jittery and this makes it easier to click buttons
                            ExecuteEvents.Execute(newPressed, PointEvents[whichHand], ExecuteEvents.pointerClickHandler);
                        }

                        if (newPressed != null) {
                            PointEvents[whichHand].pointerPress = newPressed;
                            CurrentPressed[whichHand] = newPressed;
                            Select(CurrentPressed[whichHand]);
                        }

                        ExecuteEvents.Execute(CurrentPressed[whichHand], PointEvents[whichHand], ExecuteEvents.beginDragHandler);
                        PointEvents[whichHand].pointerDrag = CurrentPressed[whichHand];
                        CurrentDragging[whichHand] = CurrentPressed[whichHand];
                    }
                }

                if (OldTriggeringInteraction[whichHand] && !isTriggeringInteraction(whichHand)) {
                    OldTriggeringInteraction[whichHand] = false;

                    if (CurrentDragging[whichHand]) {
                        ExecuteEvents.Execute(CurrentDragging[whichHand], PointEvents[whichHand], ExecuteEvents.endDragHandler);
                        if (CurrentPoint[whichHand] != null) {
                            ExecuteEvents.ExecuteHierarchy(CurrentPoint[whichHand], PointEvents[whichHand], ExecuteEvents.dropHandler);
                        }
                        PointEvents[whichHand].pointerDrag = null;
                        PointEvents[whichHand].dragging = false;
                        CurrentDragging[whichHand] = null;
                    }
                    if (CurrentPressed[whichHand]) {
                        ExecuteEvents.Execute(CurrentPressed[whichHand], PointEvents[whichHand], ExecuteEvents.pointerUpHandler);
                        PointEvents[whichHand].rawPointerPress = null;
                        PointEvents[whichHand].pointerPress = null;
                        CurrentPressed[whichHand] = null;
                    }
                }

                // drag handling
                if (CurrentDragging[whichHand] != null) {
                    ExecuteEvents.Execute(CurrentDragging[whichHand], PointEvents[whichHand], ExecuteEvents.dragHandler);
                }
            }
            switch (pointerState[whichHand]) {
                case pointerStates.OnCanvas:
                    lerpPointerColor(whichHand, new Color(0f, 0f, 0f, 1f), 0.2f);
                    lerpPointerColor(whichHand, NormalColor, 0.2f);
                    break;
                case pointerStates.OnElement:
                    lerpPointerColor(whichHand, new Color(0f, 0f, 0f, 1f), 0.2f);
                    lerpPointerColor(whichHand, HoveringColor, 0.2f);
                    break;
                case pointerStates.PinchingToCanvas:
                    lerpPointerColor(whichHand, new Color(0f, 0f, 0f, 1f), 0.2f);
                    lerpPointerColor(whichHand, TriggerMissedColor, 0.2f);
                    break;
                case pointerStates.PinchingToElement:
                    lerpPointerColor(whichHand, new Color(0f, 0f, 0f, 1f), 0.2f);
                    lerpPointerColor(whichHand, TriggeringColor, 0.2f);
                    break;
                case pointerStates.NearCanvas:
                    lerpPointerColor(whichHand, new Color(0f, 0f, 0f, 0f), 1f);
                    break;
                case pointerStates.TouchingElement:
                    lerpPointerColor(whichHand, new Color(0f, 0f, 0f, 0f), 0.2f);
                    break;
                case pointerStates.TouchingCanvas:
                    lerpPointerColor(whichHand, new Color(0f, 0f, 0f, 0f), 0.2f);
                    break;
                case pointerStates.OffCanvas:
                    lerpPointerColor(whichHand, new Color(0f, 0f, 0f, 0f), 0.2f);
                    break;
            }
        }
    }

    private void GetLookPointerEventData(int whichHand, Vector3 Origin, Vector3 Direction) {
        //Construct PointerEvent
        if (PointEvents[whichHand] == null) {
            PointEvents[whichHand] = new PointerEventData(base.eventSystem);
        } else {
            PointEvents[whichHand].Reset();
        }

        PointEvents[whichHand].button = PointerEventData.InputButton.Left;

        //Get Base of Index Finger Position and set EventCamera Origin
        Vector3 IndexFingerPosition;
        if (pointerState[whichHand] == pointerStates.NearCanvas || pointerState[whichHand] == pointerStates.TouchingCanvas || pointerState[whichHand] == pointerStates.TouchingElement)
        {
            EventCamera.transform.position = InputTracking.GetLocalPosition(VRNode.Head);
            IndexFingerPosition = LeapDataProvider.CurrentFrame.Hands[whichHand].Fingers[1].StabilizedTipPosition.ToVector3();
        } else {
            EventCamera.transform.position = Origin;
            IndexFingerPosition = LeapDataProvider.CurrentFrame.Hands[whichHand].Fingers[1].Bone(Bone.BoneType.TYPE_METACARPAL).Center.ToVector3();
        }

        //Draw Camera Origin
        if (DrawDebug)
            DebugSphereQueue.Enqueue(EventCamera.transform.position);

        //Set Camera Forward
        EventCamera.transform.forward = Direction;

        //Set its raycast direction and delta
        PointEvents[whichHand].position = Vector2.Lerp(PrevScreenPosition[whichHand], EventCamera.WorldToScreenPoint(IndexFingerPosition), 1.0f);//new Vector2(Screen.width / 2, Screen.height / 2);
        PointEvents[whichHand].delta = (PointEvents[whichHand].position - PrevScreenPosition[whichHand]) * -10f;
        PrevScreenPosition[whichHand] = PointEvents[whichHand].position;
        PointEvents[whichHand].scrollDelta = Vector2.zero;

        //Perform the raycast and see if we hit anything
        base.eventSystem.RaycastAll(PointEvents[whichHand], m_RaycastResultCache);

        //HACKY CODE TO GET IT TO WORK ON SCROLLRECTS
        //WHY DOESN'T FINDFIRSTRAYCAST DO THIS; WHY DOES IT WORK WITH MOUSE POINTERS
        //SO MANY UNANSWERED QUESTIONS
        if (OverrideScrollViewClicks) {
            PointEvents[whichHand].pointerCurrentRaycast = new RaycastResult();
            foreach (RaycastResult hit in m_RaycastResultCache) {
                if (hit.gameObject.GetComponent<Scrollbar>() != null) {
                    PointEvents[whichHand].pointerCurrentRaycast = hit;
                } else if (PointEvents[whichHand].pointerCurrentRaycast.gameObject == null && hit.gameObject.GetComponent<ScrollRect>() != null) {
                    PointEvents[whichHand].pointerCurrentRaycast = hit;
                }
            }
            if (PointEvents[whichHand].pointerCurrentRaycast.gameObject == null) {
                PointEvents[whichHand].pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
            }
        }else {
            PointEvents[whichHand].pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
        }

        //Check what we're hitting and set the PointerState to match
        OldState[whichHand] = pointerState[whichHand]; //Store old state for sound transitionary purposes
        if ((PointEvents[whichHand].pointerCurrentRaycast.gameObject != null)) {
            if (distanceOfIndexTipToPointer(whichHand) > ProjectiveToTactileTransitionDistance){
                    if (checkIfClickable(PointEvents[whichHand].pointerCurrentRaycast.gameObject)){
                        if (isTriggeringInteraction(whichHand)){
                            pointerState[whichHand] = pointerStates.PinchingToElement;
                        }else{
                            pointerState[whichHand] = pointerStates.OnElement;
                        }
                    }else{
                        if (isTriggeringInteraction(whichHand)){
                            pointerState[whichHand] = pointerStates.PinchingToCanvas;
                        }else{
                            pointerState[whichHand] = pointerStates.OnCanvas;
                        }
                    }
            }else if (isTriggeringInteraction(whichHand)) {
                if (checkIfClickable(PointEvents[whichHand].pointerCurrentRaycast.gameObject)){
                    pointerState[whichHand] = pointerStates.TouchingElement;
                }else{
                    pointerState[whichHand] = pointerStates.TouchingCanvas;
                }
            }else {
                pointerState[whichHand] = pointerStates.NearCanvas;
            }
        }else{
            pointerState[whichHand] = pointerStates.OffCanvas;
        }

        m_RaycastResultCache.Clear();

        //Transition Behaviors (sound and event triggers (color is in "Process" since it is lerped over multiple frames))
        if (OldState[whichHand] == pointerStates.OnCanvas)
        {
            if (pointerState[whichHand] == pointerStates.OnElement) {
                PointerSounds.PlayOneShot(HoverSound);
                onHover.Invoke();
            } else if (pointerState[whichHand] == pointerStates.PinchingToCanvas) {
                PointerSounds.PlayOneShot(MissedSound);
            }
        }
        else if (OldState[whichHand] == pointerStates.OnElement)
        {
            if (pointerState[whichHand] == pointerStates.OnCanvas) {
                PointerSounds.PlayOneShot(HoverSound);
            } else if (pointerState[whichHand] == pointerStates.PinchingToElement) {
                PointerSounds.PlayOneShot(TriggerSound);
                onClickDown.Invoke();
            }//ALSO PLAY HOVER SOUND IF ON DIFFERENT UI ELEMENT THAN LAST FRAME
        }
        else if (OldState[whichHand] == pointerStates.PinchingToElement)
        {
            if (pointerState[whichHand] == pointerStates.PinchingToCanvas) {
                //PointerSounds.PlayOneShot(HoverSound);
            } else if (pointerState[whichHand] == pointerStates.OnElement || pointerState[whichHand] == pointerStates.OnCanvas) {
                onClickUp.Invoke();
            }
        }
        else if (OldState[whichHand] == pointerStates.NearCanvas)
        {
            if (pointerState[whichHand] == pointerStates.TouchingElement){
                PointerSounds.PlayOneShot(TriggerSound);
                onClickDown.Invoke();
            }
            if (pointerState[whichHand] == pointerStates.TouchingCanvas){
                PointerSounds.PlayOneShot(MissedSound);
            }
        }
        else if (OldState[whichHand] == pointerStates.TouchingElement)
        {
            if (pointerState[whichHand] == pointerStates.NearCanvas){
                onClickUp.Invoke();
            }
        }
    }

    private bool checkIfClickable(GameObject gameObject) {
        return checkIfHasInteractionComponent(gameObject);
    }

    private bool checkIfHasInteractionComponent(GameObject gameObject) {
        return !gameObject.GetComponent<Canvas>();
    }

    // update the cursor location and whether it is enabled
    private void UpdatePointer(int index, PointerEventData pointData) {
        if (PointEvents[index].pointerCurrentRaycast.gameObject != null) {
            Pointers[index].gameObject.SetActive(true);

            if (pointData.pointerEnter != null) {
                RectTransform draggingPlane = pointData.pointerEnter.GetComponent<RectTransform>();
                Vector3 globalLookPos;
                if (RectTransformUtility.ScreenPointToWorldPointInRectangle(draggingPlane, pointData.position, pointData.enterEventCamera, out globalLookPos)) {
                    Pointers[index].position = globalLookPos;
                    float cursorAngle = Mathf.Rad2Deg*(Mathf.Atan2(pointData.delta.x, pointData.delta.y));
                    Pointers[index].rotation = draggingPlane.rotation * Quaternion.Euler(0f,0f, -cursorAngle);

                    // scale cursor based on distance to camera
                    float lookPointDistance = 1f;
                    if (Camera.main != null) {
                        lookPointDistance = (Pointers[index].position - Camera.main.transform.position).magnitude;
                    } else {
                        Debug.LogError("Tag a camera with 'Main Camera'");
                    }

                    float Pointerscale = lookPointDistance * NormalPointerScale;
                    if (Pointerscale < NormalPointerScale) {
                        Pointerscale = NormalPointerScale;
                    }

                    //Commented out Velocity Stretching because it looks funny when I change the
                    Pointers[index].localScale = Pointerscale * new Vector3(1f, 1f/* + pointData.delta.magnitude*0.5f*/, 1f);
                }
            }
        } else {
            Pointers[index].gameObject.SetActive(false);
        }
    }

    public bool isTriggeringInteraction(int whichHand) {
        if (pointerState[whichHand] == pointerStates.NearCanvas || pointerState[whichHand] == pointerStates.TouchingCanvas || pointerState[whichHand] == pointerStates.TouchingElement){
            return (distanceOfIndexTipToPointer(whichHand) < 0.03f);
        }else {
            Debug.Log(LeapDataProvider.CurrentFrame.Hands[whichHand].PinchDistance);
            return LeapDataProvider.CurrentFrame.Hands[whichHand].PinchDistance < 30f;
        }
    }

    public float distanceOfIndexTipToPointer(int whichHand) {
        //Get Base of Index Finger Position
        Vector3 IndexTipPosition = LeapDataProvider.CurrentFrame.Hands[whichHand].Fingers[1].StabilizedTipPosition.ToVector3();
        return -Pointers[whichHand].InverseTransformPoint(IndexTipPosition).z*Pointers[whichHand].localScale.z;
    }

    public void lerpPointerColor(int whichHand, Color color, float lerpalpha) {
        Color oldColor = Pointers[whichHand].GetComponent<UnityEngine.UI.Image>().material.color;
        if (color.r == 0f && color.g == 0f && color.b == 0f) {
            Pointers[whichHand].GetComponent<UnityEngine.UI.Image>().material.color = Color.Lerp(oldColor, new Color(oldColor.r, oldColor.g, oldColor.b, color.a), lerpalpha);
        } else if (color.a == 1f) {
            Pointers[whichHand].GetComponent<UnityEngine.UI.Image>().material.color = Color.Lerp(oldColor, new Color(color.r, color.g, color.b, oldColor.a), lerpalpha);
        } else {
            Pointers[whichHand].GetComponent<UnityEngine.UI.Image>().material.color = Color.Lerp(oldColor, color, lerpalpha);
        }
    }

    // clear the current selection
    public void ClearSelection() {
        if (base.eventSystem.currentSelectedGameObject) {
            base.eventSystem.SetSelectedGameObject(null);
        }
    }

    private void Select(GameObject go) {
        if (ExecuteEvents.GetEventHandler<ISelectHandler>(go)) {
            base.eventSystem.SetSelectedGameObject(go);
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
}