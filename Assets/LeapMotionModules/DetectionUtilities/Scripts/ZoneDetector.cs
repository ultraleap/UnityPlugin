using UnityEngine;
using System.Collections;
using Leap;

namespace Leap.Unity.DetectionUtilities {

  /** Detects when the hand has entered a rectangular zone. */
  public class ZoneDetector : Detector {
    public Vector3 zones = Vector3.one;
    public Vector3 TargetZone = Vector3.zero;

    private Device _device = null;
    private Controller _controller;
    private float[] _xDivisions;
    private float[] _yDivisions;
    private float[] _zDivisions;

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
        int yDivs = (int)(_device.Range/zones.y + 0.5f);
      }
    }
  }
}
