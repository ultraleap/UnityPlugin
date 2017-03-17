using UnityEngine;
using System.Collections.Generic;

namespace Leap.Unity.Interaction {
  public class HeuristicGrabClassifier {
    //Grab Heuristic Tuning Parameters
    //-----
    //Adds hysteresis to the thumb to make it hold better
    private const float FINGER_STICKINESS = 0f;
    private const float THUMB_STICKINESS = 0.05f;
    //The minimum and maximum curl values fingers are allowed to "Grab" within
    private const float MAXIMUM_CURL = 0.65f;
    private const float MINIMUM_CURL = -0.1f;
    //The radius considered for intersection around the fingertips
    private const float FINGERTIP_RADIUS = 0.01f;
    private const float THUMBTIP_RADIUS = 0.015f;
    //The minimum amount of time between repeated grabs of a single object
    private const float GRAB_COOLDOWN = 0.2f;
    //-----

    Collider[] collidingCandidates = new Collider[10];
    public InteractionManager _manager;

    Dictionary<IInteractionBehaviour, GrabClassifier> leftGrabClassifiers = new Dictionary<IInteractionBehaviour, GrabClassifier>();
    Dictionary<IInteractionBehaviour, GrabClassifier> rightGrabClassifiers = new Dictionary<IInteractionBehaviour, GrabClassifier>();

    public HeuristicGrabClassifier(InteractionManager manager) {
      _manager = manager;
    }

    //Grab Classifier Logic
    public void UpdateClassifier(GrabClassifier classifier, Hand _hand) {
      classifier.warpTrans = Matrix4x4.TRS(classifier.warper.RigidbodyPosition, classifier.warper.RigidbodyRotation, classifier.transform.localScale);
      
      //For each probe (fingertip)
      for (int j = 0; j < classifier.probes.Length; j++) {
        //Calculate how extended the finger is
        float tempCurl = Vector3.Dot(_hand.Fingers[j].Direction.ToVector3(), (j != 0) ? _hand.Direction.ToVector3() : (_hand.IsLeft ? 1f : -1f) * _hand.Basis.xBasis.ToVector3());

        //Determine if this probe is intersecting an object
        bool collidingWithObject = false;
        int numberOfColliders = Physics.OverlapSphereNonAlloc(_hand.Fingers[j].TipPosition.ToVector3(), j == 0 ? THUMBTIP_RADIUS : FINGERTIP_RADIUS, collidingCandidates);
        for(int i = 0; i < numberOfColliders; i++) {
          if (collidingCandidates[i].attachedRigidbody != null && collidingCandidates[i].attachedRigidbody == classifier.body) {
            collidingWithObject = true;
            break;
          }
        }

        //Nullify above findings if fingers are extended
        collidingWithObject = collidingWithObject && (tempCurl < MAXIMUM_CURL) && (tempCurl > MINIMUM_CURL);

        //Probes go inside when they intersect, probes come out when they uncurl
        if (!classifier.probes[j].isInside) {
          classifier.probes[j].isInside = collidingWithObject;
          classifier.probes[j].curl = tempCurl + (j==0 ? THUMB_STICKINESS : FINGER_STICKINESS);
        } else {
          if (tempCurl > classifier.probes[j].curl) {
            classifier.probes[j].isInside = collidingWithObject;
          }
        }
      }

      //If thumb and one other finger is "inside" the object, it's a grab!
      classifier.isGrabbing = (classifier.probes[0].isInside && (classifier.probes[1].isInside ||
                                                                 classifier.probes[2].isInside ||
                                                                 classifier.probes[3].isInside ||
                                                                 classifier.probes[4].isInside));
      //If grabbing within 10 frames of releasing, discard grab.
      //Suppresses spurious regrabs and makes throws work better.
      if (classifier.coolDownProgress <= GRAB_COOLDOWN) {
        if (classifier.isGrabbing) {
          classifier.isGrabbing = false;
        }
        classifier.coolDownProgress += Time.fixedDeltaTime;
      }
    }

    public void UpdateBehaviour(IInteractionBehaviour behaviour, Hand _hand) {
      GrabClassifier classifier;
      Dictionary<IInteractionBehaviour, GrabClassifier> classifiers = (_hand.IsLeft ? leftGrabClassifiers : rightGrabClassifiers);
      if (!classifiers.TryGetValue(behaviour, out classifier)) {
        classifier = new GrabClassifier(behaviour);
        classifiers.Add(behaviour, classifier);
      }

      //Do the actual grab classification logic
      UpdateClassifier(classifier, _hand);

      if (classifier.isGrabbing != classifier.prevGrabbing) {
        if (classifier.isGrabbing) {
          if (!_manager.TwoHandedGrasping) { _manager.ReleaseObject(behaviour); }
          _manager.GraspWithHand(_hand, behaviour);
        } else if (!classifier.isGrabbing || behaviour.IsBeingGraspedByHand(_hand.Id)) {
          _manager.ReleaseHand(_hand.Id);
          classifier.coolDownProgress = 0f;
        }
      }
      classifier.prevGrabbing = classifier.isGrabbing;
    }

    //Classifier bookkeeping
    public void UpdateHeuristicClassifier(Hand hand) {
      if (hand != null) {

        //First check if already holding an object and only process that one
        bool alreadyGrasping = false;
        var graspedBehaviours = _manager.GraspedObjects;
        for (int i = 0; i < graspedBehaviours.Count; i++) {
          if (graspedBehaviours[i].IsBeingGraspedByHand(hand.Id)) {
            UpdateBehaviour(graspedBehaviours[i], hand);
            alreadyGrasping = true;
            break;
          }
        }

        //If not, process all objects
        if (!alreadyGrasping) {
          var activeBehaviours = _manager._activityManager.ActiveBehaviours;
          for (int i = 0; i < activeBehaviours.Count; i++) {
            UpdateBehaviour(activeBehaviours[i], hand);
          }
        }
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
      public float coolDownProgress;

      public GrabClassifier(IInteractionBehaviour behaviour) {
        probes = new GrabProbe[5];
        transform = behaviour.transform;
        body = behaviour.GetComponent<Rigidbody>();
        warper = (behaviour as InteractionBehaviour).warper;
        coolDownProgress = 0;
      }
    }

    //Per-Finger Per-Object Probe
    public struct GrabProbe { public bool isInside; public float curl; };
  }
}
