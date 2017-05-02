/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System.Collections.Generic;
using InteractionEngineUtility;

namespace Leap.Unity.Interaction {
  public class HeuristicGrabClassifier {
    public InteractionManager _manager;

    Dictionary<IInteractionBehaviour, GrabClassifierHeuristics.GrabClassifier> leftGrabClassifiers = new Dictionary<IInteractionBehaviour, GrabClassifierHeuristics.GrabClassifier>();
    Dictionary<IInteractionBehaviour, GrabClassifierHeuristics.GrabClassifier> rightGrabClassifiers = new Dictionary<IInteractionBehaviour, GrabClassifierHeuristics.GrabClassifier>();
    GrabClassifierHeuristics.ClassifierParameters defaultGrabParams, scaledGrabParams;
    Collider[][] collidingCandidates = new Collider[6][];
    int[] numberOfColliders = new int[6];
    Vector3[] fingerTipPositions = new Vector3[5];


    public HeuristicGrabClassifier(InteractionManager manager, float fingerStickiness = 0f, float thumbStickiness = 0.04f, float maxCurl = 0.65f, float minCurl = -0.1f, float fingerRadius = 0.012f, float thumbRadius = 0.017f, float grabCooldown = 0.2f, float maxCurlVel = 0.0f, float maxGrabDistance = 0.05f, int layerMask = 0, QueryTriggerInteraction queryTriggers = QueryTriggerInteraction.UseGlobal) {
      _manager = manager;
      defaultGrabParams = new GrabClassifierHeuristics.ClassifierParameters(fingerStickiness, thumbStickiness, maxCurl, minCurl, fingerRadius, thumbRadius, grabCooldown, maxCurlVel, maxGrabDistance, layerMask == 0 ? (1 << manager.InteractionLayer | 1 << manager.InteractionNoClipLayer) : layerMask, queryTriggers);
      scaledGrabParams = new GrabClassifierHeuristics.ClassifierParameters(fingerStickiness, thumbStickiness, maxCurl, minCurl, fingerRadius, thumbRadius, grabCooldown, maxCurlVel, maxGrabDistance, layerMask == 0 ? (1 << manager.InteractionLayer | 1 << manager.InteractionNoClipLayer) : layerMask, queryTriggers);
      for (int i = 0; i < 6; i++) { collidingCandidates[i] = new Collider[5]; }
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
        GrabClassifierHeuristics.UpdateClassifier(classifier, collidingCandidates, numberOfColliders, scaledGrabParams);

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
        scaledGrabParams.FINGERTIP_RADIUS = defaultGrabParams.FINGERTIP_RADIUS * _manager.SimulationScale;
        scaledGrabParams.THUMBTIP_RADIUS = defaultGrabParams.THUMBTIP_RADIUS * _manager.SimulationScale;
        scaledGrabParams.MAXIMUM_DISTANCE_FROM_HAND = defaultGrabParams.MAXIMUM_DISTANCE_FROM_HAND * _manager.SimulationScale;

        //Ensure that the temporally variant variables are updated
        //scaledGrabParams.LAYER_MASK = 1<<_manager.InteractionLayer;
        for (int i = 0; i < hand.Fingers.Count; i++) {
          fingerTipPositions[i] = hand.Fingers[i].TipPosition.ToVector3();
        }

        using (new ProfilerSample("Update All Grab Classifiers", _manager)) {
          GrabClassifierHeuristics.UpdateAllProbeColliders(fingerTipPositions, ref collidingCandidates, ref numberOfColliders, scaledGrabParams);

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
      }
    }
  }
}
