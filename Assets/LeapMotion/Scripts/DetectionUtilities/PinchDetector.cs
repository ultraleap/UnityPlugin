using UnityEngine;

namespace Leap.Unity {

  /// <summary>
  /// A basic utility class to aid in creating pinch based actions.  Once linked with an IHandModel, it can
  /// be used to detect pinch gestures that the hand makes.
  /// </summary>
  public class PinchDetector : Detector {
    protected const float MM_TO_M = 0.001f;

    [SerializeField]
    protected IHandModel _handModel;

    [SerializeField]
    protected float _activatePinchDist = 0.03f;

    [SerializeField]
    protected float _deactivatePinchDist = 0.04f;

    protected int _lastUpdateFrame = -1;

    protected bool _isPinching = false;
    protected bool _didChange = false;

    protected float _lastPinchTime = 0.0f;
    protected float _lastUnpinchTime = 0.0f;

    protected Vector3 _pinchPos;
    protected Quaternion _pinchRotation;

    protected virtual void OnValidate() {
      if (_handModel == null) {
        _handModel = GetComponentInParent<IHandModel>();
      }

      _activatePinchDist = Mathf.Max(0, _activatePinchDist);
      _deactivatePinchDist = Mathf.Max(0, _deactivatePinchDist);

      //Activate distance cannot be greater than deactivate distance
      if (_activatePinchDist > _deactivatePinchDist) {
        _deactivatePinchDist = _activatePinchDist;
      }
    }

    protected virtual void Awake() {
      if (GetComponent<IHandModel>() != null) {
        Debug.LogWarning("LeapPinchDetector should not be attached to the IHandModel's transform. It should be attached to its own transform.");
      }
      if (_handModel == null) {
        Debug.LogWarning("The HandModel field of LeapPinchDetector was unassigned and the detector has been disabled.");
        enabled = false;
      }
    }

    protected virtual void Update() {
      //We ensure the data is up to date at all times because
      //there are some values (like LastPinchTime) that cannot
      //be updated on demand
      ensurePinchInfoUpToDate();
    }

    /// <summary>
    /// Returns whether or not the dectector is currently detecting a pinch.
    /// </summary>
    public bool IsPinching {
      get {
        ensurePinchInfoUpToDate();
        return _isPinching;
      }
    }

    /// <summary>
    /// Returns whether or not the value of IsPinching is different than the value reported during
    /// the previous frame.
    /// </summary>
    public bool DidChangeFromLastFrame {
      get {
        ensurePinchInfoUpToDate();
        return _didChange;
      }
    }

    /// <summary>
    /// Returns whether or not the value of IsPinching changed to true between this frame and the previous.
    /// </summary>
    public bool DidStartPinch {
      get {
        ensurePinchInfoUpToDate();
        return DidChangeFromLastFrame && IsPinching;
      }
    }

    /// <summary>
    /// Returns whether or not the value of IsPinching changed to false between this frame and the previous.
    /// </summary>
    public bool DidEndPinch {
      get {
        ensurePinchInfoUpToDate();
        return DidChangeFromLastFrame && !IsPinching;
      }
    }

    /// <summary>
    /// Returns the value of Time.time during the most recent pinch event.
    /// </summary>
    public float LastPinchTime {
      get {
        ensurePinchInfoUpToDate();
        return _lastPinchTime;
      }
    }

    /// <summary>
    /// Returns the value of Time.time during the most recent unpinch event.
    /// </summary>
    public float LastUnpinchTime {
      get {
        ensurePinchInfoUpToDate();
        return _lastUnpinchTime;
      }
    }

    /// <summary>
    /// Returns the position value of the detected pinch.  If a pinch is not currently being
    /// detected, returns the most recent pinch position value.
    /// </summary>
    public Vector3 Position {
      get {
        ensurePinchInfoUpToDate();
        return _pinchPos;
      }
    }

    /// <summary>
    /// Returns the rotation value of the detected pinch.  If a pinch is not currently being
    /// detected, returns the most recent pinch rotation value.
    /// </summary>
    public Quaternion Rotation {
      get {
        ensurePinchInfoUpToDate();
        return _pinchRotation;
      }
    }

    protected virtual void ensurePinchInfoUpToDate() {
      if (Time.frameCount == _lastUpdateFrame) {
        return;
      }
      _lastUpdateFrame = Time.frameCount;

      _didChange = false;

      Hand hand = _handModel.GetLeapHand();

      if (hand == null || !_handModel.IsTracked) {
        changePinchState(false);
        return;
      }

      float pinchDistance = hand.PinchDistance * MM_TO_M;
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

      if (_isPinching) {
        if (pinchDistance > _deactivatePinchDist) {
          changePinchState(false);
          return;
        }
      } else {
        if (pinchDistance < _activatePinchDist) {
          changePinchState(true);
        }
      }

      if (_isPinching) {
        _pinchPos = transform.position;
        _pinchRotation = transform.rotation;
      }
    }

    protected virtual void changePinchState(bool shouldBePinching) {
      if (_isPinching != shouldBePinching) {
        _isPinching = shouldBePinching;

        if (_isPinching) {
          _lastPinchTime = Time.time;
          Activate();
        } else {
          _lastUnpinchTime = Time.time;
          Deactivate();
        }

        _didChange = true;
      }
    }

    #if UNITY_EDITOR
    void OnDrawGizmos () {
      if (ShowGizmos && _handModel != null) {
        Color centerColor = Color.clear;
        Vector3 centerPosition = Vector3.zero;
        Quaternion circleRotation = Quaternion.identity;
        if (IsPinching) {
          centerColor = Color.green;
          centerPosition = Position;
          circleRotation = Rotation;
        } else {
          Hand hand = _handModel.GetLeapHand();
          if (hand != null) {
            Finger thumb = hand.Fingers[0];
            Finger index = hand.Fingers[1];
            centerColor = Color.red;
            centerPosition = ((thumb.Bone(Bone.BoneType.TYPE_DISTAL).NextJoint + index.Bone(Bone.BoneType.TYPE_DISTAL).NextJoint) / 2).ToVector3();
            circleRotation = hand.Basis.CalculateRotation();
          }
        }
        Vector3 axis;
        float angle;
        circleRotation.ToAngleAxis(out angle, out axis);
        Utils.DrawCircle(centerPosition, axis, _activatePinchDist / 2, centerColor);
        Utils.DrawCircle(centerPosition, axis, _deactivatePinchDist / 2, Color.blue);
      }
    }
    #endif

  }
}
