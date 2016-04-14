using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Leap;
using Leap.Unity;
using UnityEngine.VR;

public class LeapInputModule : BaseInputModule {
    public LeapProvider LeapDataProvider;
    public int NumberOfHands = 2;
    public float ProjectiveToTactileTransitionDistance = 0.06f;

    [HideInInspector]
    public bool GuiHit;
    [HideInInspector]
    public  bool ButtonUsed;

    public bool DrawDebug = false;

    private PointerEventData[] PointEvents;
    private Camera EventCamera;

    [Header(" [Pointer setup]")]
    public Sprite PointerSprite;
    public Material PointerMaterial;
    public float NormalPointerScale = 0.00025f; //In world space
    private RectTransform[] Pointers;

    private GameObject[] CurrentPoint;
    private GameObject[] CurrentPressed;
    private GameObject[] CurrentDragging;

    private Queue<Vector3> DebugSphereQueue;
    Canvas[] canvases;

    public float PinchingThreshold = 0.7f;
    private bool[] TriggeringInteraction;
    private Quaternion CurrentRotation;
    private Vector2[] PrevScreenPosition;

    private bool projectingFromIndexTip = false;

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
            image.material = PointerMaterial;
            image.raycastTarget = false;

            if (PointerSprite == null)
                Debug.LogError("Set PointerSprite on " + this.gameObject.name + " to the sprite you want to use as your pointer.", this.gameObject);

            Pointers[index] = pointer.GetComponent<RectTransform>();
        }

        CurrentPoint = new GameObject[NumberOfHands];
        CurrentPressed = new GameObject[NumberOfHands];
        CurrentDragging = new GameObject[NumberOfHands];

        TriggeringInteraction = new bool[NumberOfHands];
        PrevScreenPosition = new Vector2[NumberOfHands];
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

    // Process is called by UI system to process events
    public override void Process() {
        //InitializeControllers();
        //DebugSphereQueue.Enqueue(InputTracking.GetLocalPosition(VRNode.CenterEye));

        GuiHit = false;
        ButtonUsed = false;

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

            Color oldColor = Pointers[whichHand].GetComponent<UnityEngine.UI.Image>().material.color;
            if (distanceOfIndexTipToCursor(whichHand) < ProjectiveToTactileTransitionDistance +0.05f) {
                Pointers[whichHand].GetComponent<UnityEngine.UI.Image>().material.color = new Color(1f, 1f, 1f, Mathf.Lerp(oldColor.a, 0f, 0.1f));
                if (distanceOfIndexTipToCursor(whichHand) < ProjectiveToTactileTransitionDistance) {
                    projectingFromIndexTip = true;
                }
            } else {
                Pointers[whichHand].GetComponent<UnityEngine.UI.Image>().material.color = new Color(1f, 1f, 1f, Mathf.Lerp(oldColor.a, 1f, 0.1f));
                projectingFromIndexTip = false;
            }

            //Raycast from shoulder through index finger to the UI
            GetLookPointerEventData(whichHand, ProjectionOrigin, CurrentRotation * Vector3.forward);// (IndexBasePosition - ProjectionOrigin).normalized);

            if (PointEvents[whichHand].pointerCurrentRaycast.gameObject != null) {
                CurrentPoint[whichHand] = PointEvents[whichHand].pointerCurrentRaycast.gameObject;

                //Handle enter and exit events (highlight)
                base.HandlePointerExitAndEnter(PointEvents[whichHand], CurrentPoint[whichHand]);

                //Update cursor
                UpdatePointer(whichHand, PointEvents[whichHand]);

                if (!TriggeringInteraction[whichHand] && isTriggeringInteraction(whichHand)) {
                    ClearSelection();
                    TriggeringInteraction[whichHand] = true;

                    PointEvents[whichHand].pressPosition = PointEvents[whichHand].position;
                    PointEvents[whichHand].pointerPressRaycast = PointEvents[whichHand].pointerCurrentRaycast;
                    PointEvents[whichHand].pointerPress = null;

                    if (CurrentPoint[whichHand] != null) {
                        CurrentPressed[whichHand] = CurrentPoint[whichHand];

                        GameObject newPressed = ExecuteEvents.ExecuteHierarchy(CurrentPressed[whichHand], PointEvents[whichHand], ExecuteEvents.pointerDownHandler);

                        if (newPressed == null) {
                            // some UI elements might only have click handler and not pointer down handler
                            newPressed = ExecuteEvents.ExecuteHierarchy(CurrentPressed[whichHand], PointEvents[whichHand], ExecuteEvents.pointerClickHandler);
                            if (newPressed != null) {
                                CurrentPressed[whichHand] = newPressed;
                            }
                        } else {
                            CurrentPressed[whichHand] = newPressed;
                            // we want to do click on button down at same time, unlike regular mouse processing
                            // which does click when mouse goes up over same object it went down on
                            // reason to do this is head tracking might be jittery and this makes it easier to click buttons
                            ExecuteEvents.Execute(newPressed, PointEvents[whichHand], ExecuteEvents.pointerClickHandler);
                        }

                        if (newPressed != null) {
                            PointEvents[whichHand].pointerPress = newPressed;
                            CurrentPressed[whichHand] = newPressed;
                            Select(CurrentPressed[whichHand]);
                            ButtonUsed = true;
                        }

                        ExecuteEvents.Execute(CurrentPressed[whichHand], PointEvents[whichHand], ExecuteEvents.beginDragHandler);
                        PointEvents[whichHand].pointerDrag = CurrentPressed[whichHand];
                        CurrentDragging[whichHand] = CurrentPressed[whichHand];
                    }
                }

                if (TriggeringInteraction[whichHand] && !isTriggeringInteraction(whichHand)) {
                    TriggeringInteraction[whichHand] = false;

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
        }
    }

    private void GetLookPointerEventData(int whichHand, Vector3 Origin, Vector3 Direction) {
        //Construct PointerEvent
        if (PointEvents[whichHand] == null) {
            PointEvents[whichHand] = new PointerEventData(base.eventSystem);
        } else {
            PointEvents[whichHand].Reset();
        }

        //Set Camera Origin
        EventCamera.transform.position = Origin;
        if (DrawDebug)
            DebugSphereQueue.Enqueue(Origin);

        //Set Camera Forward
        EventCamera.transform.forward = Direction;

        //Get Base of Index Finger Position
        Vector3 IndexFingerPosition;
        if (projectingFromIndexTip) {
            IndexFingerPosition = LeapDataProvider.CurrentFrame.Hands[whichHand].Fingers[1].StabilizedTipPosition.ToVector3();
        } else {
            IndexFingerPosition = LeapDataProvider.CurrentFrame.Hands[whichHand].Fingers[1].Bone(Bone.BoneType.TYPE_METACARPAL).Center.ToVector3();
        }

        //Set its raycast direction and delta
        PointEvents[whichHand].position = Vector2.Lerp(PrevScreenPosition[whichHand], EventCamera.WorldToScreenPoint(IndexFingerPosition),0.5f);//new Vector2(Screen.width / 2, Screen.height / 2);
        PointEvents[whichHand].delta = (PointEvents[whichHand].position - PrevScreenPosition[whichHand])*-10f;
        PrevScreenPosition[whichHand] = PointEvents[whichHand].position;
        PointEvents[whichHand].scrollDelta = Vector2.zero;

        //Perform the raycast and see if we hit anything
        base.eventSystem.RaycastAll(PointEvents[whichHand], m_RaycastResultCache);
        PointEvents[whichHand].pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
        if (PointEvents[whichHand].pointerCurrentRaycast.gameObject != null) {
            GuiHit = true; //gets set to false at the beginning of the process event
        }

        m_RaycastResultCache.Clear();
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
                    Pointers[index].rotation = draggingPlane.rotation;

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

                    Pointers[index].localScale = Vector3.one * Pointerscale;
                }
            }
        } else {
            Pointers[index].gameObject.SetActive(false);
        }
    }

    public bool isTriggeringInteraction(int whichHand) {
        if (distanceOfIndexTipToCursor(whichHand) < ProjectiveToTactileTransitionDistance) {
            return distanceOfIndexTipToCursor(whichHand) < 0.01f;
        }else {
            return LeapDataProvider.CurrentFrame.Hands[whichHand].PinchStrength > PinchingThreshold;
        }
    }

    public float distanceOfIndexTipToCursor(int whichHand) {
        //Get Base of Index Finger Position
        Vector3 IndexTipPosition = LeapDataProvider.CurrentFrame.Hands[whichHand].Fingers[1].StabilizedTipPosition.ToVector3();
        return -Pointers[whichHand].InverseTransformPoint(IndexTipPosition).z*Pointers[whichHand].localScale.z;
    }

    public Vector3 getRectProjectiveOrigin(RectTransform Rect) {
        return Rect.localToWorldMatrix.MultiplyPoint3x4(Vector3.zero);
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