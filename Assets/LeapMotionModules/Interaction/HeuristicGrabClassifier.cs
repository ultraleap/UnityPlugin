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

    private void UpdateBehaviour(IInteractionBehaviour behaviour, Hand hand) {
      using (new ProfilerSample("Update Individual Grab Classifier",
             behaviour.gameObject)) {
        GrabClassifierHeuristics.GrabClassifier classifier;
        if (!_classifiers.TryGetValue(behaviour, out classifier)) {
          classifier = new GrabClassifierHeuristics.GrabClassifier(behaviour.gameObject);
          _classifiers.Add(behaviour, classifier);
        }

        // Do the actual grab classification logic.
        FillClassifier(hand, ref classifier);
        GrabClassifierHeuristics.UpdateClassifier(classifier, _collidingCandidates, _numberOfColliders, _scaledGrabParams);

        if (classifier.isGrabbing != classifier.prevGrabbing) {
          if (classifier.isGrabbing) {
            if (!behaviour.ignoreGrasping && !interactionHand.isGraspingObject) {
              if (behaviour.isGrasped && !behaviour.allowMultiGrasp) {
                interactionHand.interactionManager.TryReleaseObjectFromGrasp(behaviour);
              }
              interactionHand.Grasp(behaviour);
            }
          }
          else if (interactionHand.IsGrasping(behaviour)) {
            interactionHand.ReleaseGrasp();
            classifier.coolDownProgress = 0f;
          }
        }
        classifier.prevGrabbing = classifier.isGrabbing;
      }
    }

    // Classifier bookkeeping
    public void FixedUpdateHeuristicClassifier(Hand hand) {
      if (hand != null) {
        //Ensure that all scale dependent variables are properly set
        _scaledGrabParams.FINGERTIP_RADIUS = _defaultGrabParams.FINGERTIP_RADIUS
                                           * interactionHand.interactionManager.SimulationScale;
        _scaledGrabParams.THUMBTIP_RADIUS = _defaultGrabParams.THUMBTIP_RADIUS
                                          * interactionHand.interactionManager.SimulationScale;
        _scaledGrabParams.MAXIMUM_DISTANCE_FROM_HAND = _defaultGrabParams.MAXIMUM_DISTANCE_FROM_HAND
                                                     * interactionHand.interactionManager.SimulationScale;

        // Ensure that the temporally variant variables are updated
        // scaledGrabParams.LAYER_MASK = 1 << _manager.InteractionLayer;
        for (int i = 0; i < hand.Fingers.Count; i++) {
          _fingerTipPositions[i] = hand.Fingers[i].TipPosition.ToVector3();
        }

        using (new ProfilerSample("Update Hand Grab Classifiers", interactionHand.interactionManager)) {
          GrabClassifierHeuristics.UpdateAllProbeColliders(_fingerTipPositions, ref _collidingCandidates, ref _numberOfColliders, _scaledGrabParams);

          // First check if already holding an object and only process that one.
          var graspedObject = interactionHand.GetGraspedObject();
          if (graspedObject != null) {
            UpdateBehaviour(graspedObject, hand);
          }

          // Otherwise, process all objects.
          var activeObjects = interactionHand.GetGraspCandidates();
          foreach (var interactionObj in activeObjects) {
            UpdateBehaviour(interactionObj, hand);
          }
        }
      }
    }

    // TODO: Make sure the InteractionManager calls this method!! (Currently it does NOT.)
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
