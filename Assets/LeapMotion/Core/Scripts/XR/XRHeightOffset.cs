/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
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

  [ExecuteInEditMode]
  public class XRHeightOffset : MonoBehaviour {

    #region Inspector

    [Header("Room-scale Height Offset")]

    [SerializeField]
    [OnEditorChange("roomScaleHeightOffset")]
    [Tooltip("This height offset allows you to place your Rig's base location at the "
           + "approximate head position of your player during edit-time, while still "
           + "providing correct cross-platform XR rig heights. If the tracking space "
           + "type is detected as RoomScale, the Rig will be shifted DOWN by this height "
           + "on Start, matching the expected floor height for, e.g., SteamVR, "
           + "while the rig remains unchanged for Android VR and Oculus single-camera "
           + "targets. "
           + "Use the magenta gizmo as a reference; the circles represent where your "
           + "floor will be in a Room-scale experience.")]
    [MinValue(0f)]
    private float _roomScaleHeightOffset = 1.6f; // average human height (or so)
    private float _lastKnownHeightOffset = 0f;
    public float roomScaleHeightOffset {
      get { return _roomScaleHeightOffset; }
      set {
        _roomScaleHeightOffset = value;
        this.transform.position += this.transform.up
                                   * (_roomScaleHeightOffset - _lastKnownHeightOffset);
        _lastKnownHeightOffset = value;
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

    [Tooltip("If enabled, then you can use the chosen keys to step the player's height "
           + "up and down at runtime. This method ")]
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
      _lastKnownHeightOffset = _roomScaleHeightOffset;
      var trackingSpaceType = UnityEngine.XR.XRDevice.GetTrackingSpaceType();
      if (trackingSpaceType == UnityEngine.XR.TrackingSpaceType.RoomScale) {
        this.transform.position -= this.transform.up * _roomScaleHeightOffset;
      }

      // Auto recenter
      if (Application.isPlaying) {
        var userPresence = UnityEngine.XR.XRDevice.userPresence;

        if (userPresence == UnityEngine.XR.UserPresenceState.Unsupported) {
          Debug.Log("[XRAutoRecenter] XR UserPresenceState unsupported; "
                  + "disabling autoRecenterOnUserPresence. (XR support is probably disabled.)");
          autoRecenterOnUserPresence = false;
        }
      }
    }

    private void Update() {
      if (Application.isPlaying) {
        var deviceIsPresent = UnityEngine.XR.XRDevice.isPresent;
        if (deviceIsPresent) {

          if (enableRuntimeAdjustment) {
            if (Input.GetKeyDown(stepUpKey)) {
              roomScaleHeightOffset += stepSize;
            }

            if (Input.GetKeyDown(stepDownKey)) {
              roomScaleHeightOffset -= stepSize;
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
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmos() {
      Gizmos.color = Color.Lerp(Color.magenta, Color.white, 0.3f).WithAlpha(0.5f);

      var totalHeight = roomScaleHeightOffset;
      var segmentsPerMeter = 32;
      var numSegments = totalHeight * segmentsPerMeter;
      var segmentLength = totalHeight / numSegments;
      var rigPos = this.transform.position;
      var down = this.transform.rotation * Vector3.down;
      
      if (Application.isPlaying
          && UnityEngine.XR.XRDevice.GetTrackingSpaceType()
             == UnityEngine.XR.TrackingSpaceType.RoomScale) {
        var roomScaleGizmoOffset = Vector3.up * totalHeight;

        rigPos += roomScaleGizmoOffset;
      }

      for (int i = 0; i < numSegments; i += 2) {
        var segStart = rigPos + down * segmentLength * i;
        var segEnd   = rigPos + down * segmentLength * (i + 1);

        Gizmos.DrawLine(segStart, segEnd);
      }

      var groundPos = rigPos + down * totalHeight;
      drawCircle(groundPos, down, 0.01f);
      Gizmos.color = Gizmos.color.WithAlpha(0.3f);
      drawCircle(groundPos, down, 0.10f);
      Gizmos.color = Gizmos.color.WithAlpha(0.2f);
      drawCircle(groundPos, down, 0.20f);
    }

    private void drawCircle(Vector3 position, Vector3 normal, float radius) {
      var r = normal.Perpendicular() * radius;
      var q = Quaternion.AngleAxis(360f / 32, normal);
      for (int i = 0; i < 32; i++) {
        var tempR = q * r;
        Gizmos.DrawLine(position + r, position + tempR);
        r = tempR;
      }
    }

    #endregion

  }

}
