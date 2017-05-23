using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;

namespace Leap.Unity.Interaction {
  
  /// <summary>
  /// Implements IVRControllerTrackingProvider using Unity.VR.InputTracking for VRNodes.
  /// This tracking should support all native VR controller integrations in Unity,
  /// including Oculus Touch and HTC Vive.
  /// </summary>
  public class DefaultVRNodeTrackingProvider : MonoBehaviour,
                                               IVRControllerTrackingProvider {

    private bool _isVRNodeSet = false;
    private VRNode _backingVRNode;
    public VRNode vrNode {
      get { return _backingVRNode; }
      set { _backingVRNode = value; _isVRNodeSet = true; }
    }

    private bool _isTrackingController = false;

    public Action<Vector3, Quaternion> updateTrackingData = (position, rotation) => { };

    void FixedUpdate() {
      if (_isVRNodeSet) {
        var position = InputTracking.GetLocalPosition(vrNode);
        var rotation = InputTracking.GetLocalRotation(vrNode);
        
        // Unfortunately, the only alternative to checking the controller's position and
        // rotation for whether or not it is tracked is to request an allocated string
        // array of all currently-connected joysticks, which would allocate garbage
        // every frame, so it's unusable.
        _isTrackingController = position != Vector3.zero || rotation != Quaternion.identity;

        updateTrackingData(position, rotation);
      }
    }

    public bool GetIsTracked() {
      return _isTrackingController;
    }

    public void Subscribe(Action<Vector3, Quaternion> onTrackingDataUpdate) {
      updateTrackingData += onTrackingDataUpdate;
    }

    public void Unsubscribe(Action<Vector3, Quaternion> onTrackingDataUpdate) {
      updateTrackingData -= onTrackingDataUpdate;
    }
  }

}
