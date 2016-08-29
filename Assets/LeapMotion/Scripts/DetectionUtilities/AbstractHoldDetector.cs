using UnityEngine;
using Leap.Unity.Attributes;

namespace Leap.Unity { 

  /**
  * A base class for implementing detectors that detect a holding pose of a hand.
  *
  * Such detectors might use the PinchStrength, PinchDistance, GrabStrength, or GrabAngle
  * properties of the Hand or might use a more complex heuristic.
  */
  public abstract class AbstractHoldDetector : Detector {

    /** Implementations must implement this method. */
    protected abstract void ensureUpToDate();

    [AutoFind(AutoFindLocations.Parents)]
    [SerializeField]
    protected IHandModel _handModel;
    public IHandModel HandModel { get { return _handModel; } set { _handModel = value; } }

    /**
    * Whether the Transform of the object containing this Detector script
    * is transformed by the Position and Rotation of the hand when IsHolding is true.
    *
    * If false, the Transform is not affected.
    */
    [Tooltip("Whether to change the transform of the parent object.")]
    public bool ControlsTransform = true;
    /** Whether to draw the detector's Gizmos for debugging. (Not every detector provides gizmos.)
     * @since 4.1.2 
     */
    [Tooltip("Draw this detector's Gizmos, if any. (Gizmos must be on in Unity edtor, too.)")]
    public bool ShowGizmos = true;

    protected int _lastUpdateFrame = -1;

    protected bool _didChange = false;

    protected Vector3 _position;
    protected Quaternion _rotation;
    protected Vector3 _direction = Vector3.forward;
    protected Vector3 _normal = Vector3.up;
    protected float _distance;

    protected float _lastHoldTime = 0.0f;
    protected float _lastReleaseTime = 0.0f;
    protected Vector3 _lastPosition = Vector3.zero;
    protected Quaternion _lastRotation = Quaternion.identity;
    protected Vector3 _lastDirection = Vector3.forward;
    protected Vector3 _lastNormal = Vector3.up;
    protected float _lastDistance = 1.0f;


    protected virtual void Awake() {
      if (GetComponent<IHandModel>() != null && ControlsTransform == true) {
        Debug.LogWarning("Detector should not be control the IHandModel's transform. Either attach it to its own transform or set ControlsTransform to false.");
      }
      if (_handModel == null) {
        _handModel = GetComponentInParent<IHandModel>();
        if (_handModel == null) {
          Debug.LogWarning("The HandModel field of Detector was unassigned and the detector has been disabled.");
          enabled = false;
        }
      }
    }

    protected virtual void Update() {
      //We ensure the data is up to date at all times because
      //there are some values (like LastPinchTime) that cannot
      //be updated on demand
      ensureUpToDate();
    }

    /// <summary>
    /// Returns whether or not the dectector is currently detecting a pinch or grab.
    /// </summary>
    public virtual bool IsHolding {
      get {
        ensureUpToDate();
        return IsActive;
      }
    }

    /// <summary>
    /// Returns whether or not the value of IsPinching is different than the value reported during
    /// the previous frame.
    /// </summary>
    public virtual bool DidChangeFromLastFrame {
      get {
        ensureUpToDate();
        return _didChange;
      }
    }

    /// <summary>
    /// Returns whether or not the value of IsHolding changed to true between this frame and the previous.
    /// </summary>
    public virtual bool DidStartHold {
      get {
        ensureUpToDate();
        return DidChangeFromLastFrame && IsHolding;
      }
    }

    /// <summary>
    /// Returns whether or not the value of IsHolding changed to false between this frame and the previous.
    /// </summary>
    public virtual bool DidRelease {
      get {
        ensureUpToDate();
        return DidChangeFromLastFrame && !IsHolding;
      }
    }

    /// <summary>
    /// Returns the value of Time.time during the most recent pinch event.
    /// </summary>
    public float LastHoldTime {
      get {
        ensureUpToDate();
        return _lastHoldTime;
      }
    }

    /// <summary>
    /// Returns the value of Time.time during the most recent unpinch event.
    /// </summary>
    public float LastReleaseTime {
      get {
        ensureUpToDate();
        return _lastReleaseTime;
      }
    }

    /// <summary>
    /// Returns the position value of the detected pinch or grab.  If a pinch or grab is not currently being
    /// detected, returns the most recent position value.
    /// </summary>
    public Vector3 Position {
      get {
        ensureUpToDate();
        return _position;
      }
    }
    public Vector3 LastActivePosition { get { return _lastPosition; } }
    /// <summary>
    /// Returns the rotation value of the detected pinch or grab.  If a pinch or grab is not currently being
    /// detected, returns the most recent rotation value.
    /// </summary>
    public Quaternion Rotation {
      get {
        ensureUpToDate();
        return _rotation;
      }
    }
    public Quaternion LastActiveRotation { get { return _lastRotation; } }

    public Vector3 Direction { get { return _direction; } }
    public Vector3 LastActiveDirection { get { return _lastDirection; } }
    public Vector3 Normal { get { return _normal; } }
    public Vector3 LastActiveNormal { get { return _lastNormal; } }
    public float Distance { get { return _distance; } }
    public float LastActiveDistance { get { return _lastDistance; } }


    protected virtual void changeState(bool shouldBeActive) {
      bool currentState = IsActive;
      if (shouldBeActive) {
        _lastHoldTime = Time.time;
        Activate();
      } else {
        _lastReleaseTime = Time.time;
        Deactivate();
      }
      if (currentState != IsActive) {
        _didChange = true;
      }
    }

    #if UNITY_EDITOR
    protected virtual void OnDrawGizmos () {
      if (ShowGizmos) {
        ensureUpToDate();
        Color centerColor;
        Vector3 centerPosition = _position;
        Quaternion circleRotation = _rotation;
        if (IsHolding) {
          centerColor = OnColor;
        } else {
          centerColor = OffColor;
        }
        Vector3 axis;
        float angle;
        circleRotation.ToAngleAxis(out angle, out axis);
        Utils.DrawCircle(centerPosition, Normal, Distance / 2, centerColor);
        Gizmos.color = NormalColor;
        Gizmos.DrawLine(centerPosition, centerPosition + Direction * Distance / 2);
        Gizmos.color = DirectionColor;
        Gizmos.DrawLine(centerPosition, centerPosition + Normal * Distance / 2);
      }
    }
    #endif

  }
}
