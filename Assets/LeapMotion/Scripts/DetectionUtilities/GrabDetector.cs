using UnityEngine;

namespace Leap.Unity {

  public class GrabDetector : Detector {

    public IHandModel HandModel;
    [Range(0, 180)]
    public float ActivateGrabAngle = 110f; //degrees
    [Range(0, 180)]
    public float DeactivateGrabAngle = 90f; //degrees
    public float CurrentAngle;
    public float CurrentStrength;

    public Vector3 GrabCenter { get; private set; }
    public Quaternion GrabRotation { get; private set; }
    public Vector3 GrabForward = Vector3.forward;
    public Vector3 GrabNormal = Vector3.up;
    public float GrabSize;

    protected int _lastUpdateFrame = -1;

    protected bool _isGrabbing = false;
    protected bool _didChange = false;

    protected float _lastGrabTime = 0.0f;
    protected float _lastUngrabTime = 0.0f;

    protected Vector3 _grabPos;
    protected Quaternion _grabRotation;

    protected virtual void OnValidate() {
      ActivateGrabAngle = Mathf.Max(0, ActivateGrabAngle);
      DeactivateGrabAngle = Mathf.Max(0, DeactivateGrabAngle);

      //Activate angle cannot be less than deactivate angle
      if (DeactivateGrabAngle > ActivateGrabAngle) {
        DeactivateGrabAngle = ActivateGrabAngle;
      }
    }

    protected virtual void Awake() {
      if (HandModel == null) {
        HandModel = gameObject.GetComponentInParent<IHandModel>();
      }
    }

    protected virtual void Update() {
      //We ensure the data is up to date at all times because
      //there are some values (like LastPinchTime) that cannot
      //be updated on demand
      ensureGrabInfoUpToDate();
    }

    /// <summary>
    /// Returns whether or not the dectector is currently detecting a pinch.
    /// </summary>
    public bool IsGrabbing {
      get {
        ensureGrabInfoUpToDate();
        return _isGrabbing;
      }
    }

    /// <summary>
    /// Returns whether or not the value of IsPinching is different than the value reported during
    /// the previous frame.
    /// </summary>
    public bool DidChangeFromLastFrame {
      get {
        ensureGrabInfoUpToDate();
        return _didChange;
      }
    }

    /// <summary>
    /// Returns whether or not the value of IsPinching changed to true between this frame and the previous.
    /// </summary>
    public bool DidStartGrab {
      get {
        ensureGrabInfoUpToDate();
        return DidChangeFromLastFrame && IsGrabbing;
      }
    }

    /// <summary>
    /// Returns whether or not the value of IsPinching changed to false between this frame and the previous.
    /// </summary>
    public bool DidEndGrab {
      get {
        ensureGrabInfoUpToDate();
        return DidChangeFromLastFrame && !IsGrabbing;
      }
    }

    /// <summary>
    /// Returns the value of Time.time during the most recent pinch event.
    /// </summary>
    public float LastGrabTime {
      get {
        ensureGrabInfoUpToDate();
        return _lastGrabTime;
      }
    }

    /// <summary>
    /// Returns the value of Time.time during the most recent unpinch event.
    /// </summary>
    public float LastReleaseTime {
      get {
        ensureGrabInfoUpToDate();
        return _lastUngrabTime;
      }
    }

    /// <summary>
    /// Returns the position value of the detected pinch.  If a pinch is not currently being
    /// detected, returns the most recent pinch position value.
    /// </summary>
    public Vector3 Position {
      get {
        ensureGrabInfoUpToDate();
        return _grabPos;
      }
    }

    /// <summary>
    /// Returns the rotation value of the detected pinch.  If a pinch is not currently being
    /// detected, returns the most recent pinch rotation value.
    /// </summary>
    public Quaternion Rotation {
      get {
        ensureGrabInfoUpToDate();
        return _grabRotation;
      }
    }

    protected virtual void ensureGrabInfoUpToDate() {
      if (Time.frameCount == _lastUpdateFrame || HandModel == null) {
        return;
      }
      _lastUpdateFrame = Time.frameCount;

      _didChange = false;

      Hand hand = HandModel.GetLeapHand();

      if (hand == null || !HandModel.IsTracked) {
        changeGrabState(false);
        return;
      }

      float grabAngle = hand.GrabAngle * Constants.RAD_TO_DEG;
            CurrentAngle = grabAngle;
            CurrentStrength = hand.GrabStrength;

      var fingers = hand.Fingers;
      GrabCenter = Vector3.zero;
      GrabSize = 0;
      for (int i = 0; i < fingers.Count; i++) {
        Finger finger = fingers[i];
        GrabCenter += finger.TipPosition.ToVector3();
        GrabSize += fingers[0].TipPosition.DistanceTo(finger.TipPosition);
      }
      GrabCenter /= 5.0f;
      GrabSize /= 4;

      GrabForward = hand.WristPosition.ToVector3() - fingers[2].TipPosition.ToVector3();
      Vector3 thumbToPinky = fingers[0].TipPosition.ToVector3() - fingers[4].TipPosition.ToVector3();
      GrabNormal = Vector3.Cross(GrabForward, thumbToPinky).normalized;
      GrabRotation = Quaternion.LookRotation(GrabForward, GrabNormal);

      if (_isGrabbing) {
        if (grabAngle < DeactivateGrabAngle) {
          changeGrabState(false);
          return;
        }
      } else {
        if (grabAngle > ActivateGrabAngle) {
          changeGrabState(true);
        }
      }

      if (_isGrabbing) {
        _grabPos = GrabCenter;
        _grabRotation = GrabRotation;
      }
    }

    protected virtual void changeGrabState(bool shouldBeGrabbing) {
      if (_isGrabbing != shouldBeGrabbing) {
        _isGrabbing = shouldBeGrabbing;

        if (_isGrabbing) {
          _lastGrabTime = Time.time;
          Activate();
        } else {
          _lastUngrabTime = Time.time;
          Deactivate();
        }

        _didChange = true;
      }
    }

    #if UNITY_EDITOR
    void OnDrawGizmos () {
      if (ShowGizmos) {
        ensureGrabInfoUpToDate();
        Color centerColor;
        Vector3 centerPosition = GrabCenter;
        Quaternion circleRotation = GrabRotation;
        if (IsGrabbing) {
          centerColor = Color.green;
        } else {
          centerColor = Color.red;
        }
        Vector3 axis;
        float angle;
        circleRotation.ToAngleAxis(out angle, out axis);
        Utils.DrawCircle(centerPosition, axis, GrabSize / 2, centerColor);
        Debug.DrawLine(centerPosition, centerPosition + GrabForward * GrabSize / 2, Color.grey);
        Debug.DrawLine(centerPosition, centerPosition + GrabNormal * GrabSize / 2, Color.grey);
      }
    }
    #endif

  }
}
