using InteractionEngineUtility;
using Leap.Unity.RuntimeGizmos;
using Leap.Unity.UI.Interaction.Internal;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public class HeuristicGrabClassifier {

    public InteractionHand interactionHand;

    private Dictionary<IInteractionBehaviour, GrabClassifierHeuristics.GrabClassifier> _classifiers
      = new Dictionary<IInteractionBehaviour, GrabClassifierHeuristics.GrabClassifier>();
    private GrabClassifierHeuristics.ClassifierParameters _defaultGrabParams, _scaledGrabParams;
    private Collider[][] _collidingCandidates = new Collider[6][];
    private int[] _numberOfColliders = new int[6];
    private Vector3[] _fingerTipPositions = new Vector3[5];

    public HeuristicGrabClassifier(InteractionHand intHand,
                                   float fingerStickiness = 0F,
                                   float thumbStickiness = 0.04F,
                                   float maxCurl = 0.65F,
                                   float minCurl = -0.1F,
                                   float fingerRadius = 0.012F,
                                   float thumbRadius = 0.017F,
                                   float grabCooldown = 0.2F,
                                   float maxCurlVel = 0.0F,
                                   float maxGrabDistance = 0.05F,
                                   int layerMask = 0,
                                   QueryTriggerInteraction queryTriggers = QueryTriggerInteraction.UseGlobal) {
      interactionHand = intHand;
      _defaultGrabParams = new GrabClassifierHeuristics.ClassifierParameters(
        fingerStickiness, thumbStickiness, maxCurl, minCurl, fingerRadius,
        thumbRadius, grabCooldown, maxCurlVel, maxGrabDistance,
        layerMask == 0 ? (interactionHand.interactionManager.interactionLayer.layerMask
                          | interactionHand.interactionManager.interactionNoContactLayer.layerMask) : layerMask,
        queryTriggers);
      _scaledGrabParams = new GrabClassifierHeuristics.ClassifierParameters(
        fingerStickiness, thumbStickiness, maxCurl, minCurl, fingerRadius,
        thumbRadius, grabCooldown, maxCurlVel, maxGrabDistance,
        layerMask == 0 ? (interactionHand.interactionManager.interactionLayer.layerMask
                          | interactionHand.interactionManager.interactionNoContactLayer.layerMask) : layerMask,
        queryTriggers);

        for (int i = 0; i < 6; i++) {
          _collidingCandidates[i] = new Collider[5];
        }
    }

    public void FixedUpdateClassifierHandState() {
      using (new ProfilerSample("Update Classifier Hand State")) {
        var hand = interactionHand.GetLastTrackedLeapHand();
        if (interactionHand.isTracked) {
          // Ensure that all scale dependent variables are properly set.
          _scaledGrabParams.FINGERTIP_RADIUS = _defaultGrabParams.FINGERTIP_RADIUS
                                             * interactionHand.interactionManager.SimulationScale;
          _scaledGrabParams.THUMBTIP_RADIUS = _defaultGrabParams.THUMBTIP_RADIUS
                                            * interactionHand.interactionManager.SimulationScale;
          _scaledGrabParams.MAXIMUM_DISTANCE_FROM_HAND = _defaultGrabParams.MAXIMUM_DISTANCE_FROM_HAND
                                                       * interactionHand.interactionManager.SimulationScale;
      
          // Ensure that the temporally variant variables are updated.
          // scaledGrabParams.LAYER_MASK = 1 << _manager.InteractionLayer;
          for (int i = 0; i < hand.Fingers.Count; i++) {
            _fingerTipPositions[i] = hand.Fingers[i].TipPosition.ToVector3();
          }
        }
      }
    }

    public bool FixedUpdateClassifierGrasp(out IInteractionBehaviour graspedObject) {
      using (new ProfilerSample("Update Grab Classifier - Grasp", interactionHand.interactionManager)) {
        graspedObject = null;
        if (interactionHand.isGraspingObject || interactionHand.GetLeapHand() == null) {
          // Cannot grasp another object with an untracked hand or while the hand is already grasping an object.
          return false;
        }
        
        foreach (var interactionObj in interactionHand.graspCandidates) {
          IInteractionBehaviour _;
          if (UpdateBehaviour(interactionObj, interactionHand.GetLeapHand(), out graspedObject, out _)) {
            return true;
          }
        }

        return false;
      }
    }

    public bool FixedUpdateClassifierRelease(out IInteractionBehaviour releasedObject) {
      using (new ProfilerSample("Update Grab Classifier - Release", interactionHand.interactionManager)) {
        releasedObject = null;
        if (!interactionHand.isGraspingObject) {
          // Can't release an object if the hand is already not grasping one.
          return false;
        }

        IInteractionBehaviour _;
        if (UpdateBehaviour(interactionHand.graspedObject, interactionHand.GetLastTrackedLeapHand(), out _, out releasedObject)) {
          return true;
        }

        return false;
      }
    }

    /// <summary>
    /// Returns true if this update resulted in a grasp state change, false otherwise.
    /// 
    /// Only one of graspedObject or releasedObject will be non-null per call to this method.
    /// </summary>
    private bool UpdateBehaviour(IInteractionBehaviour behaviour, Hand hand, out IInteractionBehaviour graspedObject, out IInteractionBehaviour releasedObject) {
      using (new ProfilerSample("Update Individual Grab Classifier", behaviour.gameObject)) {
        graspedObject = null;
        releasedObject = null;

        // Ensure a classifier exists for this Interaction Behaviour.
        GrabClassifierHeuristics.GrabClassifier classifier;
        if (!_classifiers.TryGetValue(behaviour, out classifier)) {
          classifier = new GrabClassifierHeuristics.GrabClassifier(behaviour.gameObject);
          _classifiers.Add(behaviour, classifier);
        }

        // Do the actual grab classification logic.
        FillClassifier(hand, ref classifier);
        GrabClassifierHeuristics.UpdateClassifier(classifier, _collidingCandidates,
                                                              _numberOfColliders,
                                                              _scaledGrabParams);

        // Determine whether there was a state change.
        bool didStateChange = false;
        if (classifier.isGrabbing != classifier.prevGrabbing) {
          didStateChange = true;
          if (classifier.isGrabbing) {
            if (!behaviour.ignoreGrasping && !interactionHand.isGraspingObject) {
              graspedObject = behaviour;
            }
          }
          else if (interactionHand.graspedObject == behaviour) {
            releasedObject = behaviour;
            classifier.coolDownProgress = 0f;
          }
        }
        classifier.prevGrabbing = classifier.isGrabbing;
        return didStateChange;
      }
    }

    public void UnregisterInteractionBehaviour(IInteractionBehaviour behaviour) {
      _classifiers.Remove(behaviour);
    }

    public void NotifyGraspReleased(IInteractionBehaviour behaviour) {
      GrabClassifierHeuristics.GrabClassifier classifier;
      if (_classifiers.TryGetValue(behaviour, out classifier)) {
        classifier.isGrabbing = false;
        classifier.coolDownProgress = 0F;
        //for (int i = 0; i < classifier.probes.Length; i++) {
        //  classifier.probes[i].isInside = false;
        //}
      }
    }

    public void GetGraspingFingertipPositions(IInteractionBehaviour behaviour, Vector3[] fingertipPositionsBuffer, out int numGraspingFingertips) {
      GrabClassifierHeuristics.GrabClassifier classifier;
      if (_classifiers.TryGetValue(behaviour, out classifier)) {
        int writeIdx = 0;
        for (int probeIdx = 0; probeIdx < classifier.probes.Length; probeIdx++) {
          if (classifier.probes[probeIdx].isInside) {
            fingertipPositionsBuffer[writeIdx++] = _fingerTipPositions[probeIdx];
          }
        }
        numGraspingFingertips = writeIdx;
      }
      else {
        numGraspingFingertips = 0;
      }
    }

    protected void FillClassifier(Hand hand, ref GrabClassifierHeuristics.GrabClassifier classifier) {
      classifier.handChirality = hand.IsLeft;
      classifier.handDirection = hand.Direction.ToVector3();
      classifier.handXBasis = hand.Basis.xBasis.ToVector3();
      float simScale = interactionHand.interactionManager.SimulationScale;
      classifier.handGrabCenter = (hand.PalmPosition
                                   + (hand.Direction * 0.05f * simScale)
                                   + (hand.PalmNormal * 0.01f * simScale)).ToVector3();
      for (int i = 0; i < hand.Fingers.Count; i++) {
        classifier.probes[i].direction = hand.Fingers[i].Direction.ToVector3();
      }
    }

  }

}
