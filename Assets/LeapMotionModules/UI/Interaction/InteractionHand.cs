using Leap.Unity.RuntimeGizmos;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public class InteractionHand {

    private InteractionManager _interactionManager;
    private Func<Hand> _handAccessor;
    private Hand _hand;

    public InteractionHand(InteractionManager interactionManager,
                           Func<Hand> handAccessor,
                           float hoverActivationRadius,
                           float touchActivationRadius) {
      _interactionManager = interactionManager;
      _handAccessor = handAccessor;

      InitHovering(hoverActivationRadius);
      InitTouch(touchActivationRadius);
      //InitContact(); // TODO: Not yet implemented.
      InitGrasping();
    }

    public Hand GetLeapHand() {
      return _hand;
    }

    public void FixedUpdateHand(bool doHovering, bool doContact, bool doGrasping) {
      _hand = _handAccessor();

      if (doHovering) FixedUpdateHovering();
      if (doContact || doGrasping) FixedUpdateTouch();
      //if (doContact) FixedUpdateContact(); // TODO: Not yet implemented.
      if (doGrasping) FixedUpdateGrasping();
    }

    #region Hovering

    private void InitHovering(float hoverActivationRadius) {
      // Standard Hover
      _hoverActivityManager = new ActivityManager(_interactionManager, hoverActivationRadius);
    }

    private void FixedUpdateHovering() {
      _hoverActivityManager.FixedUpdateHand(_hand);
      CalculateIntentionPosition();
      HoverCheckResults hoverResults = CheckHoverForHand(_hand, _hoverActivityManager.ActiveBehaviours);
      ProcessHoverCheckResults(hoverResults);
      ProcessPrimaryHoverCheckResults(hoverResults);
    }

    #region Standard Hover -- Activity Manager

    /// <summary>
    /// Encapsulates tracking Hand <-> InteractionBehaviour hover candidates
    /// (broad-phase) via PhysX sphere queries.
    /// </summary>
    private ActivityManager _hoverActivityManager;
    public float HoverActivationRadius {
      get {
        return _hoverActivityManager.activationRadius;
      }
      set {
        _hoverActivityManager.activationRadius = value;
      }
    }

    private struct HoverCheckResults {
      public HashSet<InteractionBehaviourBase> hovered;
      public InteractionBehaviourBase primaryHovered;
      public float primaryHoveredDistance;
      public Hand checkedHand;
    }

    private HashSet<InteractionBehaviourBase> _hoveredLastFrame = new HashSet<InteractionBehaviourBase>();

    private HashSet<InteractionBehaviourBase> _hoverableCache = new HashSet<InteractionBehaviourBase>();
    private HoverCheckResults CheckHoverForHand(Hand hand, HashSet<InteractionBehaviourBase> hoverCandidates) {
      _hoverableCache.Clear();

      HoverCheckResults results = new HoverCheckResults() {
        hovered = _hoverableCache,
        primaryHovered = null,
        primaryHoveredDistance = float.PositiveInfinity,
        checkedHand = hand
      };

      foreach (var interactionObj in hoverCandidates) {
        results = CheckHoverForElement(hand, interactionObj, results);
      }

      return results;
    }

    private HoverCheckResults CheckHoverForElement(Hand hand, InteractionBehaviourBase hoverable, HoverCheckResults curResults) {
      float distance = hoverable.GetHoverDistance(_intentionPosition);
      if (distance > 0F) {
        curResults.hovered.Add(hoverable);
      }
      if (distance < MAX_PRIMARY_HOVER_DISTANCE && distance < curResults.primaryHoveredDistance) {
        curResults.primaryHovered = hoverable;
        curResults.primaryHoveredDistance = distance;
      }
      return curResults;
    }

    private List<InteractionBehaviourBase> _removalCache = new List<InteractionBehaviourBase>();
    private void ProcessHoverCheckResults(HoverCheckResults hoverResults) {
      var trackedBehaviours = _hoverActivityManager.ActiveBehaviours;
      foreach (var hoverable in trackedBehaviours) {
        bool inLastFrame = false, inCurFrame = false;
        if (hoverResults.hovered.Contains(hoverable)) {
          inCurFrame = true;
        }
        if (_hoveredLastFrame.Contains(hoverable)) {
          inLastFrame = true;
        }

        if (inCurFrame && !inLastFrame) {
          hoverable.HoverBegin(hoverResults.checkedHand);
          _hoveredLastFrame.Add(hoverable);
        }
        if (inCurFrame && inLastFrame) {
          hoverable.HoverStay(hoverResults.checkedHand);
        }
        if (!inCurFrame && inLastFrame) {
          hoverable.HoverEnd(hoverResults.checkedHand);
          _hoveredLastFrame.Remove(hoverable);
        }
      }

      foreach (var hoverable in _hoveredLastFrame) {
        if (!trackedBehaviours.Contains(hoverable)) {
          hoverable.HoverEnd(hoverResults.checkedHand);
          _removalCache.Add(hoverable);
        }
      }
      foreach (var hoverable in _removalCache) {
        _hoveredLastFrame.Remove(hoverable);
      }
      _removalCache.Clear();
    }

    private void ProcessPrimaryHoverCheckResults(HoverCheckResults hoverResults) {
      if (hoverResults.primaryHovered == _primaryHoveredLastFrame) {
        if (hoverResults.primaryHovered != null) hoverResults.primaryHovered.PrimaryHoverStay(hoverResults.checkedHand);
      }
      else {
        if (_primaryHoveredLastFrame != null) {
          _primaryHoveredLastFrame.PrimaryHoverEnd(hoverResults.checkedHand);
        }
        _primaryHoveredLastFrame = hoverResults.primaryHovered;
        if (_primaryHoveredLastFrame != null) _primaryHoveredLastFrame.PrimaryHoverBegin(hoverResults.checkedHand);
      }
    }

    #endregion

    private const float MAX_PRIMARY_HOVER_DISTANCE = 0.1F;
    private Vector3 _intentionPosition;
    private InteractionBehaviourBase _primaryHoveredLastFrame = null;

    private void CalculateIntentionPosition() {
      if (_hand == null) return;

      // Weighted average of medial finger bone positions and directions for base (distal) intention ray
      float usageSum = 0F;
      Leap.Vector averagePosition = Leap.Vector.Zero;
      Leap.Vector averageDirection = Leap.Vector.Zero;
      for (int i = 1; i < 5; i++) {
        Leap.Vector distalFingerBoneTip = _hand.Fingers[i].bones[3].NextJoint;
        Leap.Vector medialFingerBoneDirection = _hand.Fingers[i].bones[2].Direction;
        Leap.Vector distalDirection = _hand.Basis.zBasis;
        var fingerUsage = ((medialFingerBoneDirection.x * distalDirection.x)
                         + (medialFingerBoneDirection.y * distalDirection.y)
                         + (medialFingerBoneDirection.z * distalDirection.z)).Map(-0.6F, 1, 0, 1);
        usageSum += fingerUsage;
        averagePosition = new Leap.Vector(averagePosition.x + distalFingerBoneTip.x * fingerUsage,
                                          averagePosition.y + distalFingerBoneTip.y * fingerUsage,
                                          averagePosition.z + distalFingerBoneTip.z * fingerUsage);
        averageDirection = new Leap.Vector(averageDirection.x + medialFingerBoneDirection.x * fingerUsage,
                                           averageDirection.y + medialFingerBoneDirection.y * fingerUsage,
                                           averageDirection.z + medialFingerBoneDirection.z * fingerUsage);
      }

      // Distal Intention Ray: Add punch vector for when finger usage is low (closed fist).
      Vector3 distalAxis = _hand.DistalAxis();
      float punchWeight = usageSum.Map(0F, 1F, 1F, 0F);
      averagePosition = new Leap.Vector(averagePosition.x + _hand.Fingers[2].bones[1].PrevJoint.x * punchWeight,
                                         averagePosition.y + _hand.Fingers[2].bones[1].PrevJoint.y * punchWeight,
                                         averagePosition.z + _hand.Fingers[2].bones[1].PrevJoint.z * punchWeight);
      averageDirection = new Leap.Vector(averageDirection.x + distalAxis.x * punchWeight,
                                         averageDirection.y + distalAxis.y * punchWeight,
                                         averageDirection.z + distalAxis.z * punchWeight);
      usageSum += punchWeight;

      // Finalize weighted average for Distal Intention Ray
      _intentionPosition = new Vector3(averagePosition.x / usageSum,
                                        averagePosition.y / usageSum,
                                        averagePosition.z / usageSum);
    }

    #region Primary Hover -- Old Raycasting Method (TODO: Delete)

    //private const float INTENTION_SPHERE_RADIUS = 0.02F;
    //private InteractionBehaviourBase _primaryHoverObj = null;
    //private Vector3 _primaryHoverCastPosition = Vector3.zero;

    //private Vector3[] _intentionRayDirections = new Vector3[9];

    //private void CalculateIntentionRays() {
    //  if (_hand == null) {
    //    _intentionRayDirections[0] = Vector3.zero;
    //    return;
    //  }

    //  // Weighted average of medial finger bone positions and directions for base (distal) intention ray
    //  float usageSum = 0F;
    //  Leap.Vector averagePosition = Leap.Vector.Zero;
    //  Leap.Vector averageDirection = Leap.Vector.Zero;
    //  for (int i = 1; i < 5; i++) {
    //    Leap.Vector distalFingerBoneTip = _hand.Fingers[i].bones[3].NextJoint;
    //    Leap.Vector medialFingerBoneDirection = _hand.Fingers[i].bones[2].Direction;
    //    Leap.Vector distalDirection = _hand.Basis.zBasis;
    //    var fingerUsage = ((medialFingerBoneDirection.x * distalDirection.x)
    //                     + (medialFingerBoneDirection.y * distalDirection.y)
    //                     + (medialFingerBoneDirection.z * distalDirection.z)).Map(-0.6F, 1, 0, 1);
    //    usageSum += fingerUsage;
    //    averagePosition = new Leap.Vector(averagePosition.x + distalFingerBoneTip.x * fingerUsage,
    //                                      averagePosition.y + distalFingerBoneTip.y * fingerUsage,
    //                                      averagePosition.z + distalFingerBoneTip.z * fingerUsage);
    //    averageDirection = new Leap.Vector(averageDirection.x + medialFingerBoneDirection.x * fingerUsage,
    //                                       averageDirection.y + medialFingerBoneDirection.y * fingerUsage,
    //                                       averageDirection.z + medialFingerBoneDirection.z * fingerUsage);
    //  }

    //  // Distal Intention Ray: Add punch vector for when finger usage is low (closed fist).
    //  Vector3 distalAxis = _hand.DistalAxis();
    //  float punchWeight = usageSum.Map(0F, 1F, 1F, 0F);
    //  averagePosition = new Leap.Vector(averagePosition.x + _hand.Fingers[2].bones[1].PrevJoint.x * punchWeight,
    //                                     averagePosition.y + _hand.Fingers[2].bones[1].PrevJoint.y * punchWeight,
    //                                     averagePosition.z + _hand.Fingers[2].bones[1].PrevJoint.z * punchWeight);
    //  averageDirection = new Leap.Vector(averageDirection.x + distalAxis.x * punchWeight,
    //                                     averageDirection.y + distalAxis.y * punchWeight,
    //                                     averageDirection.z + distalAxis.z * punchWeight);
    //  usageSum += punchWeight;

    //  // Finalize weighted average for Distal Intention Ray
    //  _intentionPosition = new Vector3(averagePosition.x / usageSum,
    //                                    averagePosition.y / usageSum,
    //                                    averagePosition.z / usageSum);
    //  _intentionRayDirections[0] = new Vector3(averageDirection.x / usageSum,
    //                                           averageDirection.y / usageSum,
    //                                           averageDirection.z / usageSum);

    //  // Calculate the other intention rays.
    //  Vector3 radialAxis = _hand.RadialAxis();
    //  _intentionRayDirections[1] = Quaternion.AngleAxis(-90F, radialAxis) * _intentionRayDirections[0];
    //  _intentionRayDirections[2] = Quaternion.AngleAxis(-45F, radialAxis) * _intentionRayDirections[0];
    //  _intentionRayDirections[3] = Quaternion.AngleAxis(45F, radialAxis) * _intentionRayDirections[0];
    //  _intentionRayDirections[4] = Quaternion.AngleAxis(90F, radialAxis) * _intentionRayDirections[0];

    //  _intentionRayDirections[5] = Quaternion.AngleAxis(66F, _intentionRayDirections[0]) * _intentionRayDirections[2];
    //  _intentionRayDirections[6] = Quaternion.AngleAxis(-66F, _intentionRayDirections[0]) * _intentionRayDirections[2];
    //  _intentionRayDirections[7] = Quaternion.AngleAxis(66F, _intentionRayDirections[0]) * _intentionRayDirections[3];
    //  _intentionRayDirections[8] = Quaternion.AngleAxis(-66F, _intentionRayDirections[0]) * _intentionRayDirections[3];
    //}

    //private void ProcessIntentionRays() {
    //  InteractionBehaviourBase primaryHoverObj = null;
    //  float primaryHoverObjDistance = float.PositiveInfinity;

    //  if (_hand != null) {
    //    Vector3 primaryHoverObjCastPosition = Vector3.zero;
    //    for (int i = 0; i < _intentionRayDirections.Length; i++) {
    //      int hitCount = CastIntentionRays(i);
    //      for (int j = 0; j < hitCount; j++) {
    //        RaycastHit hitResult = _hitResults[j];
    //        Rigidbody body = hitResult.collider.attachedRigidbody;
    //        if (body == null) continue;
    //        InteractionBehaviourBase interactionObj = body.GetComponent<InteractionBehaviourBase>();
    //        if (interactionObj == null || interactionObj.interactionManager != this._interactionManager || interactionObj.ignoreHover) continue;
    //        if (primaryHoverObj == null || hitResult.distance < primaryHoverObjDistance) {
    //          primaryHoverObj = interactionObj;
    //          primaryHoverObjDistance = hitResult.distance;
    //          primaryHoverObjCastPosition = hitResult.point;
    //        }
    //      }
    //    }
    //    _primaryHoverCastPosition = primaryHoverObjCastPosition;
    //  }

    //  _primaryHoverObj = primaryHoverObj;
    //}

    //private RaycastHit[] _hitResults = new RaycastHit[32];
    //private int CastIntentionRays(int rayDirectionIndex) {
    //  //int hitCount = Physics.RaycastNonAlloc(_intentionRayOrigin, _intentionRayDirections[rayDirectionIndex], _hitResults, MAX_PRIMARY_HOVER_DISTANCE);
    //  int hitCount = Physics.SphereCastNonAlloc(_intentionPosition, INTENTION_SPHERE_RADIUS, _intentionRayDirections[rayDirectionIndex].normalized, _hitResults, MAX_PRIMARY_HOVER_DISTANCE);
    //  if (hitCount == _hitResults.Length) {
    //    _hitResults = new RaycastHit[_hitResults.Length * 2];
    //    return CastIntentionRays(rayDirectionIndex);
    //  }
    //  else {
    //    return hitCount;
    //  }
    //}

    //private void FixedUpdatePrimaryHoverCallbacks() {
    //  if (_primaryHoverObj != null) {
    //    if (_primaryHoveredLastFrame == null) {
    //      _primaryHoverObj.PrimaryHoverBegin(_hand);
    //    }
    //    else {
    //      if (_primaryHoveredLastFrame == _primaryHoverObj) {
    //        _primaryHoverObj.PrimaryHoverStay(_hand);
    //      }
    //      else {
    //        _primaryHoveredLastFrame.PrimaryHoverEnd(_hand);
    //        _primaryHoverObj.PrimaryHoverBegin(_hand);
    //      }
    //    }
    //  }
    //  else if (_primaryHoveredLastFrame != null) {
    //    _primaryHoveredLastFrame.PrimaryHoverEnd(_hand);
    //  }

    //  _primaryHoveredLastFrame = _primaryHoverObj;
    //}

    #endregion

    #endregion

    #region Touch (common logic for Contact and Grasping)

    /// <summary>
    /// Encapsulates tracking Hand <-> InteractionBehaviour contact and grasping candidates
    /// (broad-phase) via PhysX sphere queries.
    /// </summary>
    private ActivityManager _touchActivityManager;
    public float TouchActivationRadius {
      get {
        return _touchActivityManager.activationRadius;
      }
      set {
        _touchActivityManager.activationRadius = value;
      }
    }

    private void InitTouch(float touchActivationRadius) {
      _touchActivityManager = new ActivityManager(_interactionManager, touchActivationRadius);
    }

    private void FixedUpdateTouch() {
      _touchActivityManager.FixedUpdateHand(_hand);
    }

    #endregion

    #region Grasping

    private HeuristicGrabClassifier _grabClassifier;
    private InteractionBehaviourBase _graspedObject;

    private void InitGrasping() {
      _grabClassifier = new HeuristicGrabClassifier(this);
    }

    private void FixedUpdateGrasping() {
      _grabClassifier.FixedUpdate();

      if (_graspedObject != null) {
        _graspedObject.GraspHold(_hand);
      }
    }

    public void Grasp(InteractionBehaviourBase interactionObj) {
      interactionObj.GraspBegin(_hand);
      _graspedObject = interactionObj;
    }

    public void ReleaseGrasp() {
      if (_graspedObject == null) return; // Nothing to release.

      _grabClassifier.NotifyGraspReleased(_graspedObject);
      _graspedObject.GraspEnd(_hand);
      _graspedObject = null;
    }

    public bool ReleaseObject(InteractionBehaviourBase interactionObj) {
      if (interactionObj == _graspedObject) {
        ReleaseGrasp();
        return true;
      }
      else {
        return false;
      }
    }

    public InteractionBehaviourBase GetGraspedObject() {
      return _graspedObject;
    }

    public HashSet<InteractionBehaviourBase> GetGraspCandidates() {
      return _touchActivityManager.ActiveBehaviours;
    }

    public bool IsGrasping(InteractionBehaviourBase interactionObj) {
      return _graspedObject == interactionObj;
    }

    #endregion

    #region Gizmos

    private bool _enablePrimaryHoverGizmos = false;

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      if (_enablePrimaryHoverGizmos) {
        //drawer.color = Color.red;
        //drawer.DrawLine(_intentionPosition, _intentionPosition + _intentionRayDirections[0]);

        //if (_hand != null) {
        //  drawer.color = Color.blue;
        //  for (int i = 1; i < _intentionRayDirections.Length; i++) {
        //    drawer.DrawLine(_intentionPosition, _intentionPosition + _intentionRayDirections[i]);
        //  }
        //}

        //if (_primaryHoverObj != null) {
        //  drawer.color = new Color(0.8F, 0.5F, 0.2F);
        //  drawer.DrawSphere(_primaryHoverCastPosition, 0.01F);
        //}
      }
    }

    #endregion

  }

  public partial class InteractionManager : IRuntimeGizmoComponent {

    // TODO: DELETEME
    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      foreach (var hand in _interactionHands) {
        if (hand != null) {
          hand.OnDrawRuntimeGizmos(drawer);
        }
      }
    }

  }

}