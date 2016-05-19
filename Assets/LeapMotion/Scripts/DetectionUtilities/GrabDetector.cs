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
      if (Time.frameCount == _lastUpdateFrame) {
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
      for (int i = 0; i < fingers.Count; i++) {
        Finger finger = fingers[i];
        GrabCenter += finger.TipPosition.ToVector3();
      }
      GrabCenter /= 5.0f;
      Vector3 wristToMiddle = hand.WristPosition.ToVector3() - fingers[2].TipPosition.ToVector3();
      Vector3 thumbToPinky = fingers[0].TipPosition.ToVector3() - fingers[4].TipPosition.ToVector3();
      Vector3 graspNormal = Vector3.Cross(wristToMiddle, thumbToPinky).normalized;
      GrabRotation = Quaternion.LookRotation(wristToMiddle, graspNormal);

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
        Color centerColor;
        Vector3 centerPosition;
        Quaternion circleRotation;
        if (IsGrabbing) {
          centerColor = Color.green;
          centerPosition = Position;
          circleRotation = Rotation;
        } else {
          Hand hand = HandModel.GetLeapHand();
          Finger thumb = hand.Fingers[0];
          Finger index = hand.Fingers[1];
          centerColor = Color.red;
          centerPosition = ((thumb.Bone(Bone.BoneType.TYPE_DISTAL).NextJoint + index.Bone(Bone.BoneType.TYPE_DISTAL).NextJoint)/2).ToVector3();
          circleRotation = hand.Basis.CalculateRotation();
        }
        Vector3 axis;
        float angle;
        circleRotation.ToAngleAxis(out angle, out axis);
        Utils.DrawCircle(centerPosition, axis, ActivateGrabAngle / 2, centerColor);
        Utils.DrawCircle(centerPosition, axis, DeactivateGrabAngle / 2, Color.blue);
      }
    }
    #endif

  }
}
