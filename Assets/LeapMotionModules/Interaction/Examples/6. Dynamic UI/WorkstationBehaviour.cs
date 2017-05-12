using Leap.Unity.Animation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction {

  [RequireComponent(typeof(InteractionBehaviour))]
  [RequireComponent(typeof(AnchorableBehaviour))]
  public class WorkstationBehaviour : MonoBehaviour {

    /// <summary>
    /// If the rigidbody of this object moves faster than this speed and the object
    /// is in workstation mode, it will exit workstation mode.
    /// </summary>
    public const float MAX_SPEED_AS_WORKSTATION = 0.2F;

    /// <summary>
    /// If the rigidbody of this object moves slower than this speed and the object
    /// wants to enter workstation mode, it will first pick a target position and
    /// travel there. (Otherwise, it will open its workstation in-place.)
    /// </summary>
    public const float MIN_SPEED_TO_ACTIVATE_TRAVELING = 0.2F;

    public TransformTweenBehaviour workstationModeTween;

    public float workstationPositionTravelTime = 0.33F;

    private InteractionBehaviour _intObj;
    private AnchorableBehaviour _anchObj;

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
      workstationModeTween.PlayForward();
    }

    public void DeactivateWorkstation() {
      workstationModeTween.PlayBackward();
    }

    private void refreshRequiredComponents() {
      _intObj = GetComponent<InteractionBehaviour>();
      _anchObj = GetComponent<AnchorableBehaviour>();

      _intObj.OnGraspedMovement += onGraspedMovement;

      _anchObj.OnPostTryAnchorOnGraspEnd += onPostObjectGraspEnd;
    }

    private void onGraspedMovement(Vector3 preSolvePos, Quaternion preSolveRot, Vector3 curPos, Quaternion curRot, List<InteractionHand> hands) {
      // If the workstation is not fully open when grasped, close it.
      if (workstationState == WorkstationState.Opening) {
        DeactivateWorkstation();
      }

      // If the velocity of the object while grasped is even slightly large, exit workstation mode.
      if (_intObj.rigidbody.velocity.magnitude > MAX_SPEED_AS_WORKSTATION) {
        DeactivateWorkstation();
      }

      // Lock our rotation while in workstation mode.
      if (workstationState == WorkstationState.Open) {
        _intObj.rigidbody.rotation = preSolveRot;
      }
    }

    private void onPostObjectGraspEnd(AnchorableBehaviour anchObj) {
      if (_anchObj.preferredAnchor == null) {
        // Enter workstation mode immediately if our velocity is small.
        if ( _intObj.rigidbody.velocity.magnitude < MIN_SPEED_TO_ACTIVATE_TRAVELING) {
          ActivateWorkstation();
        }
        else {
          // Choose a good workstation position and rotation and begin traveling there.
          Vector3 targetPosition;
          Quaternion targetRotation;
          determineWorkstationPose(out targetPosition, out targetRotation);

          beginTraveling(_intObj.rigidbody.position, _intObj.rigidbody.velocity,
                         _intObj.rigidbody.rotation, _intObj.rigidbody.angularVelocity,
                         targetPosition, targetRotation);
        }
      }
      else {
        // Ensure the workstation is not active or being deactivated if
        // we are attaching to an anchor.
        DeactivateWorkstation();
      }
    }

    #region Traveling

    private Tween _travelTween;

    private float      _initTravelTime = 0F;
    private Vector3    _initTravelPosition = Vector3.zero;
    private Vector3    _initTravelVelocity = Vector3.zero;
    private Quaternion _initTravelRotation = Quaternion.identity;
    private Vector3    _initTravelAngVelocity = Vector3.zero;

    private Vector3    _travelTargetPosition;
    private Quaternion _travelTargetRotation;

    private void beginTraveling(Vector3 initPosition, Vector3 initVelocity,
                                Quaternion initRotation, Vector3 initAngVelocity,
                                Vector3 targetPosition, Quaternion targetRotation) {
      _initTravelPosition    = initPosition;
      _initTravelVelocity    = initVelocity;
      _initTravelRotation    = initRotation;
      _initTravelAngVelocity = initAngVelocity;
      _initTravelTime        = Time.time;

      _travelTargetPosition = targetPosition;
      _travelTargetRotation = targetRotation;

      // Construct a single-use Tween that will last a specific duration
      // and specify a custom callback as it progresses to update the
      // object's position and rotation.
      _travelTween = Tween.Single()
                          .OverTime(workstationPositionTravelTime)
                          .OnProgress(onTravelTweenProgress)
                          .OnReachEnd(ActivateWorkstation); // When the tween is finished, open workstation mode.
    }

    private void onTravelTweenProgress(float progress) {
      float      curTime = Time.time;
      Vector3    extrapolatedPosition = evaluatePosition(_initTravelPosition, _initTravelVelocity,    _initTravelTime, curTime);
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
    private Vector3 evaluatePosition(Vector3 initialPosition, Vector3 initialVelocity, float initialTime, float timeToEvaluate) {
      float t = timeToEvaluate - initialTime;
      return initialPosition + (initialVelocity * t) + (0.5f * Physics.gravity * t * t);
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

    private void determineWorkstationPose(out Vector3 position, out Quaternion rotation) {
      position = Vector3.zero;
      rotation = Quaternion.identity;
    }

    public static void DetermineWorkstationPose(Vector3 userEyePos, Quaternion userEyeRot,
                                                Vector3 stationObjPos, Vector3 stationObjVel,
                                                List<Vector3> otherStationPositions, List<float> otherStationRadii,
                                                out Vector3 stationPos, out Quaternion stationRot) {
      Vector3 groundPlaneVelocity = Vector3.ProjectOnPlane(stationObjVel, Vector3.up);
      
      float minPlacementDistance = 0.40F;
      float maxPlacementDistance = 0.60F;
      float minDotProductFromFacing = 0.17F;

      stationPos = stationObjPos;
      stationRot = userEyeRot;
    }

    #endregion

  }

}