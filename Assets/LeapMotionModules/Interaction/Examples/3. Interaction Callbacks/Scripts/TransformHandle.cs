using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.Examples {

  [RequireComponent(typeof(InteractionBehaviour))]
  public class TransformHandle : MonoBehaviour {

    protected InteractionBehaviour _intObj;
    protected TransformTool _tool;

    public UnityEvent OnShouldShowHandle  = new UnityEvent();
    public UnityEvent OnShouldHideHandle  = new UnityEvent();
    public UnityEvent OnHandleActivated   = new UnityEvent();
    public UnityEvent OnHandleDeactivated = new UnityEvent();

    protected virtual void Start() {
      _intObj = GetComponent<InteractionBehaviour>();
      _intObj.OnObjectGraspBegin += onObjectGraspBegin;
      _intObj.OnObjectGraspEnd += onObjectGraspEnd;

      _tool = GetComponentInParent<TransformTool>();
      if (_tool == null) Debug.LogError("No TransformTool found in a parent GameObject.");
    }

    public void syncRigidbodyWithTransform() {
      _intObj.rigidbody.position = this.transform.position;
      _intObj.rigidbody.rotation = this.transform.rotation;
    }

    private void onObjectGraspBegin(List<InteractionHand> hands) {
      _tool.NotifyHandleActivated(this);

      OnHandleActivated.Invoke();
    }

    private void onObjectGraspEnd(List<InteractionHand> hands) {
      _tool.NotifyHandleDeactivated(this);

      OnHandleDeactivated.Invoke();
    }

    #region Handle Visibility

    /// <summary>
    /// Called by the Transform Tool when this handle should be visible.
    /// </summary>
    public void EnsureVisible() {
      OnShouldShowHandle.Invoke();
    }

    /// <summary>
    /// Called by the Transform Tool when this handle should not be visible.
    /// </summary>
    public void EnsureHidden() {
      OnShouldHideHandle.Invoke();
    }

    #endregion

  }

}
