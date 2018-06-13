using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Gestures {

  public class GestureSequence : Gesture, IPoseGesture {

    #region Constants

    public const float DEFAULT_GESTURE_HOLD_DURATION = 1f;

    #endregion

    #region Inspector

    [System.Serializable]
    public struct GestureSequenceNode {

      public string name;

      public float waitDuration;

      [ImplementsInterface(typeof(IGesture))]
      [SerializeField]
      private MonoBehaviour _gesture;
      public IGesture gesture {
        get { return _gesture as IGesture; }
        set { _gesture = value as MonoBehaviour; }
      }

    }

    public GestureSequenceNode[] sequenceGraph;

    [Header("Debug")]

    public bool drawDebug = false;

    #endregion

    #region IGesture

    private bool _isActive = false;
    private bool _wasActivated = false;
    private bool _wasDeactivated = false;
    private bool _wasCancelled = false;
    private bool _wasFinished = false;

    public override bool isActive { get { return _isActive; } }

    public override bool wasActivated { get { return _wasActivated; } }

    public override bool wasDeactivated { get { return _wasDeactivated; } }

    public override bool wasFinished { get { return _wasFinished; } }

    public override bool wasCancelled { get { return _wasCancelled; } }

    #endregion

    #region IPoseGesture

    private Pose _latestGesturePose = Pose.identity;
    /// <summary>
    /// If there is an IPoseGesture somewhere in this GestureSequence, the sequence's
    /// pose will be the latest pose returned by the latest IPoseGesture in the sequence.
    /// Otherwise, the pose will simply be the identity pose.
    /// </summary>
    public Pose pose {
      get { return _latestGesturePose; }
    }

    #endregion

    #region Unity Events

    int _curSequenceIdx = 0;
    float _nextGestureTimer = 0f;

    void Update() {
      _wasActivated = false;
      _wasDeactivated = false;
      _wasCancelled = false;
      _wasFinished = false;

      // Update the sequence.
      if (sequenceGraph.Length != 0) {
        var curGestureNode = sequenceGraph[_curSequenceIdx];

        bool shouldActivate = false;
        bool shouldCancel = false;
        bool shouldFinish = false;

        // This gesture sequence as long as the current gesture in the sequence is
        // active.
        if (curGestureNode.gesture.isActive) {
          shouldActivate = true;

          // Check if the current gesture is an IPoseGesture, in which case, inherit its
          // pose.
          if (curGestureNode.gesture is IPoseGesture) {
            this._latestGesturePose = (curGestureNode.gesture as IPoseGesture).pose;
          }
        }
        
        if (curGestureNode.gesture.wasFinished) {

          // This gesture was completed, so next frame we'll look at the next
          // gesture in the sequence.
          _curSequenceIdx += 1;
          if (drawDebug) {
            DebugPing.Ping(Camera.main.transform.position
                           + Camera.main.transform.forward * 0.5f,
                           LeapColor.blue, 0.1f * _curSequenceIdx);
          }

          // Also reset the gesture timer.
          _nextGestureTimer = 0f;

          if (_curSequenceIdx == sequenceGraph.Length) {
            // We hit the end of the sequence successfully!
            shouldFinish = true;
            if (drawDebug) {
              DebugPing.Ping(Camera.main.transform.position
                             + Camera.main.transform.forward * 0.5f,
                             LeapColor.green, 0.11f * _curSequenceIdx);
              DebugPing.Ping(Camera.main.transform.position
                             + Camera.main.transform.forward * 0.5f,
                             LeapColor.yellow, 0.105f * _curSequenceIdx);
            }
          }
        }
        else {
          // Wait for the gesture to begin, or cancel this sequence if the
          // current gesture in the sequence was cancelled.
          if (curGestureNode.gesture.wasCancelled) {
            if (drawDebug) {
              DebugPing.Ping(Camera.main.transform.position
                             + Camera.main.transform.forward * 0.5f,
                             LeapColor.black, 0.11f * _curSequenceIdx);
              DebugPing.Ping(Camera.main.transform.position
                             + Camera.main.transform.forward * 0.5f,
                             LeapColor.red, 0.105f * _curSequenceIdx);
            }

            shouldCancel = true;
          }
          else if (!curGestureNode.gesture.isActive) {
            _nextGestureTimer += Time.deltaTime;

            if (_nextGestureTimer > curGestureNode.waitDuration) {
              if (drawDebug) {
                DebugPing.Ping(Camera.main.transform.position
                               + Camera.main.transform.forward * 0.5f,
                               LeapColor.black, 0.11f * _curSequenceIdx);
                DebugPing.Ping(Camera.main.transform.position
                               + Camera.main.transform.forward * 0.5f,
                               LeapColor.black, 0.105f * _curSequenceIdx);
              }

              shouldCancel = true;
            }
          }
        }

        // Set this gesture state appropriately.
        if (shouldActivate) {
          if (!_isActive) {
            _wasActivated = true;
          }
          _isActive = true;
        }
        else if (shouldCancel) {
          if (_isActive) {
            _isActive = false;
            _wasDeactivated = true;
            _wasCancelled = true;
            _wasFinished = false;
          }
        }
        else if (shouldFinish) {
          if (_isActive) {
            _isActive = false;
            _wasDeactivated = true;
            _wasCancelled = false;
            _wasFinished = true;
          }
        }

        if (_wasCancelled || _wasFinished) {
          _curSequenceIdx = 0;
        }
      }
    }

    #endregion

  }

}
