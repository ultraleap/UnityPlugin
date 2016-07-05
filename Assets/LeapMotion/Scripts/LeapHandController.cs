using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap;

namespace Leap.Unity {
  /**
   * LeapHandController uses a Factory to create and update HandRepresentations based on Frame's received from a Provider  */
  public class LeapHandController : MonoBehaviour {
    protected LeapProvider provider;
    protected HandFactory factory;

    protected Dictionary<int, HandRepresentation> graphicsReps = new Dictionary<int, HandRepresentation>();
    protected Dictionary<int, HandRepresentation> physicsReps = new Dictionary<int, HandRepresentation>();

    // Reference distance from thumb base to pinky base in mm.
    protected const float GIZMO_SCALE = 5.0f;

    protected bool graphicsEnabled = true;
    protected bool physicsEnabled = true;

    public bool GraphicsEnabled {
      get {
        return graphicsEnabled;
      }
      set {
        graphicsEnabled = value;
      }
    }

    public bool PhysicsEnabled {
      get {
        return physicsEnabled;
      }
      set {
        physicsEnabled = value;
      }
    }

    /** Draws the Leap Motion gizmo when in the Unity editor. */
    void OnDrawGizmos() {
      Gizmos.matrix = Matrix4x4.Scale(GIZMO_SCALE * Vector3.one);
      Gizmos.DrawIcon(transform.position, "leap_motion.png");
    }

    protected virtual void OnEnable() {
      provider = requireComponent<LeapProvider>();
      factory = requireComponent<HandFactory>();

      provider.OnUpdateFrame += OnUpdateFrame;
      provider.OnFixedFrame += OnFixedFrame;
    }

    protected virtual void OnDisable() {
      provider.OnUpdateFrame -= OnUpdateFrame;
      provider.OnFixedFrame -= OnFixedFrame;
    }

    /** Updates the graphics HandRepresentations. */
    protected virtual void OnUpdateFrame(Frame frame) {
      if (frame != null && graphicsEnabled) {
        UpdateHandRepresentations(graphicsReps, ModelType.Graphics, frame);
      }
    }

    /** Updates the physics HandRepresentations. */
    protected virtual void OnFixedFrame(Frame frame) {
      if (frame != null && physicsEnabled) {
        UpdateHandRepresentations(physicsReps, ModelType.Physics, frame);
      }
    }

    /** 
    * Updates HandRepresentations based in the specified HandRepresentation Dictionary.
    * Active HandRepresentation instances are updated if the hand they represent is still
    * present in the Provider's CurrentFrame; otherwise, the HandRepresentation is removed. If new
    * Leap Hand objects are present in the Leap HandRepresentation Dictionary, new HandRepresentations are 
    * created and added to the dictionary. 
    * @param all_hand_reps = A dictionary of Leap Hand ID's with a paired HandRepresentation
    * @param modelType Filters for a type of hand model, for example, physics or graphics hands.
    * @param frame The Leap Frame containing Leap Hand data for each currently tracked hand
    */
    protected virtual void UpdateHandRepresentations(Dictionary<int, HandRepresentation> all_hand_reps, ModelType modelType, Frame frame) {
      foreach (Leap.Hand curHand in frame.Hands) {
        HandRepresentation rep;
        if (!all_hand_reps.TryGetValue(curHand.Id, out rep)) {
          rep = factory.MakeHandRepresentation(curHand, modelType);
          if (rep != null) {
            all_hand_reps.Add(curHand.Id, rep);
          }
        }
        if (rep != null) {
          rep.IsMarked = true;
          rep.UpdateRepresentation(curHand);
          rep.LastUpdatedTime = (int)frame.Timestamp;
        }
      }

      /** Mark-and-sweep to finish unused HandRepresentations */
      HandRepresentation toBeDeleted = null;
      foreach (KeyValuePair<int, HandRepresentation> r in all_hand_reps) {
        if (r.Value != null) {
          if (r.Value.IsMarked) {
            r.Value.IsMarked = false;
          } else {
            /** Initialize toBeDeleted with a value to be deleted */
            //Debug.Log("Finishing");
            toBeDeleted = r.Value;
          }
        }
      }
      /**Inform the representation that we will no longer be giving it any hand updates 
       * because the corresponding hand has gone away */
      if (toBeDeleted != null) {
        all_hand_reps.Remove(toBeDeleted.HandID);
        toBeDeleted.Finish();
      }
    }

    private T requireComponent<T>() where T : Component {
      T component = GetComponent<T>();
      if (component == null) {
        string componentName = typeof(T).Name;
        Debug.LogError("LeapHandController could not find a " + componentName + " and has been disabled.  Make sure there is a " + componentName + " on the same gameObject.");
        enabled = false;
      }
      return component;
    }
  }
}
