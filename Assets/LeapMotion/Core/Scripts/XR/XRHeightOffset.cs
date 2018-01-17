/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System;
using System.Linq;
using Leap.Unity.Attributes;

namespace Leap.Unity {

  public class XRHeightOffset : MonoBehaviour {

    #region Inspector

    [Header("Height Offset When XR Space Tracking Type Is Stationary")]

    [SerializeField]
    [OnEditorChange("stationaryHeightOffset")]
    [Tooltip("This offset only used when the XR space tracking type is 'Stationary.' This "
         + "is the tracking type for Oculus setups with a single tracking camera and "
         + "for Android VR devices without position tracking. Two-camera Oculus setups "
         + "and Vive headsets, for example, do not require a height offset. They center "
         + "the room-scale floor on the Rig transform, and this script will not apply "
         + "a height offset when the RoomScale space-tracking type is detected.")]
    private float _stationaryHeightOffset = 1.6f; // average human height (or so)
    private float _lastKnownStationaryHeightOffset = 0f;
    public float stationaryHeightOffset {
      get { return _stationaryHeightOffset; }
      set {
        _stationaryHeightOffset = value;
        this.transform.position += this.transform.up
                                   * (_stationaryHeightOffset
                                      - _lastKnownStationaryHeightOffset);
        _lastKnownStationaryHeightOffset = value;
      }
    }

    #region Auto Recenter

    [Header("Auto Recenter")]

    [SerializeField]
    [Tooltip("If the detected XR device is present and supports userPresence, "
         + "checking this option will detect when the user puts on the device headset "
         + "and call InputTracking.Recenter.")]
    private bool autoRecenterOnUserPresence = true;

    private UnityEngine.XR.UserPresenceState _lastUserPresence;

    #endregion

    #region Runtime Height Adjustment

    [Header("Runtime Height Adjustment")]

    public bool enableRuntimeAdjustment = true;

    [DisableIf("enableRuntimeAdjustment", isEqualTo: false)]
    [Tooltip("Press this key on the keyboard to adjust the height offset up by stepSize.")]
    public KeyCode stepUpKey = KeyCode.UpArrow;

    [DisableIf("enableRuntimeAdjustment", isEqualTo: false)]
    [Tooltip("Press this key on the keyboard to adjust the height offset down by stepSize.")]
    public KeyCode stepDownKey = KeyCode.DownArrow;

    [DisableIf("enableRuntimeAdjustment", isEqualTo: false)]
    public float stepSize = 0.1f;

    #endregion

    #endregion

    #region Unity Events

    private void Start() {
      stationaryHeightOffset = _stationaryHeightOffset;

      // Auto recenter
      var userPresence = UnityEngine.XR.XRDevice.userPresence;

      if (userPresence == UnityEngine.XR.UserPresenceState.Unsupported) {
        Debug.Log("[XRAutoRecenter] XR UserPresenceState unsupported; "
                + "disabling autoRecenterOnUserPresence. (XR support is probably disabled.)");
        autoRecenterOnUserPresence = false;
      }
    }

    private void Update() {
      var deviceIsPresent = UnityEngine.XR.XRDevice.isPresent;
      if (deviceIsPresent) {

        if (enableRuntimeAdjustment) {
          if (Input.GetKeyDown(stepUpKey)) {
            stationaryHeightOffset += stepSize;
          }

          if (Input.GetKeyDown(stepDownKey)) {
            stationaryHeightOffset -= stepSize;
          }
        }

        var trackingSpaceType = UnityEngine.XR.XRDevice.GetTrackingSpaceType();
        if (trackingSpaceType == UnityEngine.XR.TrackingSpaceType.Stationary
            && autoRecenterOnUserPresence) {
          var userPresence = UnityEngine.XR.XRDevice.userPresence;

          if (_lastUserPresence != userPresence) {
            if (userPresence == UnityEngine.XR.UserPresenceState.Present) {
              UnityEngine.XR.InputTracking.Recenter();
            }

            _lastUserPresence = userPresence;
          }
        }
      }
    }

    #endregion

  }

}
