/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Interaction.Internal.InteractionEngineUtility;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Leap.Unity.Interaction.Internal {

  public class HeuristicGrabClassifier {

    public InteractionHand interactionHand;

    private Dictionary<IInteractionBehaviour, GrabClassifierHeuristics.GrabClassifier> _classifiers
      = new Dictionary<IInteractionBehaviour, GrabClassifierHeuristics.GrabClassifier>();
    private GrabClassifierHeuristics.ClassifierParameters _defaultGrabParams, _scaledGrabParams;
    private Collider[][] _collidingCandidates = new Collider[6][];
    private int[] _numberOfColliders = new int[6];
    private Vector3[] _fingerTipPositions = new Vector3[5];
    private Vector3[] _fingerKnucklePositions = new Vector3[5];

    public HeuristicGrabClassifier(InteractionHand intHand,
                                   float fingerStickiness = 0F,
                                   float thumbStickiness = 0.04F,
                                   float maxCurl = 0.65F,
                                   float minCurl = -0.1F,
                                   float fingerRadius = 0.012F,
                                   float thumbRadius = 0.017F,
                                   float grabCooldown = 0.2F,
                                   float maxCurlVel = 0.0F,
                                   float grabbedMaxCurlVel = -0.025F,
                                   float maxGrabDistance = 0.05F,
                                   int layerMask = 0,
                                   QueryTriggerInteraction queryTriggers = QueryTriggerInteraction.UseGlobal) {

      interactionHand = intHand;
      _defaultGrabParams = new GrabClassifierHeuristics.ClassifierParameters(
        fingerStickiness, thumbStickiness, maxCurl, minCurl, fingerRadius,
        thumbRadius, grabCooldown, maxCurlVel, grabbedMaxCurlVel, maxGrabDistance,
        layerMask == 0 ? interactionHand.manager.GetInteractionLayerMask() : layerMask,
        queryTriggers);
      _scaledGrabParams = new GrabClassifierHeuristics.ClassifierParameters(
        fingerStickiness, thumbStickiness, maxCurl, minCurl, fingerRadius,
        thumbRadius, grabCooldown, maxCurlVel, grabbedMaxCurlVel, maxGrabDistance,
        layerMask == 0 ? interactionHand.manager.GetInteractionLayerMask() : layerMask,
        queryTriggers);

      for (int i = 0; i < _collidingCandidates.Length; i++) {
          _collidingCandidates[i] = new Collider[5];
        }
    }

    public void FixedUpdateClassifierHandState(Transform headTransform = null) {
      using (new ProfilerSample("Update Classifier Hand State")) {
        var hand = interactionHand.leapHand;

        if (interactionHand.isTracked) {
          // Ensure that all scale dependent variables are properly set.
          _scaledGrabParams.FINGERTIP_RADIUS = _defaultGrabParams.FINGERTIP_RADIUS
                                             * interactionHand.manager.SimulationScale;
          _scaledGrabParams.THUMBTIP_RADIUS = _defaultGrabParams.THUMBTIP_RADIUS
                                            * interactionHand.manager.SimulationScale;
          _scaledGrabParams.MAXIMUM_DISTANCE_FROM_HAND = _defaultGrabParams.MAXIMUM_DISTANCE_FROM_HAND
                                                       * interactionHand.manager.SimulationScale;

          // Ensure layer mask is up-to-date.
          _scaledGrabParams.LAYER_MASK = interactionHand.manager.GetInteractionLayerMask();


          /*if (headTransform != null) {
            //SERIOUS HACKS: Have the collider for the grab classifier stretch along the camera's projective axis
            //This will make it easier for users to grab far away objects (while being uncertain of their depth)
            for (int i = 0; i < hand.Fingers.Count; i++) {
              Vector3 fingerTipPosition = hand.Fingers[i].TipPosition.ToVector3();
              Vector3 stretchDirection = (fingerTipPosition - headTransform.position) * 0.1f;
              _fingerTipPositions[i] = fingerTipPosition + stretchDirection;
              _fingerKnucklePositions[i] = fingerTipPosition - stretchDirection;
            }
          } else {*/
            for (int i = 0; i < hand.Fingers.Count; i++) {
              _fingerTipPositions[i] = hand.Fingers[i].TipPosition.ToVector3();
              _fingerKnucklePositions[i] = hand.Fingers[i].Bone(Bone.BoneType.TYPE_METACARPAL).NextJoint.ToVector3();
            }
         // }

          GrabClassifierHeuristics.UpdateAllProbeColliders(_fingerTipPositions, _fingerKnucklePositions, ref _collidingCandidates, ref _numberOfColliders, _scaledGrabParams);
        }
      }
    }

    public bool FixedUpdateClassifierGrasp(out IInteractionBehaviour graspedObject) {
      using (new ProfilerSample("Update Grab Classifier - Grasp", interactionHand.manager)) {
        graspedObject = null;
        if (interactionHand.isGraspingObject || !interactionHand.isTracked) {
          // Cannot grasp another object with an untracked hand or while the hand is
          // already grasping an object or if the hand is not tracked.
          return false;
        }

        foreach (var interactionObj in interactionHand.graspCandidates) {
          if (updateBehaviour(interactionObj, interactionHand.leapHand, graspMode: GraspUpdateMode.BeginGrasp)) {
            graspedObject = interactionObj;
            return true;
          }
        }

        return false;
      }
    }

    public bool FixedUpdateClassifierRelease(out IInteractionBehaviour releasedObject) {
      using (new ProfilerSample("Update Grab Classifier - Release", interactionHand.manager)) {
        releasedObject = null;
        if (!interactionHand.isGraspingObject) {
          // Can't release an object if the hand is already not grasping one.
          return false;
        }

        if (updateBehaviour(interactionHand.graspedObject, interactionHand.leapHand, graspMode: GraspUpdateMode.ReleaseGrasp)) {
          releasedObject = interactionHand.graspedObject;
          return true;
        }

        return false;
      }
    }

    private enum GraspUpdateMode {
      BeginGrasp,
      ReleaseGrasp
    }

    /// <summary>
    /// Returns true if the behaviour reflects the state-change (grasped or released) as specified by
    /// the graspMode.
    /// </summary>
    private bool updateBehaviour(IInteractionBehaviour behaviour, Hand hand, GraspUpdateMode graspMode, bool ignoreTemporal = false) {
      using (new ProfilerSample("Update Individual Grab Classifier", behaviour.gameObject)) {
        // Ensure a classifier exists for this Interaction Behaviour.
        GrabClassifierHeuristics.GrabClassifier classifier;
        if (!_classifiers.TryGetValue(behaviour, out classifier)) {
          classifier = new GrabClassifierHeuristics.GrabClassifier(behaviour.gameObject);
          _classifiers.Add(behaviour, classifier);
        }

        // Do the actual grab classification logic.
        FillClassifier(behaviour, hand, ref classifier);
        GrabClassifierHeuristics.UpdateClassifier(classifier, _scaledGrabParams,
                                                              ref _collidingCandidates,
                                                              ref _numberOfColliders,
                                                              ignoreTemporal);

        // Determine whether there was a state change.
        bool didStateChange = false;
        if (!classifier.prevThisControllerGrabbing && classifier.isThisControllerGrabbing
          && graspMode == GraspUpdateMode.BeginGrasp) {
          didStateChange = true;

          classifier.prevThisControllerGrabbing = classifier.isThisControllerGrabbing;
        }
        else if (classifier.prevThisControllerGrabbing && !classifier.isThisControllerGrabbing
                 && interactionHand.graspedObject == behaviour && graspMode == GraspUpdateMode.ReleaseGrasp) {
          didStateChange = true;

          classifier.coolDownProgress = 0f;
          classifier.prevThisControllerGrabbing = classifier.isThisControllerGrabbing;
        }

        return didStateChange;
      }
    }

    public void UnregisterInteractionBehaviour(IInteractionBehaviour behaviour) {
      _classifiers.Remove(behaviour);
    }

    public void NotifyGraspForciblyReleased(IInteractionBehaviour behaviour) {
      GrabClassifierHeuristics.GrabClassifier classifier;
      if (_classifiers.TryGetValue(behaviour, out classifier)) {
        classifier.prevThisControllerGrabbing = false;
        classifier.isThisControllerGrabbing = false;
        classifier.coolDownProgress = 0F;
        for (int i = 0; i < classifier.probes.Length; i++) {
          classifier.probes[i].isInside = false;
        }
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

    public bool TryGrasp(IInteractionBehaviour intObj, Hand hand) {
      Debug.Log("HEY THIS SHOULDN'T BE BEING CALLED");
      FixedUpdateClassifierHandState();

      return updateBehaviour(intObj, hand, GraspUpdateMode.BeginGrasp,
                             ignoreTemporal: true);
    }

    public void SwapClassifierState(IInteractionBehaviour original, IInteractionBehaviour replacement) {
      if (original == null) {
        throw new ArgumentNullException("original");
      }

      if (replacement == null) {
        throw new ArgumentNullException("replacement");
      }

      GrabClassifierHeuristics.GrabClassifier classifier;
      if (!_classifiers.TryGetValue(original, out classifier)) {
        throw new InvalidOperationException("Cannot swap from something that is not currently grasped!");
      }

      classifier.body = replacement.rigidbody;
      classifier.transform = replacement.transform;

      _classifiers.Remove(original);
      _classifiers[replacement] = classifier;
    }

    protected void FillClassifier(IInteractionBehaviour behaviour, Hand hand, ref GrabClassifierHeuristics.GrabClassifier classifier) {
      classifier.handChirality = hand.IsLeft;
      classifier.handDirection = hand.Direction.ToVector3();
      classifier.handXBasis = hand.Basis.xBasis.ToVector3();
      float simScale = interactionHand.manager.SimulationScale;
      classifier.handGrabCenter = (hand.PalmPosition
                                   + (hand.Direction * 0.05f * simScale)
                                   + (hand.PalmNormal * 0.01f * simScale)).ToVector3();
      for (int i = 0; i < hand.Fingers.Count; i++) {
        classifier.probes[i].direction = hand.Fingers[i].Direction.ToVector3();
      }
      classifier.isGrabbed = behaviour.isGrasped;
    }

  }

}
