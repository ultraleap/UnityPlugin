using UnityEngine;
using System.Collections;
using Leap;
using Leap.Unity;
namespace Leap.Unity.DetectionUtilities{

  //A zone detector that surounds the leap Motion Detector in concentric rings
  public class ConcentricZoneDetector : Detector {
    public int Layers = 3; //Vertical divisions
    public int Rings = 3; //Horizontal (distance-based) divisions
    public int Wedges = 12;  //Angular divisions
  
      public IHandModel HandModel = null;
      public LeapHandController HandController = null;
  
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
  
    IEnumerator zoneWatcher(){yield return null;}

    #if UNITY_EDITOR
    GameObject go;
    void OnDrawGizmos(){
      
    }
    #endif
  }
}