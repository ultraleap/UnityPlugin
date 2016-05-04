using UnityEngine;
using System.Collections;
using Leap;
using Leap.Unity;

namespace Leap.Unity.DetectionUtilities {

  /** Detects when the hand has entered a rectangular zone. */
  public class ZoneDetector : BinaryDetector {
    public Vector3 zones = Vector3.one;
    public Vector3 TargetZone = Vector3.zero;
    public Vector3 ActiveZone{ get; private set; }

    private Device _device = null;
    private Controller _controller;
    private float _vAov = 140; //degrees
    private float _hAov = 140; //degrees
    private float _range = .5f; //meters

    //Get the interaction volume, divide into zones and report which zone the hand is in.

    void Start(){
      if(_controller == null){
        _controller = new Controller();
      }
      _controller.Device += OnDevice;
    }
    void OnDevice(object sender, DeviceEventArgs deviceArgs){
      _device = deviceArgs.Device;
      if(_device.IsStreaming){
        _vAov = _device.VerticalViewAngle;
        _hAov = _device.HorizontalViewAngle;
        _range = _device.Range * transform.lossyScale.z;
        StartCoroutine(zoneWatcher());
      }
    }

    void OnEnable () {
      if(_device != null){
        StopCoroutine(zoneWatcher());
        StartCoroutine(zoneWatcher());
      }
    }
  
    void OnDisable () {
      StopCoroutine(zoneWatcher());
    }

    IEnumerator zoneWatcher() {
        Vector3 position;
        float depth;
        float width;
        float height;

      while(true){
        position = transform.localPosition;
        depth = _range;
        width = 2 * Mathf.Asin(_hAov * Constants.RAD_TO_DEG/2);
        height = 2 * Mathf.Asin(_vAov * Constants.RAD_TO_DEG/2);
        Vector3 currentZone = new Vector3(Mathf.Floor(position.x/width/zones.x), 
                                          Mathf.Floor(position.y/height/zones.y), 
                                          Mathf.Floor(position.z/depth/zones.z));
        if(currentZone != ActiveZone){
          //Dispatch zone change event
          ActiveZone = currentZone;
        }
        if(ActiveZone == TargetZone){
          Activate();
        } else if(IsActive){
          Deactivate();
        }
        yield return new WaitForSeconds(Period);
      }
    }

    #if UNITY_EDITOR
    void OnDrawGizmos(){
      LeapHandController handController = GameObject.FindObjectOfType<LeapHandController>();
      int count = 0;
      if(handController != null){
        for(float ya = -_vAov/2; ya <= _vAov/2; ya += _vAov/zones.y){
          for(float xa = -_hAov/2; xa <= _hAov/2; xa += _hAov/zones.x){
            float x = Mathf.Asin(xa * Constants.RAD_TO_DEG);
            float y = Mathf.Asin(ya * Constants.RAD_TO_DEG/2);
            Vector3 end = new Vector3(x, y, _range);
            Debug.DrawLine(handController.transform.position, end, Color.cyan);
            if(count > 3) break;
          }
          if(count > 3) break;
        }
      }
    }
    #endif

  }
}
