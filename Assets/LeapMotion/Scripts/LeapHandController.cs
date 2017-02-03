using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap;

namespace Leap.Unity {
  /**
   * LeapHandController uses a Factory to create and update HandProxies based on Frame's received from a Provider  */
  public class LeapHandController : MonoBehaviour {
    protected LeapProvider provider;
    protected HandPool pool;

    protected Dictionary<int, HandProxy> graphicsProxies = new Dictionary<int, HandProxy>();
    protected Dictionary<int, HandProxy> physicsProxies = new Dictionary<int, HandProxy>();

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
      pool = requireComponent<HandPool>();

      provider.OnUpdateFrame += OnUpdateFrame;
      provider.OnFixedFrame += OnFixedFrame;
    }

    protected virtual void OnDisable() {
      provider.OnUpdateFrame -= OnUpdateFrame;
      provider.OnFixedFrame -= OnFixedFrame;
    }

    /** Updates the graphics HandProxies. */
    protected virtual void OnUpdateFrame(Frame frame) {
      if (frame != null && graphicsEnabled) {
        UpdateHandProxies(graphicsProxies, ModelType.Graphics, frame);
      }
    }

    /** Updates the physics HandProxies. */
    protected virtual void OnFixedFrame(Frame frame) {
      if (frame != null && physicsEnabled) {
        UpdateHandProxies(physicsProxies, ModelType.Physics, frame);
      }
    }

    /** 
    * Updates HandProxies based in the specified HandProxy Dictionary.
    * Active HandProxy instances are updated if the hand they represent is still
    * present in the Provider's CurrentFrame; otherwise, the HandProxy is removed. If new
    * Leap Hand objects are present in the Leap HandProxy Dictionary, new HandProxies are 
    * created and added to the dictionary. 
    * @param all_hand_proxies = A dictionary of Leap Hand ID's with a paired HandProxy
    * @param modelType Filters for a type of hand model, for example, physics or graphics hands.
    * @param frame The Leap Frame containing Leap Hand data for each currently tracked hand
    */
    protected virtual void UpdateHandProxies(Dictionary<int, HandProxy> all_hand_proxies, ModelType modelType, Frame frame) {
      for (int i = 0; i < frame.Hands.Count; i++) {
        var curHand = frame.Hands[i];
        HandProxy prox;
        if (!all_hand_proxies.TryGetValue(curHand.Id, out prox)) {
          prox = pool.MakeHandProxy(curHand, modelType);
          if (prox != null) {
            all_hand_proxies.Add(curHand.Id, prox);
          }
        }
        if (prox != null) {
          prox.IsMarked = true;
          if (prox.Group.HandPostProcesses.GetPersistentEventCount() > 0) {
            prox.PostProcessHand.CopyFrom(curHand);
            prox.Group.HandPostProcesses.Invoke(prox.PostProcessHand);
            prox.UpdateProxy(prox.PostProcessHand);
          } else {
            prox.UpdateProxy(curHand);
          }
          prox.LastUpdatedTime = (int)frame.Timestamp;
        }
      }

      /** Mark-and-sweep to finish unused HandProxies */
      HandProxy toBeDeleted = null;
      for (var it = all_hand_proxies.GetEnumerator(); it.MoveNext();) {
        var r = it.Current;
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
      /**Inform the proxy that we will no longer be giving it any hand updates 
       * because the corresponding hand has gone away */
      if (toBeDeleted != null) {
        all_hand_proxies.Remove(toBeDeleted.HandID);
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
