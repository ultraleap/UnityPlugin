using Leap.Unity.UI.Interaction;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples.TransformHandles {

  [RequireComponent(typeof(InteractionBehaviour))]
  public abstract class InteractionTransformHandle : MonoBehaviour {

    // We'll hide the MeshRenderer when a hand isn't hovering nearby this handle.
    private const float MIN_RENDER_DISTANCE = 0.30F; // In this case, "nearby" means 30 cm.
    private MeshRenderer _meshRenderer;

    protected TransformTool _tool;
    protected InteractionBehaviour _intObj;

    public Action<InteractionTransformHandle> onHandleActivated = (handle) => { };
    public Action<InteractionTransformHandle> onHandleDeactivated = (handle) => { };

    protected virtual void Start() {
      _tool = GetComponentInParent<TransformTool>();
      _intObj = GetComponent<InteractionBehaviour>();
      _meshRenderer = GetComponent<MeshRenderer>();
    }

    protected virtual void Update() {
      if (_intObj.isHovered && Vector3.Distance(this.transform.position,
                                               _intObj.closestHoveringHand.PalmPosition.ToVector3())
                                                  < MIN_RENDER_DISTANCE) {
        _meshRenderer.enabled = true;
      }
      else {
        _meshRenderer.enabled = false;
      }
    }

  }

}