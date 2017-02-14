using UnityEngine;
using System;
using System.Collections.Generic;
using Leap.Unity.RuntimeGizmos;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.UI.Interaction {

  public class HeuristicGrabClassifier {

    public InteractionHand interactionHand;

    private Hand _hand;
    private Collider[][] _collidingCandidates = new Collider[5][];

    Dictionary<InteractionBehaviourBase, GrabClassifier> classifiers = new Dictionary<InteractionBehaviourBase, GrabClassifier>();

    public HeuristicGrabClassifier(InteractionHand interactionHand) {
      this.interactionHand = interactionHand;

      for(int i = 0; i < 5; i++) {
        _collidingCandidates[i] = new Collider[10];
      }
    }

    List<InteractionBehaviourBase> _keyRemovalCache = new List<InteractionBehaviourBase>();
    public void FixedUpdate() {
      _hand = interactionHand.GetLeapHand();

      if (_hand != null) {
        UpdateContactingColliders();

        //First check if already holding an object and only process that one
        var graspedObject = interactionHand.GetGraspedObject();
        if (graspedObject != null) {
          UpdateBehaviour(graspedObject);
        }
        else {
          //If not, process all objects
          var graspCandidates = interactionHand.GetGraspCandidates();
          foreach (var graspCandidate in graspCandidates) {
            if (!graspCandidate.ignoreGrasping) {
              UpdateBehaviour(graspCandidate);
            }
          }

          // Clear classifiers on non-grasp-candidates.
          _keyRemovalCache.Clear();
          foreach (var objClassifierPair in classifiers) {
            if (!graspCandidates.Contains(objClassifierPair.Key)) {
              _keyRemovalCache.Add(objClassifierPair.Key);
            }
          }
          foreach (var obj in _keyRemovalCache) {
            classifiers.Remove(obj);
          }
        }
      }
    }

    private void UpdateBehaviour(InteractionBehaviourBase behaviour) {
      GrabClassifier classifier;
      if (!classifiers.TryGetValue(behaviour, out classifier)) {
        classifier = new GrabClassifier(behaviour);
        classifiers.Add(behaviour, classifier);
      }

      //Do the actual grab classification logic
      UpdateClassifier(classifier);

      if (classifier.isGrabbing != classifier.prevGrabbing) {
        if (classifier.isGrabbing) {
          if (!behaviour.allowsTwoHandedGrasp__curIgnored) { interactionHand.ReleaseObject(behaviour); }
          interactionHand.Grasp(behaviour);
        }
        else if (!classifier.isGrabbing || interactionHand.IsGrasping(behaviour)) {
          interactionHand.ReleaseGrasp();
        }
      }
      classifier.prevGrabbing = classifier.isGrabbing;
    }

    //Grab Classifier Logic
    private void UpdateClassifier(GrabClassifier classifier) {
      classifier.warpTrans = Matrix4x4.TRS(classifier.warper.RigidbodyPosition, classifier.warper.RigidbodyRotation, classifier.transform.localScale);

      //For each probe (fingertip)
      for (int j = 0; j < classifier.probes.Length; j++) {
        //Calculate how extended the finger is
        float tempCurl = Vector3.Dot(_hand.Fingers[j].Direction.ToVector3(), (j != 0) ? _hand.Direction.ToVector3() : (_hand.IsLeft ? 1f : -1f) * _hand.Basis.xBasis.ToVector3());

        //Determine if this probe is intersecting an object
        bool collidingWithObject = false;
        foreach (Collider col in _collidingCandidates[j]) {
          if (col != null && col.attachedRigidbody != null) {
            collidingWithObject = (col.attachedRigidbody == classifier.body) ? true : collidingWithObject;
          }
        }

        //Nullify above findings if fingers are extended
        bool tempIsInside = collidingWithObject && (tempCurl < 0.65f);

        //Probes go inside when they intersect, probes come out when they uncurl
        if (!classifier.probes[j].isInside) {
          classifier.probes[j].isInside = tempIsInside;
          classifier.probes[j].curl = tempCurl + (j == 0 ? 0.15f : 0f);
        }
        else {
          if (tempCurl > classifier.probes[j].curl) {
            classifier.probes[j].isInside = tempIsInside;
          }
        }
      }

      //If thumb and one other finger is "inside" the object, it's a grab!
      classifier.isGrabbing = (classifier.probes[0].isInside && (classifier.probes[1].isInside ||
                                                                 classifier.probes[2].isInside ||
                                                                 classifier.probes[3].isInside ||
                                                                 classifier.probes[4].isInside));
      //If grabbing for 2 frames, truly register it as a grab.
      //Suppresses spurious regrabs and makes throws work better.
      if (classifier.isGrabbing) {
        if (classifier.warmUp <= 2) {
          classifier.warmUp++;
          classifier.isGrabbing = false;
        }
      }
      else {
        classifier.warmUp = 0;
      }
    }
    void UpdateContactingColliders() {
      for (int i = 0; i < _hand.Fingers.Count; i++) {
        Array.Clear(_collidingCandidates[i], 0, _collidingCandidates[i].Length);
        Physics.OverlapSphereNonAlloc(_hand.Fingers[i].TipPosition.ToVector3(), i == 0 ? 0.015f : 0.01f, _collidingCandidates[i]);
      }
    }

    public void NotifyGraspReleased(InteractionBehaviourBase interactionObj) {
      GrabClassifier classifier;
      if (classifiers.TryGetValue(interactionObj, out classifier)) {
        classifier.isGrabbing = false;
      }
    }

    //Per-Object Per-Hand Classifier
    public class GrabClassifier {
      public bool isGrabbing;
      public bool prevGrabbing;
      public GrabProbe[] probes = new GrabProbe[5];
      public Transform transform;
      public Rigidbody body;
      public RigidbodyWarper warper;
      public Matrix4x4 warpTrans;
      public int warmUp;

      public GrabClassifier(InteractionBehaviourBase behaviour) {
        probes = new GrabProbe[5];
        transform = behaviour.transform;
        body = behaviour.GetComponent<Rigidbody>();
        if (behaviour is InteractionBehaviour) warper = (behaviour as InteractionBehaviour).rigidbodyWarper;
        warmUp = 0;
      }
    }

    //Per-Finger Per-Object Probe
    public struct GrabProbe { public bool isInside; public float curl; };
  }
}
