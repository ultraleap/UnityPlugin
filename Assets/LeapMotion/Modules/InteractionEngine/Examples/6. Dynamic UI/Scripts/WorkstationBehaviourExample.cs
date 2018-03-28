/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap.Unity.Animation;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples {

  /// <summary>
  /// This example script constructs behavior for a very specific kind of UI object that can
  /// animate out a "workstation" panel when thrown or placed outside of an anchor.
  /// 
  /// Workstation objects exhibit the following behaviors:
  /// - Play a tween forwards or backwards when entering or exiting "workstation mode",
  ///   to open or close an arbitrary transform containing other UI elements.
  /// - Smoothly fly to a "good" target location and orientation and enter workstation mode
  ///   when the user throws the anchorable object.
  /// - Set the anchorable object to kinematic when in workstation mode.
  /// - Allow the user to adjust the anchorable object's position and rotation in a constrained
  ///   way while in workstation mode. If the user moves the anchorable object quickly, exit
  ///   workstation mode.
  /// 
  /// It is a more complicated example that demonstrates using InteractionBehaviours and
  /// AnchorableBehaviours to create novel UI behavior.
  /// </summary>
  [RequireComponent(typeof(InteractionBehaviour))]
  [RequireComponent(typeof(AnchorableBehaviour))]
  [AddComponentMenu("")]
  public class WorkstationBehaviourExample : MonoBehaviour {

    /// <summary>
    /// If the rigidbody of this object moves faster than this speed and the object
    /// is in workstation mode, it will exit workstation mode.
    /// </summary>
    public const float MAX_SPEED_AS_WORKSTATION = 0.005F;

    /// <summary>
    /// If the rigidbody of this object moves slower than this speed and the object
    /// wants to enter workstation mode, it will first pick a target position and
    /// travel there. (Otherwise, it will open its workstation in-place.)
    /// </summary>
    public const float MIN_SPEED_TO_ACTIVATE_TRAVELING = 0.5F;

    public TransformTweenBehaviour workstationModeTween;

    private InteractionBehaviour _intObj;
    private AnchorableBehaviour _anchObj;

    private bool _wasKinematicBeforeActivation = false;

    /// <summary>
    /// Gets whether the workstation is currently traveling to a target position to open
    /// in workstation mode. Will return false if it is not traveling due to not being
    /// in workstation mode or if it has already reached its target position.
    /// </summary>
    public bool isTraveling { get { return _travelTween.isValid && _travelTween.isRunning; } }

    public enum WorkstationState { Closed, Traveling, Opening, Open, Closing }
    public WorkstationState workstationState {
      get {
        if (workstationModeTween == null) return WorkstationState.Closed;
        else {
          if (workstationModeTween.tween.progress == 0F && !isTraveling) {
            return WorkstationState.Closed;
          }
          else if (isTraveling) {
            return WorkstationState.Traveling;
          }
          else if (workstationModeTween.tween.progress != 1F
                && workstationModeTween.tween.direction == Direction.Forward) {
            return WorkstationState.Opening;
          }
          else if (workstationModeTween.tween.progress == 1F) {
            return WorkstationState.Open;
          }
          else {
            return WorkstationState.Closing;
          }
        }
      }
    }

    void OnValidate() {
      refreshRequiredComponents();
    }

    void Awake() {
      initWorkstationPoseFunctions();
    }

    void Start() {
      refreshRequiredComponents();

      if (!_anchObj.tryAnchorNearestOnGraspEnd) {
        Debug.LogWarning("WorkstationBehaviour expects its AnchorableBehaviour's tryAnchorNearestOnGraspEnd property to be enabled.", this.gameObject);
      }
    }

    void OnDestroy() {
      _intObj.OnGraspedMovement -= onGraspedMovement;

      _anchObj.OnPostTryAnchorOnGraspEnd -= onPostObjectGraspEnd;
    }

    public void ActivateWorkstation() {
      if (workstationState != WorkstationState.Open) {
        _wasKinematicBeforeActivation = _intObj.rigidbody.isKinematic;
        _intObj.rigidbody.isKinematic = true;
      }

      workstationModeTween.PlayForward();
    }

    public void DeactivateWorkstation() {
      _intObj.rigidbody.isKinematic = _wasKinematicBeforeActivation;

      workstationModeTween.PlayBackward();
    }

    private void refreshRequiredComponents() {
      _intObj = GetComponent<InteractionBehaviour>();
      _anchObj = GetComponent<AnchorableBehaviour>();

      _intObj.OnGraspedMovement += onGraspedMovement;

      _anchObj.OnPostTryAnchorOnGraspEnd += onPostObjectGraspEnd;
    }

    private void onGraspedMovement(Vector3 preSolvePos, Quaternion preSolveRot,
                                   Vector3 curPos, Quaternion curRot,
                                   List<InteractionController> controllers) {
      // If the workstation is not fully open when grasped, close it.
      if (workstationState == WorkstationState.Opening) {
        DeactivateWorkstation();
      }

      // If the velocity of the object while grasped is too large, exit workstation mode.
      if (_intObj.rigidbody.velocity.magnitude > MAX_SPEED_AS_WORKSTATION
          || (_intObj.rigidbody.isKinematic && ((preSolvePos - curPos).magnitude / Time.fixedDeltaTime) > MAX_SPEED_AS_WORKSTATION)) {
        DeactivateWorkstation();
      }

      // Lock our upward axis while workstation mode is open.
      if (workstationState == WorkstationState.Open) {
        _intObj.rigidbody.rotation = (Quaternion.FromToRotation(_intObj.rigidbody.rotation * Vector3.up, Vector3.up)) * _intObj.rigidbody.rotation;
        _intObj.transform.rotation = _intObj.rigidbody.rotation;
      }
    }

    private void onPostObjectGraspEnd() {
      if (_anchObj.preferredAnchor == null) {
        // Choose a good position and rotation for workstation mode and begin traveling there.
        Vector3 targetPosition;

        // Choose the current position if our velocity is small.
        if (_intObj.rigidbody.velocity.magnitude < MIN_SPEED_TO_ACTIVATE_TRAVELING) {
          targetPosition = _intObj.rigidbody.position;
        }
        else {
          targetPosition = determineWorkstationPosition();
        }

        Quaternion targetRotation = determineWorkstationRotation(targetPosition);

        beginTraveling(_intObj.rigidbody.position, _intObj.rigidbody.velocity,
                       _intObj.rigidbody.rotation, _intObj.rigidbody.angularVelocity,
                       targetPosition, targetRotation);
      }
      else {
        // Ensure the workstation is not active or being deactivated if
        // we are attaching to an anchor.
        DeactivateWorkstation();
      }
    }

    #region Traveling

    private const float MAX_TRAVEL_SPEED = 4.00F;

    private Tween _travelTween;

    private float      _initTravelTime = 0F;
    private Vector3    _initTravelPosition = Vector3.zero;
    private Vector3    _initTravelVelocity = Vector3.zero;
    private Quaternion _initTravelRotation = Quaternion.identity;
    private Vector3    _initTravelAngVelocity = Vector3.zero;
    private Vector3    _effGravity = Vector3.zero;

    private Vector3    _travelTargetPosition;
    private Quaternion _travelTargetRotation;

    private Vector2 _minMaxWorkstationTravelTime    = new Vector2(0.05F, 1.00F);
    private Vector2 _minMaxTravelTimeFromThrowSpeed = new Vector2(0.30F, 8.00F);

    private void beginTraveling(Vector3 initPosition, Vector3 initVelocity,
                                Quaternion initRotation, Vector3 initAngVelocity,
                                Vector3 targetPosition, Quaternion targetRotation) {
      _initTravelTime        = Time.time;
      _initTravelPosition    = initPosition;
      _initTravelVelocity    = initVelocity;
      _initTravelRotation    = initRotation;
      _initTravelAngVelocity = initAngVelocity;

      float velMagnitude = _initTravelVelocity.magnitude;
      if (velMagnitude > MAX_TRAVEL_SPEED) {
        float capSpeedMultiplier = MAX_TRAVEL_SPEED / velMagnitude;
        _initTravelVelocity *= capSpeedMultiplier;
      }

      _effGravity            = Vector3.Lerp(Vector3.zero, Physics.gravity, initVelocity.magnitude.Map(0.80F, 3F, 0F, 0.70F));



      _travelTargetPosition = targetPosition;
      _travelTargetRotation = targetRotation;

      // Construct a single-use Tween that will last a specific duration
      // and specify a custom callback as it progresses to update the
      // object's position and rotation.
      _travelTween = Tween.Single()
                          .OverTime(_initTravelVelocity.magnitude.Map(_minMaxTravelTimeFromThrowSpeed.x, _minMaxTravelTimeFromThrowSpeed.y,
                                                                      _minMaxWorkstationTravelTime.x, _minMaxWorkstationTravelTime.y))
                          .OnProgress(onTravelTweenProgress)
                          .OnReachEnd(ActivateWorkstation) // When the tween is finished, open workstation mode.
                          .Play();
    }

    private void onTravelTweenProgress(float progress) {
      float      curTime = Time.time;
      Vector3    extrapolatedPosition = evaluatePosition(_initTravelPosition, _initTravelVelocity, _effGravity, _initTravelTime, curTime);
      Quaternion extrapolatedRotation = evaluateRotation(_initTravelRotation, _initTravelAngVelocity, _initTravelTime, curTime);
      
      // Interpolate from the position and rotation that the object would naturally have over time
      // (by movement due to inertia and by acceleration due to gravity)
      // to the target position and rotation over the lifetime of the tween.
      _intObj.rigidbody.position =     Vector3.Lerp(extrapolatedPosition, _travelTargetPosition, progress);
      _intObj.rigidbody.rotation = Quaternion.Slerp(extrapolatedRotation, _travelTargetRotation, progress);
    }

    private void cancelTraveling() {
      // In case traveling was halted mid-travel-tween, halt the tween.
      if (_travelTween.isValid) { _travelTween.Stop(); }
    }

    /// <summary>
    /// Evaluates the position of a body over time with initial velocity and acceleration due to gravity.
    /// </summary>
    private Vector3 evaluatePosition(Vector3 initialPosition, Vector3 initialVelocity, Vector3 gravity, float initialTime, float timeToEvaluate) {
      float t = timeToEvaluate - initialTime;
      return initialPosition + (initialVelocity * t) + (0.5f * gravity * t * t);
    }

    /// <summary>
    /// Evaluates the rotation of a body over time with initial velocity and acceleration due to gravity.
    /// </summary>
    private Quaternion evaluateRotation(Quaternion initialRotation, Vector3 angularVelocity, float initialTime, float timeToEvaluate) {
      float t = timeToEvaluate - initialTime;
      return Quaternion.Euler(angularVelocity * t) * initialRotation;
    }

    #endregion

    #region Workstation Pose

    public delegate Vector3 WorkstationPositionFunc(Vector3 userEyePosition, Quaternion userEyeRotation,
                                                 Vector3 workstationObjInitPosition, Vector3 workstationObjInitVelocity, float workstationObjRadius,
                                                 List<Vector3> otherWorkstationPositions, List<float> otherWorkstationRadii);

    public delegate Quaternion WorkstationRotationFunc(Vector3 userEyePosition, Vector3 targetWorkstationPosition);

    /// <summary>
    /// The function used to calculate this workstation's target position. By default, will attempt to choose
    /// a nearby position approximately in front of the user and in the direction the workstation object is currently
    /// traveling, and a position that doesn't overlap with other workstations. The default method is set on Awake(),
    /// so it can be overridden in OnEnable() or Start().
    /// </summary>
    public WorkstationPositionFunc workstationPositionFunc;

    /// <summary>
    /// The function used to calculate this workstation's target rotation. By default, will make the workstation's
    /// forward vector face the camera while aligning its upward vector against gravity.
    /// </summary>
    public WorkstationRotationFunc workstationRotationFunc;

    private List<Vector3> _otherStationObjPositions = new List<Vector3>();
    private List<float> _otherStationObjRadii = new List<float>();

    // Called on Awake(); possible to override the default functions in a MonoBehaviour's Start().
    private void initWorkstationPoseFunctions() {
      workstationPositionFunc = DefaultDetermineWorkstationPosition;
      workstationRotationFunc = DefaultDetermineWorkstationRotation;
    }

    private Vector3 determineWorkstationPosition() {
      return workstationPositionFunc(Camera.main.transform.position, Camera.main.transform.rotation,
                                     _intObj.rigidbody.position, _intObj.rigidbody.velocity, 0.30F,
                                     _otherStationObjPositions, _otherStationObjRadii);
    }

    private Quaternion determineWorkstationRotation(Vector3 workstationPosition) {
      return workstationRotationFunc(Camera.main.transform.position, workstationPosition);
    }

    public static Vector3 DefaultDetermineWorkstationPosition(Vector3 userEyePosition, Quaternion userEyeRotation,
                                                Vector3 workstationObjInitPosition, Vector3 workstationObjInitVelocity, float workstationObjRadius,
                                                List<Vector3> otherWorkstationPositions, List<float> otherWorkstationRadii) {
      // Push velocity away from the camera if necessary.
      Vector3 towardsCamera = (userEyePosition - workstationObjInitPosition).normalized;
      float towardsCameraness = Mathf.Clamp01(Vector3.Dot(towardsCamera, workstationObjInitVelocity.normalized));
      workstationObjInitVelocity = workstationObjInitVelocity + Vector3.Lerp(Vector3.zero, -towardsCamera * 2.00F, towardsCameraness);

      // Calculate velocity direction on the XZ plane.
      Vector3 groundPlaneVelocity = Vector3.ProjectOnPlane(workstationObjInitVelocity, Vector3.up);
      float groundPlaneDirectedness = groundPlaneVelocity.magnitude.Map(0.003F, 0.40F, 0F, 1F);
      Vector3 groundPlaneDirection = groundPlaneVelocity.normalized;

      // Calculate camera "forward" direction on the XZ plane.
      Vector3 cameraGroundPlaneForward = Vector3.ProjectOnPlane(userEyeRotation * Vector3.forward, Vector3.up);
      float cameraGroundPlaneDirectedness = cameraGroundPlaneForward.magnitude.Map(0.001F, 0.01F, 0F, 1F);
      Vector3 alternateCameraDirection = (userEyeRotation * Vector3.forward).y > 0F ? userEyeRotation * Vector3.down : userEyeRotation * Vector3.up;
      cameraGroundPlaneForward = Vector3.Slerp(alternateCameraDirection, cameraGroundPlaneForward, cameraGroundPlaneDirectedness);
      cameraGroundPlaneForward = cameraGroundPlaneForward.normalized;

      // Calculate a placement direction based on the camera and throw directions on the XZ plane.
      Vector3 placementDirection = Vector3.Slerp(cameraGroundPlaneForward, groundPlaneDirection, groundPlaneDirectedness);

      // Calculate a placement position along the placement direction between min and max placement distances.
      float minPlacementDistance = 0.25F;
      float maxPlacementDistance = 0.51F;
      Vector3 placementPosition = userEyePosition + placementDirection * Mathf.Lerp(minPlacementDistance, maxPlacementDistance,
                                                                                    (groundPlaneDirectedness * workstationObjInitVelocity.magnitude)
                                                                                    .Map(0F, 1.50F, 0F, 1F));

      // Don't move far if the initial velocity is small.
      float overallDirectedness = workstationObjInitVelocity.magnitude.Map(0.00F, 3.00F, 0F, 1F);
      placementPosition = Vector3.Lerp(workstationObjInitPosition, placementPosition, overallDirectedness * overallDirectedness);
      
      // Enforce placement height.
      float placementHeightFromCamera = -0.30F;
      placementPosition.y = userEyePosition.y + placementHeightFromCamera;

      // Enforce minimum placement away from user.
      Vector2 cameraXZ = new Vector2(userEyePosition.x, userEyePosition.z);
      Vector2 stationXZ = new Vector2(placementPosition.x, placementPosition.z);
      float placementDist = Vector2.Distance(cameraXZ, stationXZ);
      if (placementDist < minPlacementDistance) {
        float distanceLeft = (minPlacementDistance - placementDist) + workstationObjRadius;
        Vector2 xzDisplacement = (stationXZ - cameraXZ).normalized * distanceLeft;
        placementPosition += new Vector3(xzDisplacement[0], 0F, xzDisplacement[1]);
      }

      return placementPosition;
    }

    public static Quaternion DefaultDetermineWorkstationRotation(Vector3 userEyePos, Vector3 workstationPosition) {
      Vector3 toCamera = userEyePos - workstationPosition;
      toCamera.y = 0F;
      Quaternion placementRotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);

      return placementRotation;
    }

    #endregion

  }

}
