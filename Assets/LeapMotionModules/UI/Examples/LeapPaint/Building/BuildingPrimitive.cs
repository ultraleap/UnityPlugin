using Leap.Unity.UI.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.LeapPaint {

  [RequireComponent(typeof(InteractionBehaviour))]
  public class BuildingPrimitive : MonoBehaviour {

    public Toolbelt toolbelt;

    [Header("Anchored State")]
    public Transform anchor;
    public float maxHoverReach = 0.1F;
    public AnimationCurve hoverReachFromDistance;

    private InteractionBehaviour _intObj;

    private enum BuildingPrimitiveState { Anchored, Grasped, Placed }
    private BuildingPrimitiveState _state = BuildingPrimitiveState.Anchored;

    void Start() {
      _intObj = GetComponent<InteractionBehaviour>();

      InitGraspedControl();
    }

    void Update() {
      if (_state == BuildingPrimitiveState.Anchored) {
        UpdateAnchoredState();
      }
    }

    #region Anchored State

    private void UpdateAnchoredState() {
      float reachTargetAmount = 0F;
      Vector3 towardsHand = Vector3.zero;
      if (_intObj.isHovered) {
        Hand hoveringHand = _intObj.closestHoveringHand;
        reachTargetAmount = hoverReachFromDistance.Evaluate(
                              Vector3.Distance(hoveringHand.PalmPosition.ToVector3(), anchor.position)
                            );
        towardsHand = hoveringHand.PalmPosition.ToVector3() - anchor.position;
      }

      Vector3 targetPosition = anchor.position + towardsHand * maxHoverReach * reachTargetAmount;

      this.transform.position = Vector3.Lerp(this.transform.position, targetPosition, 5 * Time.deltaTime);
    }

    #endregion

    #region Grasped Control

    private void InitGraspedControl() {
      _intObj.OnGraspBegin += OnGraspBegin;
      _intObj.OnGraspEnd   += OnGraspEnd;

      _intObj.OnPreGraspedMovement += PreGraspedMovement;
      _intObj.OnGraspedMovement    += SmoothGraspedMovement;

      //_intObj.OnMultiGraspBegin;
      //_intObj.OnMultiGraspEnd;
      //_intObj.OnPreMultiGraspedMovement;
      //_intObj.OnMultiGraspedMovement;
    }

    private void OnGraspBegin(Hand _) {
      _state = BuildingPrimitiveState.Grasped;
      this.transform.parent = null;
    }

    private void OnGraspEnd(Hand _) {
      float distanceToAnchor = Vector3.Distance(this.transform.position, anchor.transform.position);
      if (distanceToAnchor < 0.3F && toolbelt.isOpen) {
        _state = BuildingPrimitiveState.Anchored;
        this.transform.parent = anchor.transform;
      }
      else {
        _state = BuildingPrimitiveState.Placed;
        this.transform.parent = null;
      }
    }

    private Vector3    _preSolvedPos;
    private Quaternion _preSolvedRot;

    private void PreGraspedMovement(Vector3 preSolvedPos, Quaternion preSolvedRot, Hand _) {
      _preSolvedPos = preSolvedPos;
      _preSolvedRot = preSolvedRot;
    }

    private void SmoothGraspedMovement(Vector3 solvedPos, Quaternion solvedRot, Hand _) {
      // This speed-based (s)lerping allows more precise placement of objects
      // when the desired adjustment of the object's position/rotation is small.

      float lerpCoeffPerSec = Vector3.Distance(_preSolvedPos, solvedPos).Map(0F, 0.2F, 0.01F, 100F);
      float slerpCoeffPerSec = Quaternion.Angle(_preSolvedRot, solvedRot).Map(0F, 50F, 0.01F, 100F);

      _intObj.Rigidbody.position = Vector3.Lerp    (_preSolvedPos, solvedPos, lerpCoeffPerSec * Time.deltaTime);
      _intObj.Rigidbody.rotation = Quaternion.Slerp(_preSolvedRot, solvedRot, slerpCoeffPerSec * Time.deltaTime);
    }

    #endregion

  }

}