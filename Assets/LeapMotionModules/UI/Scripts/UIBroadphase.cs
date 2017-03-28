using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.RuntimeGizmos;

namespace Leap.Unity.UI.Interaction {
  public class UIBroadphase : MonoBehaviour, IRuntimeGizmoComponent {
    public float activationRadius = 0.2f;
    public InteractionManager manager;

    private Collider[] _colliderResultsBuffer = new Collider[128];
    private HashSet<UnifiedUIElement> _activeBehaviours = new HashSet<UnifiedUIElement>();
    //private Hand warpedHand = new Hand();
    private LeapGui[] guis;
    private struct UnifiedUIElement {
      public LeapGuiMeshElement element;
      public InteractionBehaviourBase behaviour;
    }
    private UnifiedUIElement tempElement = new UnifiedUIElement();
    private UnifiedUIElement curElement = new UnifiedUIElement();


    /*
    public UIBroadphase(InteractionManager manager, float activationRadius = 0.2f) {
      this.manager = manager;
      this.activationRadius = activationRadius;
    }*/

    void Start() {
      if (manager == null) {
        manager = InteractionManager.singleton;
      }
      guis = FindObjectsOfType<LeapGui>();
    }

    public void FixedUpdateHand(Hand hand) {
      //warpedHand.CopyFrom(hand);

      _activeBehaviours.Clear();

      foreach (LeapGui gui in guis) {
        int count = GetSphereColliderResults(transformPoint(hand.PalmPosition.ToVector3(), gui), activationRadius, _colliderResultsBuffer, out _colliderResultsBuffer);
        UpdateActiveList(count, _colliderResultsBuffer);
      }

      float leastTotalDistance = 1000f;

      for (int i = 0; i < 5; i++) {
        if (!hand.Fingers[i].IsExtended) { continue; }
        float leastDistance = 1000f;
        Vector3 fingerTip = hand.Fingers[i].TipPosition.ToVector3();
        foreach (UnifiedUIElement elem in _activeBehaviours) {
          if (elem.element != null) {
            //NEED BETTER DISTANCE FUNCTION GRAH
            float dist = Vector3.SqrMagnitude(elem.element.transform.position - transformPoint(fingerTip, elem.element));
            if (dist < leastDistance) {
              tempElement = elem;
              leastDistance = dist;
            }
          }
        }

        if (leastDistance != 1000f) {
          //Warp finger to element space
          Vector3 newPos; Quaternion newRot;
          transformTransform(hand.Fingers[i].bones[3].NextJoint.ToVector3(), hand.Fingers[i].bones[3].Rotation.ToQuaternion(), curElement.element, out newPos, out newRot);
          hand.Fingers[i].SetTipTransform(newPos, newRot);

          if(leastDistance < leastTotalDistance) {
            leastTotalDistance = leastDistance;
            curElement = tempElement;
          }
        }
      }

      if(leastTotalDistance != 1000f) {
        //Activate element's collision
        curElement.element.Tint().tint = Color.red;
      }
    }

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      drawer.DrawSphere(curElement.element.transform.position, 0.1f);
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
      //warpedHand.CopyFrom(inHand);

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