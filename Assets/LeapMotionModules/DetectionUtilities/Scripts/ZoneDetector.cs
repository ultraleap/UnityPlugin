using UnityEngine;
using System.Collections;
using Leap;
using Leap.Unity;

namespace Leap.Unity.DetectionUtilities {

  /** Detects when the hand has entered a rectangular zone. */
  public class ZoneDetector : BinaryDetector {
    public IHandModel HandModel = null;
    public LeapHandController HandController = null;
    public Vector3 zones = Vector3.one;
    public Vector3 TargetZone = Vector3.zero;
    public Vector3 ActiveZone{ get; private set; }
    public bool ZAligned = false;

    private IEnumerator zoneWatcherCoroutine;
    private Controller _controller;
    private float _vAov = 2.007129f; //radians
    private float _hAov = 2.303835f; //radians
    private float _range = 470f / 1000f; //meters
    private float _vTan = 1.569686f; //tan(_vAov/2)
    private float _hTan = 2.246038f; //tan(_hAov/2)

    //Get the interaction volume, divide into zones and report which zone the hand is in.

    void Awake(){
      zoneWatcherCoroutine = zoneWatcher();
      if(HandModel == null){
        HandModel = gameObject.GetComponentInParent<IHandModel>();
      }
      if(HandController == null){
        HandController = GameObject.FindObjectOfType(typeof(LeapHandController)) as LeapHandController;
      }
    }

    void Start(){
      if(_controller == null){
        _controller = new Controller();
      }
      Device device = _controller.Devices.ActiveDevice;
     // if(device.IsStreaming){
        Debug.Log("Got streaming device");
        copyDeviceData(device);
      //}
      _controller.Device += OnDevice;
    }
    void OnDevice(object sender, DeviceEventArgs deviceArgs){
      Debug.Log("Got device event");
      if(deviceArgs.Device.IsStreaming){
        copyDeviceData(deviceArgs.Device);
        StopCoroutine(zoneWatcherCoroutine);
        StartCoroutine(zoneWatcherCoroutine);
      }
    }

    void copyDeviceData(Device device){
        _vAov = device.VerticalViewAngle;
        _hAov = device.HorizontalViewAngle;
        _range = device.Range * .001f;
        _vTan = Mathf.Tan(_vAov/2);
        _hTan = Mathf.Tan(_hAov/2);
    }

    void OnEnable () {
        StopCoroutine(zoneWatcherCoroutine);
        StartCoroutine(zoneWatcherCoroutine);
    }
  
    void OnDisable () {
      StopCoroutine(zoneWatcherCoroutine);
    }

    private Vector3 inZone = Vector3.zero;
    IEnumerator zoneWatcher() {
        Vector3 position;
        float depth, width, height, xZone, yZone, zZone;
        Vector3 currentZone;
      while(true){
        if(HandModel != null && HandModel.IsTracked && HandController != null){
          position = HandModel.GetLeapHand().PalmPosition.ToVector3();
          Debug.Log("Measured pos: " + position);
          if(ZAligned){
//            width =  position.z * _hTan;
//            height = position.z  * _vTan;
//            depth =  _range;
//            currentZone = new Vector3(((position.x) * zones.x/width), 
//                                      ((position.y - HandController.transform.position.y) * height/zones.y), 
//                                      ((position.z - HandController.transform.position.z) * depth/zones.z));
            depth = _range;
            zZone = Mathf.Ceil((position.z - HandController.transform.position.z) * zones.z/depth);
            width = 2 * zZone * _range/zones.z * _hTan;
            xZone = Mathf.Ceil((position.x - HandController.transform.position.x + width/2) * zones.x/ width);
            height = 2 * zZone * _range/zones.z * _vTan;
            yZone = Mathf.Ceil((position.y - HandController.transform.position.y + height/2) * zones.y/height);
            currentZone = new Vector3(xZone, yZone, zZone);
          } else { 
            height = _range;
            yZone = Mathf.Ceil((position.y - HandController.transform.position.y) * zones.y/height);
            yZone = yZone < 1 ? 1.0f : yZone;
            width = 2 * yZone * _range/zones.y * _hTan;
            xZone = Mathf.Ceil((position.x - HandController.transform.position.x + width/2) * zones.x/ width);
            depth = 2 * yZone * _range/zones.y * _vTan;
            zZone = Mathf.Ceil((position.z - HandController.transform.position.z + depth/2) * zones.z/depth);
            currentZone = new Vector3(xZone, yZone, zZone);
          }
          inZone = currentZone;
          if(currentZone != ActiveZone){
            //Dispatch zone change event
            ActiveZone = currentZone;
          }
          if(ActiveZone == TargetZone){
            Activate();
          } else if(IsActive){
            Deactivate();
          }
        } else if(IsActive){
          Deactivate(); //deactivate if hand is not tracked
        }
        yield return new WaitForSeconds(Period);
      }
    }

    #if UNITY_EDITOR
    GameObject go;
    void OnDrawGizmos(){
      if (ShowGizmos) {
        if (go == null) go = new GameObject();
        Color gridColor = Color.gray;
        float tx, ty, tz, tw = 0, ts = 0;
        float ax, ay, az, aw = 0, ah = 0;
        int layers = 1;
        int stacks = 1;
        if (HandController != null) {
          go.transform.localPosition = HandController.transform.localPosition;
          go.transform.localRotation = HandController.transform.localRotation;
          go.transform.localScale = HandController.transform.localScale;
          if (ZAligned) {
            layers = (int)zones.z;
            stacks = (int)zones.y;
            tx = TargetZone.x;
            ty = TargetZone.y;
            tz = TargetZone.z;
            ax = ActiveZone.x;
            ay = ActiveZone.y;
            az = ActiveZone.z;
          } else {
            layers = (int)zones.y;
            stacks = (int)zones.z;
            go.transform.Rotate(Vector3.right, -90);
            tx = TargetZone.x;
            ty = zones.z - TargetZone.z + 1; //mirror across axis
            tz = TargetZone.y;
            ax = ActiveZone.x;
            ay = zones.z - ActiveZone.z + 1; //mirror across axis
            az = ActiveZone.y;
          }
          for (int l = 1; l <= layers; l++) {
            float width = 2 * l * _range / layers * _vTan;
            float height = 2 * l * _range / layers * _hTan;
            for (int c = 1; c <= zones.x; c++) {
              for (int s = 1; s <= stacks; s++) {
                if (tx == c && ty == s && tz == l) { //is target zone
                  tw = width;
                  ts = height;
                } else if (ax == c && ay == s && az == l) { // is active zone
                  aw = width;
                  ah = height;
                } else {
                  drawZone(c, s, l, gridColor, width, height, stacks, layers);
                }
              }
            }
          }
          //Draw Target Zone last
          Color targetColor;
          if (IsActive) {
            targetColor = Color.green;
          } else {
            targetColor = Color.red;
            drawZone(ax, ay, az, Color.blue, aw, ah, stacks, layers);
          }
          drawZone(tx, ty, tz, targetColor, tw, ts, stacks, layers);
        }
      }
    }

    void drawZone(float c, float s, float l, Color gridColor, float width, float height, int stacks, int layers){
      Vector3 ptA, ptB, ptC, ptD, ptE, ptF, ptG, ptH;

      ptA = go.transform.TransformPoint(new Vector3(      c * width/zones.x - width/2,       s * height/stacks - height/2, l * _range/layers));
      ptB = go.transform.TransformPoint(new Vector3((c - 1) * width/zones.x - width/2,       s * height/stacks - height/2, l * _range/layers));
      ptC = go.transform.TransformPoint(new Vector3(      c * width/zones.x - width/2, (s - 1) * height/stacks - height/2, l * _range/layers));
      ptD = go.transform.TransformPoint(new Vector3((c - 1) * width/zones.x - width/2, (s - 1) * height/stacks - height/2, l * _range/layers));
      ptE = go.transform.TransformPoint(new Vector3(      c * width/zones.x - width/2,       s * height/stacks - height/2, (l -1) * _range/layers));
      ptF = go.transform.TransformPoint(new Vector3((c - 1) * width/zones.x - width/2,       s * height/stacks - height/2, (l -1) * _range/layers));
      ptG = go.transform.TransformPoint(new Vector3(      c * width/zones.x - width/2, (s - 1) * height/stacks - height/2, (l -1) * _range/layers));
      ptH = go.transform.TransformPoint(new Vector3((c - 1) * width/zones.x - width/2, (s - 1) * height/stacks - height/2, (l -1) * _range/layers));
      
      Debug.DrawLine(ptA, ptB, gridColor, 0, true);
      Debug.DrawLine(ptB, ptD, gridColor, 0, true);
      Debug.DrawLine(ptD, ptC, gridColor, 0, true);
      Debug.DrawLine(ptC, ptA, gridColor, 0, true);

      Debug.DrawLine(ptE, ptF, gridColor, 0, true);
      Debug.DrawLine(ptF, ptH, gridColor, 0, true);
      Debug.DrawLine(ptH, ptG, gridColor, 0, true);
      Debug.DrawLine(ptG, ptE, gridColor, 0, true);

      Debug.DrawLine(ptA, ptE, gridColor, 0, true);
      Debug.DrawLine(ptB, ptF, gridColor, 0, true);
      Debug.DrawLine(ptC, ptG, gridColor, 0, true);
      Debug.DrawLine(ptD, ptH, gridColor, 0, true);
    }
    #endif

  }
}
