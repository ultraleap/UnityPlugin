using UnityEngine;

namespace Leap.Unity {

  public class GrabDetector : Detector {
    protected const float MM_TO_M = 0.001f;

    [SerializeField]
    protected IHandModel _handModel;

    [SerializeField]
    protected float _activateGrabAngle = 0.03f;

    [SerializeField]
    protected float _deactivateGrabAngle = 0.04f;

    protected int _lastUpdateFrame = -1;

    protected bool _isGrabbing = false;
    protected bool _didChange = false;

    protected float _lastGrabTime = 0.0f;
    protected float _lastUngrabTime = 0.0f;

    protected Vector3 _grabPos;
    protected Quaternion _grabRotation;

    protected virtual void OnValidate() {
      if (_handModel == null) {
        _handModel = GetComponentInParent<IHandModel>();
      }

      _activateGrabAngle = Mathf.Max(0, _activateGrabAngle);
      _deactivateGrabAngle = Mathf.Max(0, _deactivateGrabAngle);

      //Activate distance cannot be greater than deactivate distance
      if (_activateGrabAngle > _deactivateGrabAngle) {
        _deactivateGrabAngle = _activateGrabAngle;
      }
    }

    protected virtual void Awake() {
      if (GetComponent<IHandModel>() != null) {
        Debug.LogWarning("LeapGrabDetector should not be attached to the IHandModel's transform. It should be attached to its own transform.");
      }
      if (_handModel == null) {
        Debug.LogWarning("The HandModel field of LeapGrabDetector was unassigned and the detector has been disabled.");
        enabled = false;
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

      Hand hand = _handModel.GetLeapHand();

      if (hand == null || !_handModel.IsTracked) {
        changeGrabState(false);
        return;
      }

      float grabAngle = hand.GrabAngle;
      transform.rotation = hand.Basis.CalculateRotation();

      var fingers = hand.Fingers;
      transform.position = Vector3.zero;
      for (int i = 0; i < fingers.Count; i++) {
        Finger finger = fingers[i];
        if (finger.Type == Finger.FingerType.TYPE_INDEX ||
            finger.Type == Finger.FingerType.TYPE_THUMB) {
          transform.position += finger.Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();
        }
      }
      transform.position /= 2.0f;

      if (_isGrabbing) {
        if (grabAngle > _deactivateGrabAngle) {
          changeGrabState(false);
          return;
        }
      } else {
        if (grabAngle < _activateGrabAngle) {
          changeGrabState(true);
        }
      }

      if (_isGrabbing) {
        _grabPos = transform.position;
        _grabRotation = transform.rotation;
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
          Hand hand = _handModel.GetLeapHand();
          Finger thumb = hand.Fingers[0];
          Finger index = hand.Fingers[1];
          centerColor = Color.red;
          centerPosition = ((thumb.Bone(Bone.BoneType.TYPE_DISTAL).NextJoint + index.Bone(Bone.BoneType.TYPE_DISTAL).NextJoint)/2).ToVector3();
          circleRotation = hand.Basis.CalculateRotation();
        }
        Vector3 axis;
        float angle;
        circleRotation.ToAngleAxis(out angle, out axis);
        Utils.DrawCircle(centerPosition, axis, _activateGrabAngle / 2, centerColor);
        Utils.DrawCircle(centerPosition, axis, _deactivateGrabAngle / 2, Color.blue);
      }
    }
    #endif

  }
}
