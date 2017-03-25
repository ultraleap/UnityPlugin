using InteractionEngineUtility;
using Leap.Unity.RuntimeGizmos;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public class HeuristicGrabClassifier {

    public InteractionHand interactionHand;

    private Collider[] _collidingCandidates = new Collider[10];
    Dictionary<InteractionBehaviourBase, GrabClassifierHeuristics.GrabClassifier> _classifiers
      = new Dictionary<InteractionBehaviourBase, GrabClassifierHeuristics.GrabClassifier>();
    GrabClassifierHeuristics.ClassifierParameters _defaultGrabParams, _scaledGrabParams;

    public HeuristicGrabClassifier(InteractionHand intHand,
                                   float fingerStickiness = 0F,
                                   float thumbStickiness = 0.04F,
                                   float maxCurl = 0.65F,
                                   float minCurl = -0.1F,
                                   float fingerRadius = 0.012F,
                                   float thumbRadius = 0.017F,
                                   float grabCooldown = 0.2F,
                                   float maxCurlVel = 0.0F,
                                   float maxGrabDistance = 0.05F) {
      interactionHand = intHand;
      _defaultGrabParams = new GrabClassifierHeuristics.ClassifierParameters(
        fingerStickiness, thumbStickiness, maxCurl, minCurl, fingerRadius,
        thumbRadius, grabCooldown, maxCurlVel, maxGrabDistance);
      _scaledGrabParams = new GrabClassifierHeuristics.ClassifierParameters(
        fingerStickiness, thumbStickiness, maxCurl, minCurl, fingerRadius,
        thumbRadius, grabCooldown, maxCurlVel, maxGrabDistance);
    }

    private void UpdateBehaviour(InteractionBehaviourBase behaviour, Hand hand) {
      using (new ProfilerSample("Update Individual Grab Classifier",
             behaviour.gameObject)) {
        GrabClassifierHeuristics.GrabClassifier classifier;
        if (!_classifiers.TryGetValue(behaviour, out classifier)) {
          classifier = new GrabClassifierHeuristics.GrabClassifier(behaviour.gameObject);
          _classifiers.Add(behaviour, classifier);
        }

        // Do the actual grab classification logic.
        FillClassifier(hand, ref classifier);
        GrabClassifierHeuristics.UpdateClassifier(classifier, _collidingCandidates, _scaledGrabParams);

        if (classifier.isGrabbing != classifier.prevGrabbing) {
          if (classifier.isGrabbing) {
            if (!behaviour.allowMultiGrasp) {
              interactionHand.interactionManager.ReleaseObjectFromGrasp(behaviour);
            }
            interactionHand.Grasp(behaviour);
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

        using (new ProfilerSample("Update Hand Grab Classifiers", interactionHand.interactionManager)) {
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
    public void UnregisterInteractionBehaviour(InteractionBehaviourBase behaviour) {
      _classifiers.Remove(behaviour);
    }

    public void NotifyGraspReleased(InteractionBehaviourBase behaviour) {
      GrabClassifierHeuristics.GrabClassifier classifier;
      if (_classifiers.TryGetValue(behaviour, out classifier)) {
        classifier.isGrabbing = false;
        classifier.coolDownProgress = 0F;
        //for (int i = 0; i < classifier.probes.Length; i++) {
        //  classifier.probes[i].isInside = false;
        //}
      }
    }

    public void GetGraspingFingertipPositions(InteractionBehaviourBase behaviour, Vector3[] fingertipPositionsBuffer, out int numGraspingFingertips) {
      GrabClassifierHeuristics.GrabClassifier classifier;
      if (_classifiers.TryGetValue(behaviour, out classifier)) {
        int writeIdx = 0;
        for (int probeIdx = 0; probeIdx < classifier.probes.Length; probeIdx++) {
          fingertipPositionsBuffer[writeIdx++] = classifier.probes[probeIdx].position;
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
        classifier.probes[i].position = hand.Fingers[i].TipPosition.ToVector3();
        classifier.probes[i].direction = hand.Fingers[i].Direction.ToVector3();
      }
    }

  }

}
