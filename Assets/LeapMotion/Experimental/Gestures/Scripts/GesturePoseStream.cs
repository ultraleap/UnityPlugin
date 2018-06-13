using System;
using Leap.Unity.Attributes;
using UnityEngine;

namespace Leap.Unity.Gestures {

  public class GesturePoseStream : MonoBehaviour,
                                   IStream<Pose> {

    public enum GestureStreamMode { ActiveOnly, EligibleOnly, Always }
    [Tooltip("How to stream poses from the Pose Gesture.\n"
           + "ActiveOnly: Only stream while the Pose Gesture is active.\n"
           + "EligibleOnly: Only stream while the Pose Gesture is active or eligible.\n"
           + "Always: Always stream as long as there is a valid pose gesture reference.")]
    public GestureStreamMode streamMode = GestureStreamMode.ActiveOnly;

    [SerializeField, ImplementsInterface(typeof(IPoseGesture))]
    private MonoBehaviour _poseGesture;
    public IPoseGesture poseGesture {
      get { return _poseGesture as IPoseGesture; }
    }

    public event Action OnOpen;
    public event Action<Pose> OnSend;
    public event Action OnClose;

    private bool _streamOpen = false;

    private void Update() {
      bool shouldStream;
      if (poseGesture == null) {
        shouldStream = false;
      }
      else {
        switch (streamMode) {
          case GestureStreamMode.ActiveOnly:
            shouldStream = poseGesture.isActive;
            break;
          case GestureStreamMode.EligibleOnly:
            shouldStream = poseGesture.isActive || poseGesture.isEligible;
            break;
          case GestureStreamMode.Always:
            shouldStream = true;
            break;
          default:
            shouldStream = false;
            break;
        }
      }

      if (!shouldStream && _streamOpen) {
        OnClose();
      }
      if (shouldStream && !_streamOpen) {
        OnOpen();
      }
      if (shouldStream) {
        OnSend(poseGesture.pose);
      }
    }

    private void OnDisable() {
      if (_streamOpen) {
        OnClose();
      }
    }

  }
  
}
