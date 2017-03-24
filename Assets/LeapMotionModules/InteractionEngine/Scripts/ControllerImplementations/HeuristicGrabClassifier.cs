using UnityEngine;
using System.Collections.Generic;
using InteractionEngineUtility;

namespace Leap.Unity.Interaction {
  public class HeuristicGrabClassifier {
    Collider[] collidingCandidates = new Collider[10];
    public InteractionManager _manager;

    Dictionary<IInteractionBehaviour, GrabClassifierHeuristics.GrabClassifier> leftGrabClassifiers = new Dictionary<IInteractionBehaviour, GrabClassifierHeuristics.GrabClassifier>();
    Dictionary<IInteractionBehaviour, GrabClassifierHeuristics.GrabClassifier> rightGrabClassifiers = new Dictionary<IInteractionBehaviour, GrabClassifierHeuristics.GrabClassifier>();
    GrabClassifierHeuristics.ClassifierParameters grabParams;


    public HeuristicGrabClassifier(InteractionManager manager, float fingerStickiness = 0f, float thumbStickiness = 0.04f, float maxCurl = 0.65f, float minCurl = -0.1f, float fingerRadius = 0.012f, float thumbRadius = 0.017f, float grabCooldown = 0.2f, float maxCurlVel = 0.0f, float maxGrabDistance = 0.04f) {
      _manager = manager;
      grabParams = new GrabClassifierHeuristics.ClassifierParameters(fingerStickiness, thumbStickiness, maxCurl, minCurl, fingerRadius * _manager.SimulationScale, thumbRadius * _manager.SimulationScale, grabCooldown, maxCurlVel, maxGrabDistance * _manager.SimulationScale);
    }

    public void UpdateBehaviour(IInteractionBehaviour behaviour, Hand _hand) {
      using (new ProfilerSample("Update Individual Classifier", behaviour.gameObject)) {
        GrabClassifierHeuristics.GrabClassifier classifier;
        Dictionary<IInteractionBehaviour, GrabClassifierHeuristics.GrabClassifier> classifiers = (_hand.IsLeft ? leftGrabClassifiers : rightGrabClassifiers);
        if (!classifiers.TryGetValue(behaviour, out classifier)) {
          classifier = new GrabClassifierHeuristics.GrabClassifier(behaviour.gameObject);
          classifiers.Add(behaviour, classifier);
        }

        //Do the actual grab classification logic
        fillClassifier(_hand, ref classifier);
        GrabClassifierHeuristics.UpdateClassifier(classifier, collidingCandidates, grabParams);

        if (classifier.isGrabbing != classifier.prevGrabbing) {
          if (classifier.isGrabbing) {
            if (!_manager.TwoHandedGrasping) { _manager.ReleaseObject(behaviour); }
            _manager.GraspWithHand(_hand, behaviour);
          } else if (behaviour.IsBeingGraspedByHand(_hand.Id)) {
            _manager.ReleaseHand(_hand.Id);
            classifier.coolDownProgress = 0f;
          }
        }
        classifier.prevGrabbing = classifier.isGrabbing;
      }
    }

    //Classifier bookkeeping
    public void UpdateHeuristicClassifier(Hand hand) {
      if (hand != null) {
        //Ensure that all scale dependent variables are properly set
        grabParams.FINGERTIP_RADIUS = 0.12f * _manager.SimulationScale;
        grabParams.THUMBTIP_RADIUS = 0.017f * _manager.SimulationScale;
        grabParams.MAXIMUM_DISTANCE_FROM_HAND = 0.04f * _manager.SimulationScale;

        using (new ProfilerSample("Update All Grab Classifiers", _manager)) {
          //First check if already holding an object and only process that one
          var graspedBehaviours = _manager.GraspedObjects;
          for (int i = 0; i < graspedBehaviours.Count; i++) {
            if (graspedBehaviours[i].IsBeingGraspedByHand(hand.Id)) {
              UpdateBehaviour(graspedBehaviours[i], hand);
              return;
            }
          }

          //If not, process all objects
          var activeBehaviours = _manager._activityManager.ActiveBehaviours;
          for (int i = 0; i < activeBehaviours.Count; i++) {
            UpdateBehaviour(activeBehaviours[i], hand);
          }
        }
      }
    }

    public void UnregisterInteractionBehaviour(IInteractionBehaviour behaviour) {
      leftGrabClassifiers.Remove(behaviour);
      rightGrabClassifiers.Remove(behaviour);
    }

    protected void fillClassifier(Hand _hand, ref GrabClassifierHeuristics.GrabClassifier classifier) {
      classifier.handChirality = _hand.IsLeft;
      classifier.handDirection = _hand.Direction.ToVector3();
      classifier.handXBasis = _hand.Basis.xBasis.ToVector3();
      classifier.handGrabCenter = (_hand.PalmPosition + (_hand.Direction * 0.05f * _manager.SimulationScale) + (_hand.PalmNormal * 0.01f * _manager.SimulationScale)).ToVector3();
      for(int i = 0; i<_hand.Fingers.Count; i++) {
        classifier.probes[i].direction = _hand.Fingers[i].Direction.ToVector3();
        classifier.probes[i].position = _hand.Fingers[i].TipPosition.ToVector3();
        classifier.probes[i].direction = _hand.Fingers[i].Direction.ToVector3();
      }
    }
  }
}
