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

    public Hand GetHand() {
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

    private void InitHovering(float hoverActivationRadius) {
      _hoverActivityManager = new ActivityManager(_interactionManager, hoverActivationRadius);
    }

    private struct HoverCheckResults {
      public HashSet<InteractionBehaviourBase> hovered;
      public InteractionBehaviourBase primaryHovered;
      public float primaryHoveredScore;
      public Hand checkedHand;
    }

    private HashSet<InteractionBehaviourBase> _hoveredLastFrame = new HashSet<InteractionBehaviourBase>();
    private InteractionBehaviourBase _primaryHoveredLastFrame;

    private void FixedUpdateHovering() {
      _hoverActivityManager.FixedUpdateHand(_hand);

      HoverCheckResults hoverResults = CheckHoverForHand(_hand, _hoverActivityManager.ActiveBehaviours);
      ProcessHoverCheckResults(hoverResults);
      ProcessPrimaryHoverCheckResults(hoverResults);
    }

    private HashSet<InteractionBehaviourBase> _hoverableCache = new HashSet<InteractionBehaviourBase>();
    private HoverCheckResults CheckHoverForHand(Hand hand, HashSet<InteractionBehaviourBase> hoverCandidates) {
      _hoverableCache.Clear();
      HoverCheckResults results = new HoverCheckResults() { hovered = _hoverableCache,
                                                            primaryHovered = null,
                                                            primaryHoveredScore = float.NegativeInfinity,
                                                            checkedHand = hand };
      foreach (var interactionObj in hoverCandidates) {
        results = CheckHoverForElement(hand, interactionObj, results);
      }

      return results;
    }

    private HoverCheckResults CheckHoverForElement(Hand hand, InteractionBehaviourBase hoverable, HoverCheckResults curResults) {
      float score = hoverable.GetHoverScore(hand);
      if (score > 0F) {
        curResults.hovered.Add(hoverable);
      }
      if (score > curResults.primaryHoveredScore) {
        curResults.primaryHovered = hoverable;
        curResults.primaryHoveredScore = score;
      }
      return curResults;
    }

    private void ProcessHoverCheckResults(HoverCheckResults hoverResults) {
      foreach (var hoverable in _hoverActivityManager.ActiveBehaviours) {
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

  }

}