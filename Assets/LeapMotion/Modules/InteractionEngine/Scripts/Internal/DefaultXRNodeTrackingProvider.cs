/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_5
using UnityEngine.VR;
#else
using UnityEngine.XR;
#endif

namespace Leap.Unity.Interaction {
  
  /// <summary>
  /// Implements IVRControllerTrackingProvider using Unity.XR.InputTracking for XRNodes.
  /// This tracking should support all native XR controller integrations in Unity,
  /// including Oculus Touch and HTC Vive.
  /// </summary>
  public class DefaultXRNodeTrackingProvider : MonoBehaviour,
                                               IXRControllerTrackingProvider {

    private bool _isTrackingController = false;
    public bool isTracked { get { return _isTrackingController; } }

    private bool _isXRNodeSet = false;
    #if UNITY_5
    private VRNode _backingXRNode;
    public VRNode xrNode {
      get { return _backingXRNode; }
      set { _backingXRNode = value; _isXRNodeSet = true; }
    }
    #else
    private XRNode _backingXRNode;
    public XRNode xrNode {
      get { return _backingXRNode; }
      set { _backingXRNode = value; _isXRNodeSet = true; }
    }
    #endif

    public event Action<Vector3, Quaternion> OnTrackingDataUpdate = (position, rotation) => { };

    void FixedUpdate() {
      updateTrackingData();
    }

    void updateTrackingData() {
      if (_isXRNodeSet) {

        var position = InputTracking.GetLocalPosition(xrNode);
        var rotation = InputTracking.GetLocalRotation(xrNode);

        // Unfortunately, the only alternative to checking the controller's position and
        // rotation for whether or not it is tracked is to request an allocated string
        // array of all currently-connected joysticks, which would allocate garbage
        // every frame, so it's unusable.
        _isTrackingController = position != Vector3.zero && rotation != Quaternion.identity;

        Transform rigTransform = Camera.main.transform.parent;
        if (rigTransform != null) {
          position = rigTransform.TransformPoint(position);
          rotation = rigTransform.TransformRotation(rotation);
        }

        OnTrackingDataUpdate(position, rotation);
      }
    }

  }

}
