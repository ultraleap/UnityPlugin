using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {
  public class UIBroadphase {
    public float activationRadius = 0.2f;
    public InteractionManager manager;
    public UnifiedUIElement[] perFingerClosestElement = new UnifiedUIElement[3];
    public UnifiedUIElement perHandClosestElement = new UnifiedUIElement();
    public struct UnifiedUIElement {
      public LeapGuiMeshElement element;
      public InteractionBehaviourBase behaviour;
      //This shouldn't be necessary but my unified struct generates lots of garbage if I don't override GetHashCode()
      public override int GetHashCode() {
        return behaviour.GetHashCode();
      }
    }

    private Collider[] _colliderResultsBuffer = new Collider[128];
    private HashSet<UnifiedUIElement> _activeBehaviours = new HashSet<UnifiedUIElement>();
    private Hand originalHand = new Hand();
    
    public UIBroadphase(InteractionManager manager, float activationRadius = 0.2f) {
      this.manager = manager;
      this.activationRadius = activationRadius;
    }

    public void FixedUpdateHand(Hand hand, LeapGui[] guis) {
      originalHand.CopyFrom(hand);
      _activeBehaviours.Clear();

      foreach (LeapGui gui in guis) {
        int count = GetSphereColliderResults(transformPoint(hand.PalmPosition.ToVector3(), gui), activationRadius, _colliderResultsBuffer, out _colliderResultsBuffer);
        UpdateActiveList(count, _colliderResultsBuffer);
      }

      float leastHandDistance = float.PositiveInfinity;
      for (int i = 0; i < perFingerClosestElement.Length; i++) {
        if (!hand.Fingers[i].IsExtended) { continue; }
        float leastFingerDistance = float.PositiveInfinity;
        Vector3 fingerTip = hand.Fingers[i].TipPosition.ToVector3();
        foreach (UnifiedUIElement elem in _activeBehaviours) {
          if (elem.element != null) {

            //NEED BETTER DISTANCE FUNCTION
            float dist = Vector3.SqrMagnitude(elem.element.transform.position - transformPoint(fingerTip, elem.element));

            if (dist < leastFingerDistance) {
              perFingerClosestElement[i] = elem;
              leastFingerDistance = dist;
              if (leastFingerDistance < leastHandDistance) {
                leastHandDistance = leastFingerDistance;
                perHandClosestElement = perFingerClosestElement[i];
              }
            }
          }
        }
      }

      if (perHandClosestElement.element != null) {
        //Transform bulk hand to the closest element's warped space
        coarseInverseTransformHand(hand, perFingerClosestElement[1].element.attachedGroup.gui);

        //(Eventually) Activate element's collision
        perHandClosestElement.element.Tint().tint = Color.red;
      }

      //Warp finger to respective closest element spaces
      for (int i = 0; i < perFingerClosestElement.Length; i++) {
        if (perFingerClosestElement[i].element != null) {
          Vector3 newPos; Quaternion newRot;
          transformTransform(originalHand.Fingers[i].bones[3].NextJoint.ToVector3(), originalHand.Fingers[i].bones[3].Rotation.ToQuaternion(), perFingerClosestElement[i].element, out newPos, out newRot);
          hand.Fingers[i].SetTipTransform(newPos, newRot);
        }
      }
    }

    private void UpdateActiveList(int numResults, Collider[] results) {
      for (int i = 0; i < numResults; i++) {
        if (results[i].attachedRigidbody != null) {
          InteractionBehaviourBase interactionObj;
          if (results[i].attachedRigidbody != null && manager.RigidbodyRegistry.TryGetValue(results[i].attachedRigidbody, out interactionObj)) {
            UnifiedUIElement elem = new UnifiedUIElement();
            elem.behaviour = interactionObj;
            elem.element = interactionObj.GetComponent<LeapGuiMeshElement>();
            elem.element.Tint().tint = Color.Lerp(elem.element.Tint().tint, Color.white, 0.1f);
            _activeBehaviours.Add(elem);
          }
        }
      }
    }

    public Vector3 transformPoint(Vector3 worldPoint, LeapGui gui) {
      ITransformer space = gui.space.GetTransformer(gui.transform);
      Vector3 localPalmPos = gui.transform.InverseTransformPoint(worldPoint);
      return gui.transform.TransformPoint(space.InverseTransformPoint(localPalmPos));
    }

    public Vector3 transformPoint(Vector3 worldPoint, LeapGuiElement elem) {
      Vector3 localPalmPos = elem.attachedGroup.gui.transform.InverseTransformPoint(worldPoint);
      ITransformer space = elem.attachedGroup.gui.space.GetTransformer(elem.anchor);
      return elem.attachedGroup.gui.transform.TransformPoint(space.InverseTransformPoint(localPalmPos));
    }

    public void transformTransform(Vector3 worldPoint, Quaternion worldRotation, LeapGuiElement elem, out Vector3 transformedPosition, out Quaternion transformedRotation) {
      if (elem != null) {
        Vector3 localPos = elem.attachedGroup.gui.transform.InverseTransformPoint(worldPoint);
        Quaternion localRot = elem.attachedGroup.gui.transform.InverseTransformRotation(worldRotation);
        ITransformer space = elem.attachedGroup.gui.space.GetTransformer(elem.anchor);
        transformedPosition = elem.attachedGroup.gui.transform.TransformPoint(space.InverseTransformPoint(localPos));
        transformedRotation = elem.attachedGroup.gui.transform.TransformRotation(space.InverseTransformRotation(localPos, localRot));
      } else {
        transformedPosition = worldPoint;
        transformedRotation = worldRotation;
      }
    }

    public void coarseInverseTransformHand(Hand inHand, LeapGui gui) {
      ITransformer space = gui.space.GetTransformer(gui.transform);
      Vector3 localPalmPos = gui.transform.InverseTransformPoint(inHand.PalmPosition.ToVector3());
      Quaternion localPalmRot = gui.transform.InverseTransformRotation(inHand.Rotation.ToQuaternion());

      inHand.SetTransform(gui.transform.TransformPoint(space.InverseTransformPoint(localPalmPos)),
                          gui.transform.TransformRotation(space.InverseTransformRotation(localPalmPos, localPalmRot)));
    }

    private int GetSphereColliderResults(Vector3 position, float radius, Collider[] resultsBuffer_in, out Collider[] resultsBuffer_out) {
      resultsBuffer_out = resultsBuffer_in;

      int overlapCount = 0;
      while (true) {
        overlapCount = Physics.OverlapSphereNonAlloc(position, radius, resultsBuffer_in, 1 << manager.InteractionLayer, QueryTriggerInteraction.Collide);
        if (overlapCount < resultsBuffer_out.Length) {
          break;
        } else {
          resultsBuffer_out = new Collider[resultsBuffer_out.Length * 2];
          resultsBuffer_in = resultsBuffer_out;
        }
      }
      return overlapCount;
    }
  }
}